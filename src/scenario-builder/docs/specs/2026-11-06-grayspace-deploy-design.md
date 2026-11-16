# Deploy to GraySpace: Wiring the Page

2026-11-06. Approved in chat the same day. Replaces the read-only GraySpace stub (decision 2 in
[the scenario builder design](2026-10-29-scenario-builder-design.md)).

## What It Does

The Topology page's **Deploy to GraySpace** screen connects to one deployment of a GraySpace (Terrain API 1.7.0, or a later build; see below), shows what that
GraySpace offers, lets a person build the scenario's enclave, and deploys it.

- **Connection.** An admin gives GraySpace's address and its `GRAYSPACE_API_TOKEN`. One GraySpace runs several
  deployments, each a base and its enclaves, so the address is a deployment's base URL
  (`http://grayspace.local:8080/deployments/abc`); given the server's address alone, the API takes its default
  deployment. The API saves them only once GraySpace answers `GET /deployments/{id}` with that token, which it does
  with or without a deployed base. What the server offers (images, roles, jobs, capacity) is read from
  `GET /capabilities`, asked for a shipped version when no base is deployed. The token never
  leaves the API. Everyone else sees where it is connected, by whom and since when.
- **Live offers, read-only:** the base and its health; attachment points (from the map's routers when GraySpace has no
  `/attachment-points`); images; roles, with the variables each needs;
  DNS and CAs; the base's links. Each part GraySpace fails to give shows its error in its own section.
- **Deploy everything,** at the top: whatever is missing, in order, each with its default. The base if none is deployed
  (the deployed or first shipped version, at tier1, the smaller base; checked first), the enclave as drafted or, if the
  draft is empty, one NAT site with one segment and one host on the smallest container image, then an instance of it,
  followed to the end. It stops where something fails and says where; pressed again, it carries on.
- **The base, by an admin:** pick a shipped version and a scope, check it (GraySpace's preflight; Deploy waits for a
  check with no errors), deploy, configure again, or destroy it. Destroy never takes live instances with it: GraySpace
  refuses and names them. Everyone sees the latest base job (stage, state and log, every 2 s while it runs), whoever
  started it, read from GraySpace's job list; an admin cancels or retries it.
- **The enclave, built on the page:** new (named from the scenario) or reused from GraySpace; sites with an attach point,
  NAT or routed, and segments; hosts with segment or internet, image, count, size, roles, a public address and the
  simulated-users label `ghosts.pool`; and the variables the chosen roles need. It saves as a draft as it is edited.
- **Deploy:** validate (findings with where and a suggested fix), save to GraySpace (a new version), plan, create an
  instance (prefix and version), follow its job (stage, state and log, every 2 s while it runs; cancel, retry), destroy.
  Plan and create without a version first save the current draft, so they never run a stale one.

Out of scope, so shown but disabled: adding or deleting images; DNS records and certificates; network conditions;
actions on a running instance.

## How It Works

- **Tables** (a fresh database; EnsureCreated adds no tables to an existing one): `GraySpaceConnections`, one row; and
  `Enclaves`, one per scenario, holding the draft spec as JSON, the last saved version, the last instance and the last
  job this page started.
- **Routes:** `GET/PUT/DELETE /api/grayspace` (PUT and DELETE are admin-only), `GET /api/grayspace/offers`, and under
  `/api/scenarios/{id}/enclave`: `GET`, `PUT`, `POST reuse`, `POST validate`, `POST save`, `POST plan`,
  `POST/GET/DELETE instance`, `POST job/{cancel|retry}`. GraySpace's JSON passes through as it wrote it.
- **Who:** any person working on the scenario; the agent is refused (403) on every route.
- **Errors:** not connected 409 `NOT_CONNECTED`; unreachable 502 `GRAYSPACE_UNREACHABLE`; token refused 502
  `GRAYSPACE_TOKEN_REJECTED`; GraySpace failing 502 `GRAYSPACE_FAILED`; any other refusal keeps GraySpace's status as
  `GRAYSPACE_REFUSED`, with its `detail` (findings included) beside the message.

## Verification

API tests against a fake GraySpace; UI specs for the page; a live run against GHOSTS's GraySpace simulator
(`/stubs/grayspace`, token `dev-token`). The simulator doesn't serve attachment points, a single enclave, plan or cancel,
so those are covered by the API tests only.

## GraySpace After 1.7.0

2026-11-16. GraySpace retired base clients: anything attached to the core, apart from its DNS and CA servers, is an
enclave. The deprecated slice names went with them, so the API now uses only the enclave ones:

- `/enclaves/{slug}` (put, validate, plan, reuse) and `GET /enclaves`, not `/slices`. Offers name the list `enclaves`.
- `POST /instances` sends `enclave`, not `slice`; an instance's or job's `enclave` is read first, `slice` second.
- Specs are `kind: Enclave`; the page shows `enclave_features` and `enclave_pool`.
- `/attachment-points` may be gone: the API falls back to the routers on `GET /map` (ISP, ASN, location, coordinates).
  The drafter attaches a site or internet host by its point's ISP and location, not its router.
- Images list `features`. The drafter puts a user's workstation, desktop or laptop on GraySpace's desktop image (an LXC
  image with the `desktop` feature, such as `grayspace-desktop`) when it takes the same roles as the LXC image, so a
  GHOSTS workstation keeps `ghosts-client`. The page marks desktop images in the image picker.

None of this was checked against the new build's OpenAPI document: its routes need a token. The routes and spec fields
come from the GraySpace web app's code and the 1.7.0 document, where the enclave names were already the current ones.
