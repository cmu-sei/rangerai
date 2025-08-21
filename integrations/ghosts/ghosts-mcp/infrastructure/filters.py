import csv
import io

def filter_csv_columns(csv_string, column_names):
    input_io = io.StringIO(csv_string)
    output_io = io.StringIO()

    reader = csv.DictReader(input_io)
    writer = csv.DictWriter(output_io, fieldnames=column_names)
    writer.writeheader()

    for row in reader:
        filtered_row = {k: row[k] for k in column_names if k in row}
        writer.writerow(filtered_row)

    return output_io.getvalue()