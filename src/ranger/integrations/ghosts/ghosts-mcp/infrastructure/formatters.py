import csv
import io
import json

def json_to_csv_string(json_data):
    if isinstance(json_data, str):
        json_data = json.loads(json_data)

    if not json_data:
        return ""

    output = io.StringIO()
    writer = csv.DictWriter(output, fieldnames=json_data[0].keys())
    writer.writeheader()
    writer.writerows(json_data)
    return output.getvalue()
