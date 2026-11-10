// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Api.Models;

/// <summary>Why a record was refused: the spec's findings.</summary>
public sealed record InvalidChronicle(string Code, IReadOnlyList<Finding> Findings);
