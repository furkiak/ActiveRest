using System;

namespace ActiveRest.Services
{
    public static class ActivityTrackerFactory
    {
        public static IActivityTracker Create() =>
            OperatingSystem.IsWindows() ? new WindowsActivityTracker() : new NullActivityTracker();
    }
}
