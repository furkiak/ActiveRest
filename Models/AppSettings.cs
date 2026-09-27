using System;

namespace ActiveRest.Models
{
    public class AppSettings
    { 
        public int EyeRestIntervalMinutes { get; set; } = 20;
        public bool IsEyeRestEnabled { get; set; } = true;
        public bool WeekStartsOnSunday { get; set; }
        public string AppearanceTheme { get; set; } = "Light";
        public int TextScalePercent { get; set; } = 100;
    }
}
