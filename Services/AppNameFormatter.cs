using System;
using System.Collections.Generic;

namespace ActiveRest.Services
{
    /// <summary>Turns raw process names into friendly display names and gives each app a stable accent hue.</summary>
    public static class AppNameFormatter
    {
        private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "Google Chrome",
            ["msedge"] = "Microsoft Edge",
            ["firefox"] = "Firefox",
            ["opera"] = "Opera",
            ["brave"] = "Brave",
            ["code"] = "Visual Studio Code",
            ["devenv"] = "Visual Studio",
            ["rider64"] = "JetBrains Rider",
            ["idea64"] = "IntelliJ IDEA",
            ["pycharm64"] = "PyCharm",
            ["explorer"] = "File Explorer",
            ["winword"] = "Microsoft Word",
            ["excel"] = "Microsoft Excel",
            ["powerpnt"] = "Microsoft PowerPoint",
            ["outlook"] = "Microsoft Outlook",
            ["onenote"] = "Microsoft OneNote",
            ["teams"] = "Microsoft Teams",
            ["ms-teams"] = "Microsoft Teams",
            ["slack"] = "Slack",
            ["discord"] = "Discord",
            ["telegram"] = "Telegram",
            ["whatsapp"] = "WhatsApp",
            ["spotify"] = "Spotify",
            ["notepad"] = "Notepad",
            ["notepad++"] = "Notepad++",
            ["windowsterminal"] = "Windows Terminal",
            ["powershell"] = "PowerShell",
            ["pwsh"] = "PowerShell",
            ["cmd"] = "Command Prompt",
            ["applicationframehost"] = "Windows Apps",
            ["steam"] = "Steam",
            ["figma"] = "Figma",
            ["postman"] = "Postman",
            ["obsidian"] = "Obsidian",
            ["notion"] = "Notion",
            ["zoom"] = "Zoom",
            ["vlc"] = "VLC",
        };

        public static string Pretty(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "Unknown";
            return Known.TryGetValue(key, out var friendly) ? friendly : key;
        }

        public static string Initial(string display)
        {
            foreach (char c in display)
                if (char.IsLetterOrDigit(c)) return char.ToUpperInvariant(c).ToString();
            return "?";
        }

        /// <summary>Deterministic hue in [0, 360) so the same app always gets the same colour across runs.</summary>
        public static double Hue(string key)
        {
            unchecked
            {
                uint hash = 2166136261;                       // FNV-1a
                foreach (char c in key.ToLowerInvariant()) hash = (hash ^ c) * 16777619;
                hash ^= hash >> 15; hash *= 2246822519;       // avalanche so similar names land far apart
                hash ^= hash >> 13;
                return hash % 360;
            }
        }
    }
}
