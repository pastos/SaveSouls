using System.Collections.Generic;

namespace Grabthebirdy.SaveSouls.Main.Models
{
    public class AppSettings
    {
        /// <summary>
        /// Stores the last-used save file path keyed by game name (e.g. "Dark Souls 3").
        /// </summary>
        public Dictionary<string, string> GameSavePaths { get; set; } = new();
        public string LastSelectedGame { get; set; } = "";
    }
}
