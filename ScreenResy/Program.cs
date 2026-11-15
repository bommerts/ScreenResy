using System.Reflection;
using System.Runtime.InteropServices;

namespace ScreenResy;

internal static class AppInfo
{
    public const string Name = "ScreenResy";
    public const string Copyright = "Copyright (C) 2023-2026 Joe Farrell";
    public const string AuthorUrl = "https://www.linkedin.com/in/joefarrell/";
    public const string ProjectUrl = "https://github.com/bommerts/ScreenResy";

    /// <summary>The version from the project file, e.g. "2.0".</summary>
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "";
}

internal static class Program
{
    /// <summary>With no arguments opens the window; otherwise runs as a command-line tool.</summary>
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length > 0)
            return CommandLine.Run(args);

        CloseOwnConsole();
        Application.Run(new MainApp());
        return 0;
    }

    /// <summary>
    /// Before Windows 11 24H2 the consoleAllocationPolicy in app.manifest is ignored, so starting
    /// the app from Explorer opens a console window just for it. Being the only process attached
    /// to the console means it is that one, and not a terminal the user started the app from.
    /// </summary>
    private static void CloseOwnConsole()
    {
        if (GetConsoleProcessList(new uint[2], 2) == 1)
            FreeConsole();
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();
}
