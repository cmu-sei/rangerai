#!/usr/bin/env python3
"""Mock Chronicles from the Scenario Builder's scenarios, so the Chronicle UI has real-looking events.

Reads every scenario from the Scenario Builder API, maps its answers to World, Intent and (for a GraySpace
deployment) As Built items, and writes one add entry per item. Each entry carries the decision that set it: its time,
who made it, whether by a person or the agent, whether it is still proposed, and the reason given.

    python3 src/chronicle/tools/mock-from-scenario-builder.py                    # write the files only
    python3 src/chronicle/tools/mock-from-scenario-builder.py --post             # and import them into the Chronicle API

Files go to src/chronicle/samples/scenario-builder/, which the API's sample seeder does not read. --post skips
Chronicles that already exist. This is a mock-up, not the Scenario Builder's publisher: the mapping is a first guess.
"""

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
import uuid
from pathlib import Path

SCHEMA_VERSION = "0.3.0"
NAMESPACE = uuid.UUID("6f3c1c2e-9a51-4c38-9d0e-3b8f3f0c7a11")


def get(url):
    with urllib.request.urlopen(url, timeout=10) as r:
        return json.load(r)


def slug(text, limit=40):
    s = re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")
    return s[:limit].rstrip("-") or "x"


def visibility(entry):
    return entry.get("visibility", "participant") if isinstance(entry, dict) else "participant"


def text_of(entry):
    return entry["text"] if isinstance(entry, dict) else str(entry)


class Builder:
    """Collects items with the decision behind each, then orders them into a history."""

    def __init__(self, scenario, summary):
        self.scenario = scenario
        self.summary = summary
        self.decisions = {d["question"]: d for d in scenario.get("decisions", [])}
        self.items = []  # (part, item, decision or None, author override)
        self.ids = set()

    def unique(self, base):
        candidate, n = base, 2
        while candidate in self.ids:
            candidate, n = f"{base}-{n}", n + 1
        self.ids.add(candidate)
        return candidate

    def add(self, part, kind, base_id, question, author=None, **fields):
        decision = self.decisions.get(question)
        item = {"kind": kind, "id": self.unique(base_id), **{k: v for k, v in fields.items() if v not in (None, [], "")}}
        item.setdefault("visibility", "participant")
        item["standing"] = "proposed" if decision and decision.get("who") == "proposed" else "stated"
        self.items.append((part, item, decision, author))
        return item["id"]

    def fact(self, topic, text, question, vis="participant"):
        if text:
            return self.add("world", "fact", slug(re.sub(r"(?<=[a-z])(?=[A-Z])", "-", topic)), question, topic=topic,
                            text=text, visibility=vis)
        return None

    def history(self, chronicle_id):
        fallback = self.summary["updated"]
        ordered = sorted(enumerate(self.items), key=lambda p: ((p[1][2] or {}).get("time") or fallback, p[0]))
        entries = []
        for seq, (_, (part, item, decision, author)) in enumerate(ordered, start=1):
            if author is None:
                author = {"app": "scenario-builder", "actor": (decision or {}).get("by") or self.scenario_owner(),
                          "via": (decision or {}).get("via") or "app"}
            entry = {
                "id": str(uuid.uuid5(NAMESPACE, f"{chronicle_id}/{item['id']}")),
                "chronicleId": chronicle_id,
                "sequence": seq,
                "recordedAt": (decision or {}).get("time") or fallback,
                "part": part,
                "kind": item["kind"],
                "op": "add",
                "itemId": item["id"],
                "item": item,
                "author": author,
            }
            if decision and decision.get("reason"):
                entry["reason"] = decision["reason"]
            entries.append(entry)
        return entries

    def scenario_owner(self):
        return self.summary.get("owner") or "designer"


