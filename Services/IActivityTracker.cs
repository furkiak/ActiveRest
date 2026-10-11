namespace ActiveRest.Services
{
    /// <summary>Platform abstraction over "what is the user doing right now".</summary>
    public interface IActivityTracker
    {
        /// <summary>Seconds since the last keyboard or mouse input.</summary>
        double GetIdleTimeSeconds();

        /// <summary>True while the default playback device is producing sound.</summary>
        bool IsMediaPlaying();

        /// <summary>Process name of the foreground window.</summary>
        string GetActiveAppName();
    }
}
