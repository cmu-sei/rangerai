// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Runtime.CompilerServices;

namespace Chronicle.Tests;

/// <summary>
/// The generated files under src/chronicle. A test fails when one is out of date; run the tests with
/// CHRONICLE_UPDATE=1 to rewrite them.
/// </summary>
public static class Files
{
    public static string Root([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, ".."));

    public static string RecordSchema => Path.Combine(Root(), "schema", "chronicle-record.v0.schema.json");
    public static string EntrySchema => Path.Combine(Root(), "schema", "chronicle-entry.v0.schema.json");
    public static string Sample => Path.Combine(Root(), "samples", "harbor-ttx.chronicle.json");

    private static bool Updating => Environment.GetEnvironmentVariable("CHRONICLE_UPDATE") == "1";

    /// <summary>Asserts <paramref name="path"/> holds <paramref name="expected"/>, or writes it when updating.</summary>
    public static void AssertCurrent(string path, string expected)
    {
        expected = expected.ReplaceLineEndings("\n") + "\n";
        if (Updating)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, expected);
            return;
        }
        Assert.True(File.Exists(path), $"{path} is missing; run the tests with CHRONICLE_UPDATE=1.");
        Assert.True(File.ReadAllText(path).ReplaceLineEndings("\n") == expected,
            $"{Path.GetFileName(path)} is out of date; run the tests with CHRONICLE_UPDATE=1 and review the diff.");
    }
}
