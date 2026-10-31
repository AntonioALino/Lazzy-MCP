namespace Lazzy.Abstractions;

/// <summary>
/// A half-open interval of time: it includes <see cref="From"/> and excludes <see cref="To"/>.
/// An event exactly at <see cref="To"/> belongs to the next range, so it is never counted twice.
/// </summary>

public readonly record struct TimeRange
{
    public TimeRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (to < from)
        {
            throw new ArgumentException("The end of the range must not be earlier than its start.", nameof(to));
        }
        
        From = from;
        To = to;
    }
    
    /// <summary>Start of the range, inclusive.</summary>
    public DateTimeOffset From { get; }
    
    /// <summary>End of the range, exclusive.</summary>
    public DateTimeOffset To { get; }
    
    /// <summary>True when the range has no duration and therefore contains nothing.</summary>
    public bool IsEmpty => From == To;

    /// <summary>True when <paramref name="instant"/> is inside the range.</summary>
    public bool Contains(DateTimeOffset instant) => instant >= From && instant < To;
}