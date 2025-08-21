from pathlib import Path
import requests
import os
import json

# --- Configuration ---
EXPORT_DIR = "workflows"
N8N_API_URL = os.getenv("N8N_API_URL", "http://localhost:5678/api/v1/workflows")
N8N_API_KEY = os.getenv("N8N_API_KEY")

if not N8N_API_KEY:
    raise ValueError("N8N_API_KEY environment variable is not set.")

payload = {}
headers = {
  'accept': 'application/json',
  'X-N8N-API-KEY': N8N_API_KEY,
}

Path(EXPORT_DIR).mkdir(parents=True, exist_ok=True)

response = requests.request("GET", N8N_API_URL, headers=headers, data=payload)

workflows = response.json()
print(f"Found {len(workflows['data'])} workflows.")

for wf in workflows['data']:
    print(wf)
    if not isinstance(wf, dict):
        print(f"Skipping invalid entry: {wf}")
        continue

    wf_id = wf['id']
    wf_name = wf['name'].replace(" ", "_").replace("/", "_")
    
    r = requests.get(f"{N8N_API_URL}/{wf_id}", headers=headers)
    r.raise_for_status()
    wf_data = r.json()
    
    path = os.path.join(EXPORT_DIR, f"{wf_name}_{wf_id}.json")
    with open(path, "w") as f:
        json.dump(wf_data, f, indent=2)
    
    print(f"Exported: {wf_name}")

print(f"\nDone. All workflows saved to '{EXPORT_DIR}/'")
