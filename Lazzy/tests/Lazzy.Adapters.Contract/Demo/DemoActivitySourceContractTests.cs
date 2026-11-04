using Lazzy.Adapters.Demo;
using Lazzy.Application.Ports.Driven;
using Lazzy.Domain.Services;
using Lazzy.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Lazzy.Adapters.Contract.Demo;

public sealed class DemoActivitySourceContractTests : ActivitySourceContract
{
    // Monday, November 9, 2026, 09:00 in São Paulo.
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.FromHours(-3));

    protected override IEnumerable<ActivityQuery> QueriesWithActivity =>
        DemoActivitySource.Targets.Select(target => new ActivityQuery(target, new TimeRange(Now.AddDays(-14), Now), null));

    protected override IActivitySource CreateSource() =>
        new DemoActivitySource(new FakeTimeProvider(Now), TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"), BusinessCalendar.WeekendsOnly);
}
