namespace Lazzy.Application.Ports.Driven;

/// <summary>
/// An expected failure of an activity source: rejected token, target not found, rate limit.
/// The message is shown to the user, so it must say what to do.
/// </summary>
public sealed class ActivitySourceException : Exception
{
    public ActivitySourceException()
        : base("The activity source failed.")
    {
    }

    public ActivitySourceException(string message)
        : base(message)
    {
    }

    public ActivitySourceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
