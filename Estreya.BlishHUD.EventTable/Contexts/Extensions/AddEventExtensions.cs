namespace Estreya.BlishHUD.EventTable.Contexts.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class AddEventExtensions
{
    /// <summary>
    /// Generates a filler event between the current event and another.
    /// </summary>
    /// <param name="currentEvent">The current event.</param>
    /// <param name="nextEvent">The next event. Can be the same as current if it just has more occurences.</param>
    /// <param name="fromOccurenceIndex">The start occurence for the filler from the current event <paramref name="currentEvent"/></param>
    /// <param name="toOccurenceIndex">The next (end) occurence for the filler from the next event</param>
    /// <returns>The generated filler event.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static AddEvent GenerateEventFiller(this AddEvent currentEvent, AddEvent nextEvent, int fromOccurenceIndex = 0, int toOccurenceIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(currentEvent.CategoryKey)) throw new ArgumentException("CategoryKey of the current event is empty or null.");
        if (string.IsNullOrWhiteSpace(nextEvent.CategoryKey)) throw new ArgumentException("CategoryKey of the next event is empty or null.");
        if (currentEvent.CategoryKey != nextEvent.CategoryKey) throw new ArgumentException("CategoryKey of events is not the same.");
        if (string.IsNullOrWhiteSpace(currentEvent.Key)) throw new ArgumentException("Key of the current event is empty or null.");
        if (string.IsNullOrWhiteSpace(nextEvent.Key)) throw new ArgumentException("Key of the next event is empty or null.");
        if (currentEvent.Occurences is null || currentEvent.Occurences.Count < toOccurenceIndex) throw new ArgumentOutOfRangeException(nameof(fromOccurenceIndex), $"Current event has no occurence at index {fromOccurenceIndex}");
        if (nextEvent.Occurences is null || nextEvent.Occurences.Count < toOccurenceIndex) throw new ArgumentOutOfRangeException(nameof(toOccurenceIndex), $"Next event has no occurence at index {toOccurenceIndex}");

        AddEvent eventFiller = new AddEvent
        {
            CategoryKey = currentEvent.CategoryKey,
            Key = $"{currentEvent.Key}-{nextEvent.Key}_{Guid.NewGuid()}",
            Name = $"{currentEvent.Name} - {nextEvent.Name}",
            Duration = Math.Max(0, (int)(nextEvent.Occurences[toOccurenceIndex] - currentEvent.Occurences[fromOccurenceIndex]).TotalMinutes),
            Filler = true,
            Occurences = new List<DateTime>()
            {
                currentEvent.Occurences[fromOccurenceIndex].AddMinutes(currentEvent.Duration)
            }
        };

        return eventFiller;
    }

    /// <summary>
    /// Generates a filler event between the occurences of the current event.
    /// </summary>
    /// <param name="currentEvent">The current event.</param>
    /// <param name="fromOccurenceIndex">The start occurence for the filler.</param>
    /// <param name="toOccurenceIndex">The next (end) occurence for the filler.</param>
    /// <returns>The generated filler event.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static AddEvent GenerateEventFiller(this AddEvent currentEvent, int fromOccurenceIndex = 0, int toOccurenceIndex = 1)
    {
        AddEvent eventFiller = currentEvent.GenerateEventFiller(currentEvent, fromOccurenceIndex, toOccurenceIndex);

        return eventFiller;
    }

    /// <summary>
    /// Generates filler events between all occurences of this event.
    /// </summary>
    /// <param name="currentEvent">The current event.</param>
    /// <returns>A list of generated filler events.</returns>
    public static IEnumerable<AddEvent> GenerateEventFillers(this AddEvent currentEvent)
    {
        List<AddEvent> filler = new List<AddEvent>();

        if (currentEvent.Occurences == null) return filler;

        for (int i = 0; i < currentEvent.Occurences.Count -1; i++)
        {
            var fromIndex = i;
            var toIndex = i + 1;

            filler.Add(currentEvent.GenerateEventFiller(fromIndex, toIndex));
        }

        return filler;
    }
}
