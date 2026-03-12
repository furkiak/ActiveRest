namespace ActiveRest.Models
{
    public class AppUsageItem
    {
        public string AppName { get; set; } = string.Empty;
        public string DurationText { get; set; } = string.Empty;
        public double TotalSeconds { get; set; } 
        public string PercentageText { get; set; } = string.Empty;
    }
}