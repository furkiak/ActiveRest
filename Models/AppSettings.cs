using System;

namespace ActiveRest.Models
{
    public class AppSettings
    { 
        public int EyeRestIntervalMinutes { get; set; } = 20;
        public bool IsEyeRestEnabled { get; set; } = true;
    }
}