def build(scenario, summary):
    b = Builder(scenario, summary)
    eng = scenario.get("engagement", {})
    sc = scenario.get("scenario", {})
    net = scenario.get("network", {})
    names = scenario.get("names", {})
    vocab = scenario.get("vocabulary", {})

    # ---- World ----
    sides = {}
    for entry in names.get("organizations", []):
        name, _, stance = entry.partition(":")
        stance = stance.strip().lower()
        side_key = {"friendly": "blue", "adversary": "red", "hostile": "red"}.get(stance)
        if side_key and side_key not in sides:
            sides[side_key] = b.add("world", "side", side_key, "organizations",
                                    name="Friendly" if side_key == "blue" else "Adversary",
                                    visibility="participant" if side_key == "blue" else "excon")
        b.add("world", "organization", f"org-{slug(name)}", "organizations", name=name.strip(),
              sideId=sides.get(side_key), visibility="excon" if side_key == "red" else "participant")
    if "blue" in sides and "red" in sides:
        for item in (i for _, i, _, _ in b.items if i["kind"] == "side"):
            item["relations"] = [{"sideId": "red" if item["id"] == "blue" else "blue", "stance": "hostile"}]

    b.fact("situation", sc.get("placeAndTime"), "placeAndTime")
    b.fact("audience", eng.get("participants") or eng.get("unit"), "participants" if "participants" in eng else "unit")
    b.fact("audience", eng.get("headcount") or eng.get("teamSize"), "headcount" if "headcount" in eng else "teamSize")
    b.fact("audience", eng.get("proficiency"), "proficiency")
    b.fact("informationEnvironment", sc.get("population"), "population")
    b.fact("informationEnvironment", sc.get("informationEnvironment"), "informationEnvironment")
    b.fact("network", net.get("notional"), "notionalNetwork")
    b.fact("simulatedUsers", net.get("simulatedUsers"), "simulatedUsers")
    for entry in sc.get("groundTruth", []) + scenario.get("groundTruth", []):
        b.add("world", "fact", f"gt-{slug(entry.get('id', text_of(entry)))}", "groundTruth", topic="groundTruth",
              text=text_of(entry), visibility=visibility(entry))
    b.fact("groundTruth", sc.get("attackPath"), "attackPath", "excon")
    b.fact("groundTruth", sc.get("adversary"), "adversary", "excon")
    b.fact("groundTruth", sc.get("motive"), "motive", "excon")
    b.fact("redHerring", sc.get("redHerring"), "redHerring", "excon")
    b.fact("redHerring", sc.get("obviousExplanation"), "obviousExplanation", "excon")
    for entry in sc.get("attackTechniques", []):
        b.fact("attackTechnique", text_of(entry), "attackTechniques", visibility(entry))
    for entry in net.get("planted", []):
        b.fact("planted", text_of(entry), "planted", visibility(entry))

    built = []
    for entry in net.get("segments", []):
        name, _, about = entry.partition(":")
        built.append(b.add("world", "network", f"net-{slug(name)}", "segments", name=name.strip(),
                           description=about.strip() or None))
    for entry in net.get("hosts", []):
        name, _, about = entry.partition(":")
        role = about.rsplit(",", 1)[0].strip() if about else None
        built.append(b.add("world", "device", f"host-{slug(name)}", "hosts", name=name.strip(),
                           deviceTypes=["ComputerNode"], isVirtual=True, role=role))

    # ---- Intent ----
    measures = sc.get("successCriteria") or sc.get("standards") or []
    objectives = scenario.get("objectives", [])
    objective_ids = []
    for i, entry in enumerate(objectives):
        objective_ids.append(b.add(
            "intent", "objective", f"obj-{slug(entry.get('id', str(i + 1)))}",
            "objectives" if "objectives" in b.decisions else "tasks", text=entry["text"],
            measures=[measures[i]] if len(measures) == len(objectives) else []))
    for entry in scenario.get("teachingPoints", []):
        b.add("intent", "teachingPoint", f"tp-{slug(text_of(entry), 30)}", "teachingPoints", text=text_of(entry),
              visibility=visibility(entry))
    for field, category in [("rulesOfEngagement", "conduct"), ("escalationCeiling", "scope"), ("deadline", "timing")]:
        value = eng.get(field) or sc.get(field)
        if value:
            b.add("intent", "rule", f"rule-{category}", field, text=value, category=category)
    for usage, question in [("fixed", "fixedTerms"), ("banned", "bannedTerms")]:
        for term in vocab.get(usage, []):
            if term.strip().lower() not in ("none", ""):
                b.add("intent", "term", f"term-{slug(term, 30)}", question, text=term, usage=usage)
    previous = None
    for entry in sc.get("keyEvents", []):
        previous = b.add("intent", "plannedEvent", f"pe-{slug(entry.get('id', text_of(entry)))}", "keyEvents",
                         title=text_of(entry), visibility=visibility(entry), after=[previous] if previous else [],
                         objectiveIds=objective_ids if len(objective_ids) == 1 else [])
    for entry in sc.get("decisionPoints", []):
        b.add("intent", "summary", f"dp-{slug(entry.get('id', text_of(entry)))}", "decisionPoints",
              title=f"Decision point {entry.get('id', '')}".strip(), text=text_of(entry), visibility=visibility(entry))
    for i, entry in enumerate(sc.get("escalationSteps", []), start=1):
        b.add("intent", "summary", f"esc-{i}", "escalationSteps", title=f"Escalation step {i}", text=text_of(entry),
              visibility=visibility(entry))
    if sc.get("executiveSummary"):
        b.add("intent", "summary", "executive-summary", "executiveSummary", title="Executive summary",
              text=sc["executiveSummary"])

    # ---- As Built: what GraySpace reported building ----
    for d in scenario.get("deployments", []):
        b.add("asBuilt", "deployment", f"dep-{slug(d.get('target', 'build'))}-{slug(d.get('enclave', ''), 30)}", "",
              author={"app": d.get("target", "grayspace"), "via": "app"}, builder=d.get("target", "grayspace"),
              reference=d.get("enclave"), version=d.get("version"), state="ready", builds=built, visibility="excon")

    chronicle_id = scenario["slug"]
    history = b.history(chronicle_id)
    part = lambda name: [e["item"] for e in history if e["part"] == name]  # parts follow the history's order
    return {
        "schemaVersion": SCHEMA_VERSION,
        "id": chronicle_id,
        "name": scenario["name"],
        "kind": "exercise",
        "scenarioType": scenario["scenarioType"],
        **({"description": d} if (d := sc.get("description") or sc.get("problem") or eng.get("intent")) else {}),
        "created": summary["created"],
        "world": part("world"),
        "intent": part("intent"),
        "asBuilt": part("asBuilt"),
        "asRun": [],
        "lessons": [],
        "history": history,
    }


