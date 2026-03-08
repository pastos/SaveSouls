namespace Grabthebirdy.SaveSouls.Main.Models
{
    public class GameItem
    {
        public string Text { get; set; } = "";
        public string FolderName { get; set; } = "";
        public string SaveFileName { get; set; } = "";
        public bool UseDocuments { get; set; } = false;
        public string SteamAppId { get; set; } = "";

        public override string ToString() => Text;
    }
}
