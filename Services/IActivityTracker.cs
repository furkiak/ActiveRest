using System;

namespace ActiveRest.Services
{
    public interface IActivityTracker
    {
        double GetIdleTimeSeconds();
        bool IsMediaPlaying(); 
        string GetActiveAppName();
    }
}