using Lazzy.Domain.ValueObjects;

namespace Lazzy.Application.Models;

/// <summary>The user's activity across the selected projects.</summary>
/// <param name="Range">The range that was read.</param>
/// <param name="Events">Every event inside the range, without duplicates, oldest first.</param>
/// <param name="Warnings">What could not be read and why, so the answer can say the report is partial.</param>
public sealed record ActivityReport(TimeRange Range, IReadOnlyList<ActivityEvent> Events, IReadOnlyList<string> Warnings);
