using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Utility;

/*
 * Okay, so, what is this?
 * As far as I can tell, there currently doesn't exist a way to delay an event until some specific timestamp.
 * The best you can do is to store the event somewhere and then constantly check the timestamp in the update function.
 * This has performance concerns. It's best to use update as little as possible, even on relatively simple components that there will be few instances of.
 * But my component, specifically the slime extract component, is terrible. It is complex and there's going to be a lot of instances.
 * So I need to get rid of the update function. But then how do you do delays? I somehow need to raise an event at a specific timestamp.
 * As far as I can tell, there is no such thing. So we need to make it.
 *
 * TimedEventSystem does one thing and only one thing: It holds onto events and sends them at their relevant timestamps.
 * This is fast thanks to the use of a priority queue: The events with the lowest timestamps are executed first.
 * If we ever bump into a timestamp that exceeds the current server time (i.e. its an event that should be sent later),
 * the queue stops being processed. This way, we only process the events that need processing and defer the rest.
 * Thanks to the nature of the priority queue, this early stop is guaranteed to defer ONLY the events that are later and
 * ignore the rest.
 * Further, a well-implemented priority queue has a min-delete time of O(log n), and min-delete is precisely what we want.
 * And this means that performance will not significantly worsen as the number of events increases.
 * Though if performance is a probem with the TimedEventSystem, we can write different "tiers" of queues that are
 * processed at different times.
 * Tier 0 would be processed every tick, Tier 1 would be processed every second, Tier 2 would be processed every 10 seconds,
 * and each event will be shifted down to the lower tier below some threshold.
 * But I don't think this will be necessary.
 */

public record TimedEventRecord(EntityUid Uid, object Args, bool Broadcast, TimeSpan TimeStamp, Guid Guid) : IComparable<TimedEventRecord>
{
    public bool Cancelled { get; set; } = false;

    public int CompareTo(TimedEventRecord? other)
    {
        // RT priority queues sort from highest to lowest, so this needs to be inverted so that the "smaller" times are higher priority
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return -1;
        return other.TimeStamp.CompareTo(TimeStamp);
    }
};

/// <summary>
/// Handles events that are deferred to another point in time.
/// THIS DOES NOT HANDLE PAUSING. Use the returned Guid from ScheduleEvent to cancel a timed event and then to restart it once unpaused.
/// </summary>
public sealed class TimedEventSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    private readonly PriorityQueue<TimedEventRecord> _eventDictionary = new();
    // To prevent infinite loops within a single frame, and to prevent modifying collections,
    // new events are deferred to the next frame
    // I use a queue here as it has O(1) addition and removal time
    private readonly Queue<TimedEventRecord> _incomingEventQueue = new();
    // To keep track of events in case of deletion, we also need a HashMap
    // We do not cycle through this
    private readonly Dictionary<Guid, TimedEventRecord> _eventRecords = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        while (_incomingEventQueue.TryDequeue(out var eventRecord))
            _eventDictionary.Add(eventRecord);
        // Now this is where the magic happens
        while (_eventDictionary.Count > 0)
        {
            var timedEventRecordTest = _eventDictionary.Peek();
            if (timedEventRecordTest.TimeStamp <= _gameTiming.CurTime)
            {
                var timedEventRecord = _eventDictionary.Take();
                if (!timedEventRecord.Cancelled)
                    RaiseLocalEvent(timedEventRecord.Uid, timedEventRecord.Args, timedEventRecord.Broadcast);
                _eventRecords.Remove(timedEventRecord.Guid);
            }
            else break;
        }
    }

    /// <summary>
    /// Schedules a local event to occur at a future point in time.
    /// IMPORTANT! This returns a Guid. KEEP TRACK OF IT.
    /// If you need to cancel a timed event before it has occurred, you use the Guid.
    /// </summary>
    public Guid ScheduleEvent(EntityUid uid, object args, TimeSpan timeStamp, bool broadcast = false)
    {
        var guid = Guid.NewGuid();
        var record = new TimedEventRecord(uid, args, broadcast, timeStamp, guid);
        _incomingEventQueue.Enqueue(record);
        _eventRecords.Add(guid, record);
        return guid;
    }

    /// <summary>
    /// Gets a scheduled event by guid. USE THE GUID OBTAINED FROM ScheduleEvent.
    /// </summary>
    public bool TryGetEvent(Guid guid, [NotNullWhen(true)] out TimedEventRecord? record)
    {
        if (_eventRecords.TryGetValue(guid, out var innerRecord) && !innerRecord.Cancelled)
        {
            record = innerRecord;
            return true;
        }

        record = null;
        return false;
    }

    /// <summary>
    /// Deletes a scheduled event by guid and then returns it. USE THE GUID OBTAINED FROM ScheduleEvent.
    /// </summary>
    public bool TryDeleteEvent(Guid guid, [NotNullWhen(true)] out TimedEventRecord? record)
    {
        if (_eventRecords.TryGetValue(guid, out var innerRecord) && !innerRecord.Cancelled)
        {
            innerRecord.Cancelled = true;
            record = innerRecord;
            return true;
        }
        
        record = null;
        return false;
    }
}