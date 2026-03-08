using SharpHook.Native;

namespace Grabthebirdy.SaveSouls.Main.Models
{
    public class QuickSaveLoad
    {
        public int Number { get; set; }
        public string Folder { get; set; } = "";
        public KeyCode SaveKey { get; set; }
        public KeyCode LoadKey { get; set; }
    }
}
