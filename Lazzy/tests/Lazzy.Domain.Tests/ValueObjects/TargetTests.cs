using Lazzy.Domain.ValueObjects;

namespace Lazzy.Domain.Tests.ValueObjects;

public sealed class TargetTests
{
    [Fact]
    public void ParsesProviderAndLocation()
    {
        Assert.True(Target.TryParse("github:my-org/my-repo", out var target));

        Assert.Equal("github", target.Provider);
        Assert.Equal("my-org/my-repo", target.Location);
    }

    [Fact]
    public void OnlyTheFirstColonSeparatesTheProvider()
    {
        Assert.True(Target.TryParse("git:https://git.example.com/team/repo", out var target));

        Assert.Equal("git", target.Provider);
        Assert.Equal("https://git.example.com/team/repo", target.Location);
    }

    [Fact]
    public void TrimsSpacesAroundEachPartButKeepsSpacesInsideTheLocation()
    {
        Assert.True(Target.TryParse("  ado :  my-org/My Project  ", out var target));

        Assert.Equal("ado", target.Provider);
        Assert.Equal("my-org/My Project", target.Location);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("github")]
    [InlineData("github/my-org/my-repo")]
    [InlineData("github:")]
    [InlineData("github:   ")]
    [InlineData(":my-org/my-repo")]
    [InlineData("git hub:my-org/my-repo")]
    public void RejectsValuesNotInProviderLocationFormat(string? value)
    {
        Assert.False(Target.TryParse(value, out var target));
        Assert.Null(target);
    }

    [Fact]
    public void ToStringWritesProviderColonLocation()
    {
        Assert.Equal("ado:my-org/My Project", new Target(" ado ", " my-org/My Project ").ToString());
    }

    [Fact]
    public void TargetsWithTheSamePartsAreEqual()
    {
        Assert.True(Target.TryParse("github:my-org/my-repo", out var parsed));

        Assert.Equal(new Target("github", "my-org/my-repo"), parsed);
    }

    [Theory]
    [InlineData("", "my-org/my-repo", "provider")]
    [InlineData("github", " ", "location")]
    [InlineData("git hub", "my-org/my-repo", "provider")]
    [InlineData("git:hub", "my-org/my-repo", "provider")]
    public void ConstructorRejectsInvalidParts(string provider, string location, string parameterName)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => new Target(provider, location));

        Assert.Equal(parameterName, exception.ParamName);
    }
}
