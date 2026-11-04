using Lazzy.Domain.Enums;

namespace Lazzy.Domain.ValueObjects;

/// <summary>One thing the user did, at one instant, on one target.</summary>
public sealed record ActivityEvent
{
    /// <summary>What was done.</summary>
    public required ActivityKind Kind
    {
        get;
        init => field = Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Use one of the values of ActivityKind.");
    }

    /// <summary>When it was done, with the offset the provider reported.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>A short human description: the first line of the commit message, the pull request or work item title.</summary>
    public required string Title
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>Where a person can open it in a browser. Always absolute.</summary>
    public required Uri Url
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value.IsAbsoluteUri
                ? value
                : throw new ArgumentException($"The URL '{value}' must be absolute, such as 'https://example.com/item/1'.", nameof(value));
        }
    }

    /// <summary>The id of the activity source that reported it, such as <c>github</c>.</summary>
    public required string Source
    {
        get;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    }

    /// <summary>The configured target it happened on.</summary>
    public required Target Target
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>How people refer to it: a short commit hash, <c>#42</c>, a work item id.</summary>
    public required string Reference
    {
        get;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    }

    /// <summary>The state before the change, for <see cref="ActivityKind.WorkItemStateChanged"/>.</summary>
    public string? FromState { get; init; }

    /// <summary>The state after the change, for <see cref="ActivityKind.WorkItemStateChanged"/>.</summary>
    public string? ToState { get; init; }
}
