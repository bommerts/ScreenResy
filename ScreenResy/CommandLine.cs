namespace ScreenResy;

internal static class CommandLine
{
    public const int Success = 0;
    public const int InvalidArguments = 1;
    public const int ChangeFailed = 2;

    private const string Usage = """
        Usage:
          ScreenResy                                  Open the ScreenResy window
          ScreenResy [-x <width> -y <height>] [-r <hz>] [-d <percent>] [-m <display>]
          ScreenResy --list

        Options:
          -x, --width <pixels>     Horizontal resolution. Use together with -y.
          -y, --height <pixels>    Vertical resolution. Use together with -x.
          -r, --refresh <hz>       Refresh rate. Without it, Windows picks one.
          -d, --scale <percent>    Display scale: 100, 125, 150, 175, 200, ...
          -m, --monitor <number>   Display to change (see --list). Default: the primary display.
          -l, --list               Show each display, its current settings and supported modes.
          -h, --help               Show this help.
          -v, --version            Show the version.

        Examples:
          ScreenResy -x 1920 -y 1080 -d 125
          ScreenResy -x 2560 -y 1440 -r 60 -m 2
          ScreenResy -d 150

        Exit codes:
          0  success    1  invalid arguments    2  Windows refused the change
        """;

    private sealed class Options
    {
        public int? Width, Height, Refresh, Scale, Monitor;
        public bool List, Help, Version;
    }

    public static int Run(string[] args)
    {
        Options options;
        try
        {
            options = Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"{AppInfo.Name}: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(Usage);
            return InvalidArguments;
        }

        if (options.Help)
        {
            Console.WriteLine($"{AppInfo.Name} {AppInfo.Version} - change screen resolution, refresh rate and scale.");
            Console.WriteLine($"{AppInfo.Copyright} - {AppInfo.AuthorUrl}");
            Console.WriteLine(AppInfo.ProjectUrl);
            Console.WriteLine();
            Console.WriteLine(Usage);
            return Success;
        }

        if (options.Version)
        {
            Console.WriteLine($"{AppInfo.Name} {AppInfo.Version}");
            return Success;
        }

        try
        {
            IReadOnlyList<Display> displays = Display.GetAll();

            if (options.List)
            {
                List(displays);
                return Success;
            }

            Display? display = options.Monitor is int number
                ? displays.FirstOrDefault(d => d.Number == number)
                : displays.FirstOrDefault(d => d.IsPrimary) ?? displays[0];
            if (display is null)
            {
                Console.Error.WriteLine($"{AppInfo.Name}: there is no display {options.Monitor}. Available: {string.Join(", ", displays.Select(d => d.Number))}.");
                return InvalidArguments;
            }

            Apply(display, options);
            return Success;
        }
        catch (DisplayException ex)
        {
            Console.Error.WriteLine($"{AppInfo.Name}: {ex.Message}");
            return ChangeFailed;
        }
    }

    private static void Apply(Display display, Options options)
    {
        // Catch a mistyped scale before the resolution is changed, not after.
        if (options.Scale is int scale && !Display.ScaleSteps.Contains(scale))
            throw new DisplayException($"{scale}% is not a scale Windows offers. Use one of: {string.Join(", ", Display.ScaleSteps)}.");

        if (options.Width is not null || options.Refresh is not null)
        {
            DisplayMode current = display.GetCurrentMode();
            display.SetMode(options.Width ?? current.Width, options.Height ?? current.Height, options.Refresh, persist: true);

            DisplayMode now = display.GetCurrentMode();
            Console.WriteLine($"Display {display.Number}: resolution set to {now.Width}x{now.Height} at {now.Frequency} Hz");
        }

        // After the resolution, because the highest scale a display allows depends on it.
        if (options.Scale is int percent)
        {
            display.SetScale(percent);
            Console.WriteLine($"Display {display.Number}: scale set to {percent}%");
        }
    }

    private static void List(IReadOnlyList<Display> displays)
    {
        foreach (Display display in displays)
        {
            DisplayMode current = display.GetCurrentMode();
            Console.WriteLine($"Display {display.Number}: {display.FriendlyName}{(display.IsPrimary ? " (primary)" : "")}");
            Console.WriteLine($"  Current:      {current.Width}x{current.Height} at {current.Frequency} Hz");
            try
            {
                ScaleInfo scale = display.GetScale();
                Console.WriteLine($"  Scale:        {scale.Current}% (recommended {scale.Recommended}%, maximum {scale.Maximum}%)");
            }
            catch (DisplayException)
            {
                Console.WriteLine("  Scale:        not available");
            }

            Console.WriteLine("  Resolutions:");
            foreach (var resolution in display.GetModes().GroupBy(m => (m.Width, m.Height)))
            {
                string size = $"{resolution.Key.Width}x{resolution.Key.Height}";
                Console.WriteLine($"    {size,-12} {string.Join(", ", resolution.Select(m => m.Frequency))} Hz");
            }
            Console.WriteLine();
        }
    }

    private static Options Parse(string[] args)
    {
        var options = new Options();

        for (int i = 0; i < args.Length; i++)
        {
            string option = args[i];
            switch (option.ToLowerInvariant())
            {
                case "-x" or "--width": options.Width = Value(); break;
                case "-y" or "--height": options.Height = Value(); break;
                case "-r" or "--refresh": options.Refresh = Value(); break;
                case "-d" or "--scale" or "--dpi": options.Scale = Value(); break;
                case "-m" or "--monitor" or "--display": options.Monitor = Value(); break;
                case "-l" or "--list": options.List = true; break;
                case "-h" or "--help" or "-?" or "/?": options.Help = true; break;
                case "-v" or "--version": options.Version = true; break;
                default: throw new ArgumentException($"unknown option '{option}'.");
            }

            int Value()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"{option} needs a value.");
                string text = args[++i];
                if (!int.TryParse(text, out int value) || value <= 0)
                    throw new ArgumentException($"'{text}' is not a valid value for {option}; it must be a whole number above zero.");
                return value;
            }
        }

        if (options.Help || options.Version || options.List)
            return options;

        if ((options.Width is null) != (options.Height is null))
            throw new ArgumentException("-x and -y must be used together.");
        if (options.Width is null && options.Refresh is null && options.Scale is null)
            throw new ArgumentException("nothing to change. Give a resolution (-x and -y), a refresh rate (-r) or a scale (-d).");

        return options;
    }
}