def post(api, record):
    request = urllib.request.Request(f"{api}/api/chronicles", data=json.dumps(record).encode(), method="POST",
                                     headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=10) as r:
            return r.status, ""
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:400]


def main():
    here = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--scenario-builder", default="http://localhost:5280", help="Scenario Builder API")
    parser.add_argument("--chronicle", default="http://localhost:5285", help="Chronicle API, for --post")
    parser.add_argument("--out", default=str(here.parent / "samples" / "scenario-builder"), help="where to write")
    parser.add_argument("--post", action="store_true", help="import each Chronicle into the Chronicle API")
    args = parser.parse_args()

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    failed = False
    for summary in get(f"{args.scenario_builder}/api/scenarios"):
        if summary.get("archived"):
            continue
        record = build(get(f"{args.scenario_builder}/api/scenarios/{summary['id']}"), summary)
        path = out / f"{record['id']}.chronicle.json"
        path.write_text(json.dumps(record, indent=2, ensure_ascii=False) + "\n")
        line = f"{record['id']:<24} {len(record['history']):>3} entries  {path.name}"
        if args.post:
            status, body = post(args.chronicle, record)
            line += {201: "  imported", 409: "  already there"}.get(status, f"  HTTP {status} {body}")
            failed |= status not in (201, 409)
        print(line)
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
