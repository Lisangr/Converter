namespace Converter.Application.Models;

public enum SegmentEditMode
{
    KeepOnly,
    Remove
}

public sealed class TimelineSegment
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Label { get; set; }

    public TimeSpan Duration => End - Start;

    public void Validate()
    {
        if (End <= Start)
            throw new InvalidOperationException($"Invalid segment [{Start} - {End}]");
    }

    public override string ToString()
        => $"{Label ?? "Segment"} [{Start}–{End}] ({Duration})";
}