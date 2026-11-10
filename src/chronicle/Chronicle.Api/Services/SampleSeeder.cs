// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Api.Services;

/// <summary>Imports the sample Chronicles that aren't stored yet, so a fresh API has something to show.</summary>
public static class SampleSeeder
{
    public static async Task Seed(ChronicleStore store, string directory, ILogger log)
    {
        if (!Directory.Exists(directory))
            return;
        foreach (var path in Directory.GetFiles(directory, "*.chronicle.json").Order())
        {
            var record = ChronicleJson.Deserialize<ChronicleRecord>(await File.ReadAllTextAsync(path));
            if (store.Get(record.Id) is not null)
                continue;
            var findings = await store.Create(record, CancellationToken.None);
            if (findings.Count > 0)
                log.LogWarning("Sample {Path} wasn't imported: {Findings}", path, string.Join("; ", findings.Select(f => f.Message)));
            else
                log.LogInformation("Imported sample Chronicle {Id}", record.Id);
        }
    }
}
