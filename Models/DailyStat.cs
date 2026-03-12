using System;
using System.Collections.Generic;

namespace ActiveRest.Models
{
    public class DailyStat
    {
        public DateTime Date { get; set; }
        public DateTime FirstBootTime { get; set; } 
        public double TotalFocusedSeconds { get; set; }
        public double TotalRelaxedSeconds { get; set; }
        public double TotalBreakSeconds { get; set; } 
        public Dictionary<string, double> AppUsageSeconds { get; set; }

        public DailyStat()
        {
            Date = DateTime.Today;
            FirstBootTime = DateTime.Now;
            AppUsageSeconds = new Dictionary<string, double>();  
        }
    }
}