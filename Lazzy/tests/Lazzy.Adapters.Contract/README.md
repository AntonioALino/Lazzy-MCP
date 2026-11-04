# Activity source contract

`ActivitySourceContract` holds the rules every driven adapter that implements `IActivitySource` must follow.
You do not need to read its code to use it: subclass it, answer two questions, and every rule runs as a test.

## Using it

```csharp
public sealed class GitHubActivitySourceContractTests : ActivitySourceContract
{
    // Monday, November 9, 2026, 09:00 in São Paulo.
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.FromHours(-3));

    // 1. How to build the adapter: recorded fixtures and a fake clock, never the network.
    protected override IActivitySource CreateSource() =>
        new GitHubActivitySource(FixtureHttpClient.For("github/one-week-of-work"), new FakeTimeProvider(Now));

    // 2. Where the fixtures have activity: one query per kind of target the adapter reads.
    protected override IEnumerable<ActivityQuery> QueriesWithActivity =>
    [
        new(new Target("github", "my-org/checkout-api"), new TimeRange(Now.AddDays(-7), Now), new UserIdentity("octo-dev", [])),
    ];
}
```

The subclass lives in the adapter's test project, next to the adapter's own tests.
The demo adapter's subclass is in [Demo/DemoActivitySourceContractTests.cs](Demo/DemoActivitySourceContractTests.cs).

## The rules

| Test | The adapter must... |
|---|---|
| `IdIsAShortLowercaseToken` | have an `Id` of lowercase letters and digits, starting with a letter, at most 16 characters (`github`, `ado`) |
| `ReturnsActivityForARangeThatHasSome` | return events for every query in `QueriesWithActivity` |
| `NeverReturnsEventsOutsideTheRequestedRange` | return only events inside the half-open range `[From, To)`, also when the range is cut in half |
| `EveryEventIsTraceable` | set `Source` to its id, use a target whose provider is its id, give each event a reference and an absolute URL |
| `StateChangesAlwaysCarryTheNewState` | fill `ToState` on every `WorkItemStateChanged` event |
| `AnEmptyRangeReturnsNothing` | return nothing for a range with no duration, even at the exact instant of an event |

When a rule fails, its message names the event and the target, so you can find the fixture that caused it.

## Choosing the queries

- Give one query per kind of target the adapter reads: a repository, a board.
- Make each range wide enough to contain activity in the fixtures; the rules cut it in half on their own.
- If the adapter reports work item state changes, include a query that returns some, or the state rule has nothing to check.
