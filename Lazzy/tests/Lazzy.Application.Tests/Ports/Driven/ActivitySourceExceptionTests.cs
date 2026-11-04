using Lazzy.Application.Ports.Driven;

namespace Lazzy.Application.Tests.Ports.Driven;

public sealed class ActivitySourceExceptionTests
{
    [Fact]
    public void HasADefaultMessage()
    {
        Assert.False(string.IsNullOrWhiteSpace(new ActivitySourceException().Message));
    }

    [Fact]
    public void KeepsTheMessageForTheUser()
    {
        Assert.Equal("The token was rejected.", new ActivitySourceException("The token was rejected.").Message);
    }

    [Fact]
    public void KeepsTheCause()
    {
        var cause = new HttpRequestException("401");

        var exception = new ActivitySourceException("The token was rejected.", cause);

        Assert.Equal("The token was rejected.", exception.Message);
        Assert.Same(cause, exception.InnerException);
    }
}
