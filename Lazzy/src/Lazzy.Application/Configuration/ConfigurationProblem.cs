namespace Lazzy.Application.Configuration;

/// <summary>One thing wrong with the configuration.</summary>
/// <param name="Path">Where the problem is, such as <c>projects[1].targets[0]</c>.</param>
/// <param name="Message">What is wrong and how to fix it.</param>
public sealed record ConfigurationProblem(string Path, string Message)
{
    /// <summary>The problem as one line: <c>path: message</c>.</summary>
    public override string ToString() => $"{Path}: {Message}";
}
