// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle.Api.Models;

namespace Chronicle.Api.Services;

/// <summary>
/// Takes activity in: a notable one stores its entry first, then every one goes to the live feed and is counted against
/// its subscriber.
/// </summary>
public sealed class ActivityService(ChronicleStore store, LiveHub hub, SubscriberStore subscribers)
{
    /// <exception cref="KeyNotFoundException">There is no such Chronicle.</exception>
    /// <exception cref="ChronicleException">The activity's entry breaks a rule.</exception>
    public async Task<Activity> Report(string chronicleId, ActivityPost post, CancellationToken ct)
    {
        if (store.Get(chronicleId) is null)
            throw new KeyNotFoundException(chronicleId);
        long? sequence = null;
        var visibility = post.Visibility;
        if (post.Entry is { } entry)
        {
            var (stored, _) = await store.Append(chronicleId, entry, ct);
            sequence = stored.Sequence;
            visibility ??= stored.Item?.Visibility;
        }
        var activity = new Activity
        {
            Id = Guid.CreateVersion7().ToString(),
            ChronicleId = chronicleId,
            Source = post.Source,
            SubscriberId = post.SubscriberId,
            Actor = post.Actor,
            Kind = post.Kind,
            Text = post.Text,
            Host = post.Host,
            At = post.At ?? DateTimeOffset.UtcNow,
            Visibility = visibility ?? Audience.Excon,
            Data = post.Data,
            Sequence = sequence
        };
        hub.Add(activity);
        if (subscribers.Saw(activity) is { } subscriber && sequence is not null)
            Announce(subscriber);
        return activity;
    }

    /// <summary>Tells listeners a subscriber changed; its agent's prompts go only to exercise control and designers.</summary>
    public void Announce(Subscriber subscriber)
    {
        string Json(Subscriber s) => LiveHub.Serialize(new { type = "subscriber", subscriber = s });
        if (subscriber.Agent is not null && subscriber.Audience < Audience.Excon)
        {
            hub.Send(subscriber.ChronicleId, new LiveHub.Message(Json(SubscriberStore.ForAudience(subscriber, subscriber.Audience)), subscriber.Audience, Below: Audience.Excon));
            hub.Send(subscriber.ChronicleId, new LiveHub.Message(Json(subscriber), Audience.Excon));
        }
        else
            hub.Send(subscriber.ChronicleId, new LiveHub.Message(Json(subscriber), subscriber.Audience));
    }
}
