using Lazzy.Application.Configuration;
using Lazzy.Application.Validation;

namespace Lazzy.Application.Tests.Validation;

public sealed class LazzyOptionsValidatorTests
{
    private static readonly ProjectOptions Checkout = new()
    {
        Name = "Checkout",
        Aliases = ["checkout", "payments"],
        Targets = ["github:my-org/checkout-api", "ado:my-org/Checkout"],
    };

    private static readonly ProjectOptions Billing = new()
    {
        Name = "Billing",
        Aliases = ["invoices"],
        Targets = ["github:my-org/billing"],
    };

    private static LazzyOptions Options(
        string? timeZone = "America/Sao_Paulo",
        IReadOnlyList<string>? holidays = null,
        IReadOnlyList<ProjectOptions>? projects = null) => new()
        {
            TimeZone = timeZone,
            Holidays = holidays ?? ["2026-11-02", "2026-11-20"],
            Identity = new Dictionary<string, IdentityOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["github"] = new() { Login = "octo-dev", Emails = ["dev@example.com"] },
            },
            Projects = projects ?? [Checkout, Billing],
        };

    private static ConfigurationProblem SingleProblem(LazzyOptions options) =>
        Assert.Single(LazzyOptionsValidator.Validate(options));

    [Fact]
    public void AValidConfigurationReturnsNoProblems()
    {
        Assert.Empty(LazzyOptionsValidator.Validate(Options()));
    }

    [Fact]
    public void AnEmptyConfigurationIsValid()
    {
        Assert.Empty(LazzyOptionsValidator.Validate(new LazzyOptions()));
    }

    [Fact]
    public void UnknownTimeZoneIsReported()
    {
        var problem = SingleProblem(Options(timeZone: "Mars/Olympus_Mons"));

        Assert.Equal("timeZone", problem.Path);
        Assert.Contains("'Mars/Olympus_Mons'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("IANA", problem.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("02/11/2026")]
    [InlineData("2026-11-2")]
    [InlineData("2026-13-01")]
    [InlineData("")]
    [InlineData(null)] // an empty list item in YAML
    public void HolidayNotInIsoFormatIsReported(string? holiday)
    {
        var problem = SingleProblem(Options(holidays: ["2026-11-02", holiday!]));

        Assert.Equal("holidays[1]", problem.Path);
        Assert.Contains("yyyy-MM-dd", problem.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectWithoutNameIsReported(string name)
    {
        var problem = SingleProblem(Options(projects: [Checkout, Billing with { Name = name }]));

        Assert.Equal("projects[1].name", problem.Path);
    }

    [Theory]
    [InlineData("my-org/billing")]
    [InlineData("github:")]
    [InlineData(":my-org/billing")]
    public void TargetNotInProviderLocationFormatIsReported(string target)
    {
        var problem = SingleProblem(Options(projects: [Checkout, Billing with { Targets = ["github:my-org/billing", target] }]));

        Assert.Equal("projects[1].targets[1]", problem.Path);
        Assert.Contains("provider:location", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NameUsedByTwoProjectsIsReportedRegardlessOfCase()
    {
        var problem = SingleProblem(Options(projects: [Checkout, Billing with { Name = "CHECKOUT" }]));

        Assert.Equal("projects[1].name", problem.Path);
        Assert.Contains("'Checkout'", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AliasUsedByTwoProjectsIsReported()
    {
        var problem = SingleProblem(Options(projects: [Checkout, Billing with { Aliases = ["invoices", " Payments "] }]));

        Assert.Equal("projects[1].aliases[1]", problem.Path);
        Assert.Contains("'Payments'", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AliasThatIsAnotherProjectsNameIsReported()
    {
        var problem = SingleProblem(Options(projects: [Checkout, Billing with { Aliases = ["checkout"] }]));

        Assert.Equal("projects[1].aliases[0]", problem.Path);
    }

    [Fact]
    public void AProjectMayRepeatItsOwnNameAsAnAlias()
    {
        Assert.Empty(LazzyOptionsValidator.Validate(Options(projects: [Checkout with { Aliases = ["CHECKOUT", "checkout"] }])));
    }

    [Fact]
    public void BlankAliasesAreIgnored()
    {
        Assert.Empty(LazzyOptionsValidator.Validate(Options(projects: [Checkout with { Aliases = [""] }, Billing with { Aliases = [" "] }])));
    }

    [Fact]
    public void SeveralProblemsAreAllReportedInOneCall()
    {
        var options = Options(
            timeZone: "Nowhere/Land",
            holidays: ["20/11/2026"],
            projects:
            [
                Checkout with { Targets = ["checkout-api"] },
                Billing with { Name = "" },
                Billing with { Name = "Payments" },
            ]);

        var problems = LazzyOptionsValidator.Validate(options);

        Assert.Equal(
            ["timeZone", "holidays[0]", "projects[0].targets[0]", "projects[1].name", "projects[2].name", "projects[2].aliases[0]"],
            problems.Select(problem => problem.Path));
    }

    [Fact]
    public void ValidateRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => LazzyOptionsValidator.Validate(null!));
    }
}
