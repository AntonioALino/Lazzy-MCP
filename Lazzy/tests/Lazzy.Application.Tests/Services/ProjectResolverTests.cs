using Lazzy.Application.Configuration;
using Lazzy.Application.Services;

namespace Lazzy.Application.Tests.Services;

public sealed class ProjectResolverTests
{
    private static readonly ProjectOptions Checkout = new()
    {
        Name = "Checkout",
        Aliases = ["payments", "pagamentos"],
        Targets = ["github:my-org/checkout-api"],
    };

    private static readonly ProjectOptions Billing = new()
    {
        Name = "Billing",
        Aliases = ["invoices"],
        Targets = ["github:my-org/billing"],
    };

    private static ProjectResolver Resolver(params ProjectOptions[] projects) =>
        new(new LazzyOptions { Projects = projects.Length == 0 ? [Checkout, Billing] : projects });

    [Fact]
    public void ResolvesByName()
    {
        Assert.Same(Billing, Resolver().Resolve("Billing"));
    }

    [Fact]
    public void ResolvesByAlias()
    {
        Assert.Same(Checkout, Resolver().Resolve("pagamentos"));
    }

    [Theory]
    [InlineData("checkout")]
    [InlineData("CHECKOUT")]
    [InlineData("Payments")]
    [InlineData("  payments  ")]
    public void IgnoresCaseAndSurroundingSpaces(string nameOrAlias)
    {
        Assert.Same(Checkout, Resolver().Resolve(nameOrAlias));
    }

    [Theory]
    [InlineData("Shipping")]
    [InlineData("check")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UnknownNameReturnsNothing(string? nameOrAlias)
    {
        Assert.Null(Resolver().Resolve(nameOrAlias));
    }

    [Fact]
    public void BlankNamesAndAliasesNeverMatch()
    {
        var nameless = Checkout with { Name = " ", Aliases = ["", "payments"] };

        var resolver = Resolver(nameless);

        Assert.Null(resolver.Resolve(" "));
        Assert.Same(nameless, resolver.Resolve("payments"));
    }

    [Fact]
    public void TheFirstProjectKeepsANameThatClashes()
    {
        var clash = Billing with { Aliases = ["checkout"] };

        Assert.Same(Checkout, Resolver(Checkout, clash).Resolve("checkout"));
    }

    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectResolver(null!));
    }
}
