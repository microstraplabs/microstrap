namespace Bloxstrap
{
    internal static class OverlayPaths
    {
        public static string Directory => Path.Combine(Paths.Integrations, "MicrostrapOverlay");
        public static string Script => Path.Combine(Directory, "MicrostrapOverlay.py");
        public static string Runtime => Path.Combine(Directory, "python.exe");
        public static string Launcher => Path.Combine(Directory, "MicrostrapOverlay.cmd");
    }
}
