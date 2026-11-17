// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;

namespace Chronicle.Client;

/// <summary>
/// A Chronicle API call that didn't succeed: the HTTP status (absent when the API couldn't be reached), the API's
/// code (such as STALE, ID_TAKEN, NOT_FOUND, CHRONICLE_EXISTS) and its message.
/// </summary>
public class ChronicleApiException(HttpStatusCode? status, string? code, string message, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>The HTTP status; absent when the API couldn't be reached.</summary>
    public HttpStatusCode? Status { get; } = status;

    /// <summary>The API's code for what went wrong, from its problem response, when it gave one.</summary>
    public string? Code { get; } = code;

    /// <summary>The API couldn't be reached, or failed on its side (5xx): worth trying again later.</summary>
    public bool IsUnavailable => Status is null || (int)Status >= 500;

    /// <summary>There's no such Chronicle (or entry, or subscriber), or the audience may not see it.</summary>
    public bool IsNotFound => Status == HttpStatusCode.NotFound;

    /// <summary>
    /// The entry conflicts with what others wrote: STALE (a later entry already replaced the version it revises) or
    /// ID_TAKEN (another item has its id). Read the item again, and decide.
    /// </summary>
    public bool IsConflict => Status == HttpStatusCode.Conflict;

    /// <summary>The entry or record breaks a Chronicle rule (422); <see cref="Code"/> says which.</summary>
    public bool IsInvalid => Status == HttpStatusCode.UnprocessableEntity;
}
