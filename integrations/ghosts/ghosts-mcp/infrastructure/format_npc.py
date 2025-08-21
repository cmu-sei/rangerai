import json
import csv
from io import StringIO

def json_to_csv(json_data):
    # Parse JSON if input is a string
    if isinstance(json_data, str):
        data = json.loads(json_data)
    else:
        data = json_data

    output = StringIO()
    writer = csv.DictWriter(output, fieldnames=[
        "id", "first_name", "last_name", "email", "home_phone", "cell_phone"
    ])
    writer.writeheader()

    for entry in data:
        npc = entry.get("npcProfile", {})
        name = npc.get("name", {})
        unit = npc.get("unit", {}).get("address", {})

        writer.writerow({
            "id": npc.get("id"),
            "first_name": name.get("first"),
            "last_name": name.get("last"),
            "email": npc.get("email"),
            "home_phone": npc.get("homePhone"),
            "cell_phone": npc.get("cellPhone")
        })

    return output.getvalue()
