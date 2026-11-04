# Events and metrics

Every number Lazzy reports is computed by code from the events below. The model never counts.

## Events

An event is one thing the user did, at one instant, on one configured target.
Only events inside the requested range `[from, to)` are reported: an event exactly at `to` belongs to the next range.

| Kind | Counted when | Timestamp |
|---|---|---|
| `CommitAuthored` | the user is the commit author (by login, or by one of `identity.<provider>.emails`) | author date |
| `PullRequestOpened` | the user opened the pull request | creation time |
| `PullRequestMerged` | a pull request opened by the user was merged | merge time |
| `PullRequestReviewed` | the user submitted a review on someone else's pull request; each review counts | submission time |
| `WorkItemStateChanged` | the user changed the state of a work item; records the state before and after | change time |

Every event carries the source that reported it, the target it happened on, a reference people use for it
(a short commit hash, `#42`, a work item id) and an absolute URL.

## Merging answers

- Each target is read once, even when several projects list it.
- Two events are the same when they have the same kind, URL and instant; duplicates are reported once.
- Events are ordered oldest first; ties are broken by kind, then by URL.
- A target that cannot be read becomes a warning, and the report still contains everything else.
