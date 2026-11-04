using Lazzy.Domain.Enums;

namespace Lazzy.Adapters.Demo;

/// <summary>One step of the demo story, placed in business days before today.</summary>
/// <param name="BusinessDaysAgo">1 is the previous business day, 2 the one before, and so on.</param>
/// <param name="At">Local time of day in the configured time zone.</param>
/// <param name="Kind">What the user did.</param>
/// <param name="Location">The demo location it happened on: <see cref="DemoStory.CodeLocation"/> or <see cref="DemoStory.BoardLocation"/>.</param>
/// <param name="Title">The commit message, pull request or work item title.</param>
/// <param name="Path">Where it lives under the location, used to build its URL.</param>
/// <param name="Reference">How people refer to it.</param>
/// <param name="FromState">The state before the change, for work item state changes.</param>
/// <param name="ToState">The state after the change, for work item state changes.</param>
internal sealed record DemoStep(
    int BusinessDaysAgo,
    TimeOnly At,
    ActivityKind Kind,
    string Location,
    string Title,
    string Path,
    string Reference,
    string? FromState = null,
    string? ToState = null);

/// <summary>
/// The scripted story of a small online shop: a discount code feature goes from To Do to Ready to Test
/// with its commits, pull request, review round and merge; a second task is started; the user reviews
/// colleagues' pull requests. Every name, hash and URL is fictional.
/// </summary>
internal static class DemoStory
{
    public const string CodeLocation = "example-shop/storefront";
    public const string BoardLocation = "example-shop/board";

    public static Uri BaseUrl { get; } = new("https://demo.example.com/");

    public static IReadOnlyList<DemoStep> Steps { get; } =
    [
        new(5, new(10, 0), ActivityKind.CommitAuthored, CodeLocation, "Upgrade the payment SDK", "commit/8d1e4b7", "8d1e4b7"),

        new(3, new(9, 30), ActivityKind.WorkItemStateChanged, BoardLocation, "Discount codes at checkout", "items/101", "#101", "To Do", "Doing"),
        new(3, new(11, 10), ActivityKind.CommitAuthored, CodeLocation, "Add a discount code field to checkout", "commit/3f9c2a1", "3f9c2a1"),
        new(3, new(16, 20), ActivityKind.CommitAuthored, CodeLocation, "Validate discount codes against active campaigns", "commit/a72b0e5", "a72b0e5"),

        new(2, new(10, 5), ActivityKind.PullRequestOpened, CodeLocation, "Discount codes at checkout", "pull/42", "#42"),
        new(2, new(15, 0), ActivityKind.PullRequestReviewed, CodeLocation, "Fix the cart badge count", "pull/41#review-1", "#41"),

        new(1, new(9, 40), ActivityKind.PullRequestReviewed, CodeLocation, "Faster product search", "pull/40#review-2", "#40"),
        new(1, new(11, 20), ActivityKind.CommitAuthored, CodeLocation, "Round discounts down, as asked in review", "commit/c4e19d8", "c4e19d8"),
        new(1, new(14, 0), ActivityKind.PullRequestMerged, CodeLocation, "Discount codes at checkout", "pull/42", "#42"),
        new(1, new(14, 5), ActivityKind.WorkItemStateChanged, BoardLocation, "Discount codes at checkout", "items/101", "#101", "Doing", "Ready to Test"),
        new(1, new(15, 30), ActivityKind.WorkItemStateChanged, BoardLocation, "Show the order history", "items/102", "#102", "To Do", "Doing"),
        new(1, new(16, 45), ActivityKind.CommitAuthored, CodeLocation, "Add an empty order history page", "commit/e05f3b9", "e05f3b9"),
    ];
}
