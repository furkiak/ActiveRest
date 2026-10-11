namespace ActiveRest.Models
{
    /// <summary>One bar of the trend chart.</summary>
    public sealed record ChartBar(
        string Label,
        double Seconds,
        bool ShowLabel,
        bool IsCurrent,
        bool MetGoal);
}
