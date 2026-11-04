using Lazzy.Application.Validation;

namespace Lazzy.Application.Tests.Validation;

public sealed class ConfigurationProblemTests
{
    [Fact]
    public void ToStringStartsWithThePath()
    {
        var problem = new ConfigurationProblem("projects[0].name", "The project has no name.");

        Assert.Equal("projects[0].name: The project has no name.", problem.ToString());
    }
}
