namespace Lazzy.Domain.Enums;

/// <summary>What a person did. See docs/metrics.md for the exact definition of each kind.</summary>
public enum ActivityKind
{
    /// <summary>A commit whose author is the user.</summary>
    CommitAuthored,

    /// <summary>A pull request opened by the user.</summary>
    PullRequestOpened,

    /// <summary>A pull request opened by the user that was merged.</summary>
    PullRequestMerged,

    /// <summary>A review the user submitted on someone else's pull request.</summary>
    PullRequestReviewed,

    /// <summary>A work item whose state the user changed.</summary>
    WorkItemStateChanged,
}
