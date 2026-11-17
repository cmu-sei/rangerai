// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle;
using Chronicle.Client;
using Chronicle.Client.Hosting;

namespace GhostsSubscriber;

/// <summary>
/// The app's own logic: what the NPCs do with what they hear. This one only logs what it sees, and when an inject
/// reaches the office staff, it reacts the way an NPC might: an activity for the live feed and an observation for the record.
/// </summary>
public sealed class NpcHandler(ChronicleClient chronicle, ILogger<NpcHandler> log) : IChronicleHandler
{
    public async Task HandleAsync(Entry entry, CancellationToken ct)
    {
        switch (entry.Item)
        {
            case Persona persona:
                log.LogInformation("Persona {Name} ({Role}) is ours to play.", persona.Name, persona.Role);
                break;

            case PersonaPool pool:
                log.LogInformation("Pool {Id}: {Name}.", pool.Id, pool.Name);
                break;

            case InjectDelivery inject:
                log.LogInformation("Inject {Id} reached {To}: {Note}", inject.Id, string.Join(", ", inject.To), inject.Note);
                await React(entry, inject, ct);
                break;

            default:
                log.LogInformation("#{Sequence} {Op} {Part} {Kind} {Id}", entry.Sequence, entry.Op, entry.Part, entry.Kind, entry.ItemId);
                break;
        }
    }

    private async Task React(Entry entry, InjectDelivery inject, CancellationToken ct)
    {
        // An id derived from what we react to, so handling the same entry twice (delivery is at least once) is harmless:
        // the second publish of the same item id is refused, and we let it go.
        var id = $"obs-ghosts-{inject.Id}";

        // Live feed only: what the NPC did, as it happened.
        await chronicle.ReportActivityAsync(entry.ChronicleId, new ActivityReport
        {
            Kind = "email",
            Actor = inject.To.FirstOrDefault(),
            Text = $"Opened the message from inject {inject.Id}",
            Visibility = Audience.Participant
        }, ct);

        // The record: an observation exercise control can review after the event.
        try
        {
            await chronicle.AddAsync(entry.ChronicleId, Part.AsRun, new Observation
            {
                Id = id,
                At = DateTimeOffset.UtcNow,
                By = "ghosts",
                Text = $"The office staff opened the message from {inject.Id}.",
                About = [inject.Id],
                Visibility = Audience.Excon
            }, reason: "An NPC reacted to an inject.", ct: ct);
        }
        catch (ChronicleApiException e) when (e.IsConflict || e.IsInvalid)
        {
            log.LogInformation("Already reacted to {Inject} ({Code}).", inject.Id, e.Code);
        }
    }
}
