namespace ActiveRest.Services
{
    /// <summary>Fallback for platforms without a native tracker: the user is always considered active.</summary>
    public sealed class NullActivityTracker : IActivityTracker
    {
        public double GetIdleTimeSeconds() => 0;
        public bool IsMediaPlaying() => false;
        public string GetActiveAppName() => "Desktop";
    }
}
