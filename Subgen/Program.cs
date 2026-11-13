namespace SubGen;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        // Optional first argument: a video path (used by the right-click menu).
        Application.Run(new MainForm(args.Length > 0 ? args[0] : null));
    }
}
