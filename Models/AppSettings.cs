using System;

namespace ActiveRest.Models
{
    public class AppSettings
    {
        public const int MinEyeInterval = 5, MaxEyeInterval = 120;
        public const int MinIdleMinutes = 1, MaxIdleMinutes = 10;
        public const int MinGoalMinutes = 30, MaxGoalMinutes = 16 * 60;

        public bool IsEyeRestEnabled { get; set; } = true;
        public int EyeRestIntervalMinutes { get; set; } = 20;

        /// <summary>Minutes without input after which the user counts as away from the keyboard.</summary>
        public int IdleThresholdMinutes { get; set; } = 3;

        /// <summary>Daily focus target, in minutes.</summary>
        public int DailyFocusGoalMinutes { get; set; } = 6 * 60;

        /// <summary>"auto", "en" or "tr".</summary>
        public string Language { get; set; } = "auto";

        /// <summary>"system", "light" or "dark".</summary>
        public string Theme { get; set; } = "system";

        /// <summary>Clamps every value into its valid range; called after loading and after each edit.</summary>
        public void Normalize()
        {
            EyeRestIntervalMinutes = Math.Clamp(EyeRestIntervalMinutes, MinEyeInterval, MaxEyeInterval);
            IdleThresholdMinutes = Math.Clamp(IdleThresholdMinutes, MinIdleMinutes, MaxIdleMinutes);
            DailyFocusGoalMinutes = Math.Clamp(DailyFocusGoalMinutes, MinGoalMinutes, MaxGoalMinutes);
            if (Language is not ("auto" or "en" or "tr")) Language = "auto";
            if (Theme is not ("system" or "light" or "dark")) Theme = "system";
        }
    }
}
