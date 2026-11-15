using System.Runtime.InteropServices;

namespace ScreenResy;

/// <summary>A display change that Windows or the driver refused, with a message fit to show the user.</summary>
internal sealed class DisplayException(string message) : Exception(message);

internal readonly record struct DisplayMode(int Width, int Height, int Frequency);

/// <summary>Scale percentages for one display. The maximum depends on its current resolution.</summary>
internal readonly record struct ScaleInfo(int Current, int Recommended, int Maximum)
{
    public IEnumerable<int> Supported
    {
        get
        {
            int maximum = Maximum;
            return Display.ScaleSteps.TakeWhile(step => step <= maximum);
        }
    }
}

/// <summary>One active display: reads and changes its resolution, refresh rate and scale.</summary>
internal sealed class Display
{
    /// <summary>Every scale Windows offers. A display supports the steps up to its maximum.</summary>
    public static readonly int[] ScaleSteps = [100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500];

    private readonly LUID adapterId;
    private readonly uint sourceId;

    private Display(string deviceName, string friendlyName, LUID adapterId, uint sourceId)
    {
        DeviceName = deviceName;
        FriendlyName = friendlyName;
        this.adapterId = adapterId;
        this.sourceId = sourceId;

        // "\\.\DISPLAY2" -> 2
        string digits = new(deviceName.Where(char.IsAsciiDigit).ToArray());
        Number = int.TryParse(digits, out int number) ? number : 0;

        // Windows always places the primary display at the origin of the desktop.
        DEVMODE current = ReadCurrentMode();
        IsPrimary = current.dmPositionX == 0 && current.dmPositionY == 0;
    }

    /// <summary>The number in the GDI device name, e.g. 2 for \\.\DISPLAY2.</summary>
    public int Number { get; }
    public string DeviceName { get; }
    public string FriendlyName { get; }
    public bool IsPrimary { get; }

    public override string ToString() => $"{Number}: {FriendlyName}{(IsPrimary ? " (primary)" : "")}";

    public static IReadOnlyList<Display> GetAll()
    {
        var displays = new List<Display>();
        foreach (DISPLAYCONFIG_PATH_INFO path in QueryActivePaths())
        {
            var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
            {
                header = Header(DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                    Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(), path.sourceInfo.adapterId, path.sourceInfo.id),
            };
            if (DisplayConfigGetDeviceInfo(ref source) != ERROR_SUCCESS)
                continue;

            // Duplicated ("cloned") monitors share one source, and so one resolution and scale.
            if (displays.Any(d => d.DeviceName == source.viewGdiDeviceName))
                continue;

            var target = new DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                header = Header(DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                    Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(), path.targetInfo.adapterId, path.targetInfo.id),
            };
            string name = DisplayConfigGetDeviceInfo(ref target) == ERROR_SUCCESS ? target.monitorFriendlyDeviceName : "";
            if (string.IsNullOrWhiteSpace(name))
                name = "Display"; // built-in laptop panels often report no name

            displays.Add(new Display(source.viewGdiDeviceName, name, path.sourceInfo.adapterId, path.sourceInfo.id));
        }

        if (displays.Count == 0)
            throw new DisplayException("No active displays were found.");

        return displays.OrderBy(d => d.Number).ToList();
    }

    public DisplayMode GetCurrentMode()
    {
        DEVMODE mode = ReadCurrentMode();
        return new DisplayMode(mode.dmPelsWidth, mode.dmPelsHeight, mode.dmDisplayFrequency);
    }

    /// <summary>The modes the display reports for its current orientation, largest first.</summary>
    public IReadOnlyList<DisplayMode> GetModes()
    {
        var modes = new HashSet<DisplayMode>();
        DEVMODE mode = NewDevMode();
        for (int i = 0; EnumDisplaySettingsEx(DeviceName, i, ref mode, 0); i++)
            modes.Add(new DisplayMode(mode.dmPelsWidth, mode.dmPelsHeight, mode.dmDisplayFrequency));

        return modes
            .OrderByDescending(m => m.Width)
            .ThenByDescending(m => m.Height)
            .ThenByDescending(m => m.Frequency)
            .ToList();
    }

    /// <summary>
    /// Changes the resolution and, when <paramref name="frequency"/> is given, the refresh rate.
    /// Without a frequency Windows picks one. With <paramref name="persist"/> the mode is saved and
    /// survives a restart; without it the mode lasts until <see cref="RestoreSavedMode"/> or sign-out.
    /// </summary>
    public void SetMode(int width, int height, int? frequency, bool persist)
    {
        DEVMODE mode = NewDevMode();
        mode.dmPelsWidth = width;
        mode.dmPelsHeight = height;
        mode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
        if (frequency is int hz)
        {
            mode.dmDisplayFrequency = hz;
            mode.dmFields |= DM_DISPLAYFREQUENCY;
        }

        string description = frequency is null ? $"{width}x{height}" : $"{width}x{height} at {frequency} Hz";

        // Some drivers accept a refresh rate they never offered and then run at something else,
        // so a requested rate has to be one the display lists.
        if (frequency is int rate && !GetModes().Contains(new DisplayMode(width, height, rate)))
            throw new DisplayException($"Could not set {description}: the display does not list that mode.");

        // Ask the driver whether it can do this before touching the screen.
        int result = ChangeDisplaySettingsEx(DeviceName, ref mode, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
        if (result == DISP_CHANGE_SUCCESSFUL)
            result = ChangeDisplaySettingsEx(DeviceName, ref mode, IntPtr.Zero, persist ? CDS_UPDATEREGISTRY : 0, IntPtr.Zero);

        if (result != DISP_CHANGE_SUCCESSFUL)
            throw new DisplayException($"Could not set {description}: {DescribeChangeResult(result)}.");
    }

    /// <summary>Returns to the saved mode, undoing a <see cref="SetMode"/> made without persist.</summary>
    public void RestoreSavedMode()
    {
        int result = ChangeDisplaySettingsEx(DeviceName, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (result != DISP_CHANGE_SUCCESSFUL)
            throw new DisplayException($"Could not restore the previous display mode: {DescribeChangeResult(result)}.");
    }

    public ScaleInfo GetScale()
    {
        var request = new DISPLAYCONFIG_SOURCE_DPI_SCALE_GET
        {
            header = Header(DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE,
                Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DPI_SCALE_GET>(), adapterId, sourceId),
        };
        int error = DisplayConfigGetDeviceInfo(ref request);
        if (error != ERROR_SUCCESS)
            throw new DisplayException($"Could not read the scale of display {Number} (error {error}).");

        // Windows reports scale as steps relative to the recommended one. The lowest step is always
        // 100%, so minScaleRel says how many steps the recommended scale sits above 100%.
        int recommended = -request.minScaleRel;
        int current = recommended + Math.Clamp(request.curScaleRel, request.minScaleRel, request.maxScaleRel);
        int maximum = recommended + request.maxScaleRel;
        if (recommended < 0 || maximum < recommended || maximum >= ScaleSteps.Length)
            throw new DisplayException($"Windows reported a scale range for display {Number} that ScreenResy does not understand.");

        return new ScaleInfo(ScaleSteps[current], ScaleSteps[recommended], ScaleSteps[maximum]);
    }

    public void SetScale(int percent)
    {
        int step = Array.IndexOf(ScaleSteps, percent);
        if (step < 0)
            throw new DisplayException($"{percent}% is not a scale Windows offers. Use one of: {string.Join(", ", ScaleSteps)}.");

        ScaleInfo info = GetScale();
        if (percent > info.Maximum)
            throw new DisplayException($"{percent}% is above the maximum scale of display {Number} at its current resolution ({info.Maximum}%).");

        var request = new DISPLAYCONFIG_SOURCE_DPI_SCALE_SET
        {
            header = Header(DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE,
                Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DPI_SCALE_SET>(), adapterId, sourceId),
            scaleRel = step - Array.IndexOf(ScaleSteps, info.Recommended),
        };
        int error = DisplayConfigSetDeviceInfo(ref request);
        if (error != ERROR_SUCCESS)
            throw new DisplayException($"Could not set the scale of display {Number} to {percent}% (error {error}).");
    }

    private DEVMODE ReadCurrentMode()
    {
        DEVMODE mode = NewDevMode();
        if (!EnumDisplaySettingsEx(DeviceName, ENUM_CURRENT_SETTINGS, ref mode, 0))
            throw new DisplayException($"Could not read the current mode of {DeviceName}.");
        return mode;
    }

    private static string DescribeChangeResult(int result) => result switch
    {
        DISP_CHANGE_BADMODE => "the display does not support that mode",
        DISP_CHANGE_RESTART => "Windows needs a restart before it can use that mode",
        DISP_CHANGE_NOTUPDATED => "the setting could not be saved",
        DISP_CHANGE_BADDUALVIEW => "the display is in a dual-view configuration that does not allow it",
        _ => $"the display driver refused the change (code {result})",
    };

    private static DISPLAYCONFIG_PATH_INFO[] QueryActivePaths()
    {
        // The display configuration can change between asking for its size and reading it.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            int error = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount);
            if (error != ERROR_SUCCESS)
                break;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new byte[modeCount * DISPLAYCONFIG_MODE_INFO_SIZE];
            error = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (error == ERROR_SUCCESS)
                return paths[..(int)pathCount];
            if (error != ERROR_INSUFFICIENT_BUFFER)
                break;
        }

        throw new DisplayException("Could not read the display configuration from Windows.");
    }

    private static DEVMODE NewDevMode() => new()
    {
        dmDeviceName = "",
        dmFormName = "",
        dmSize = (short)Marshal.SizeOf<DEVMODE>(),
    };

    private static DISPLAYCONFIG_DEVICE_INFO_HEADER Header(int type, int size, LUID adapterId, uint id) => new()
    {
        type = type,
        size = (uint)size,
        adapterId = adapterId,
        id = id,
    };

    #region Win32

    private const int ERROR_SUCCESS = 0;
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    private const int ENUM_CURRENT_SETTINGS = -1;

    private const int DM_PELSWIDTH = 0x80000;
    private const int DM_PELSHEIGHT = 0x100000;
    private const int DM_DISPLAYFREQUENCY = 0x400000;

    private const uint CDS_UPDATEREGISTRY = 0x1;
    private const uint CDS_TEST = 0x2;

    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_RESTART = 1;
    private const int DISP_CHANGE_BADMODE = -2;
    private const int DISP_CHANGE_NOTUPDATED = -3;
    private const int DISP_CHANGE_BADDUALVIEW = -6;

    private const uint QDC_ONLY_ACTIVE_PATHS = 0x2;
    private const int DISPLAYCONFIG_MODE_INFO_SIZE = 64;

    private const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

    // Not in the Windows SDK. These are the requests the Settings app itself uses to read and set
    // per-display scale; there is no documented API for it. See
    // https://github.com/lihas/windows-DPI-scaling-sample
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE = -3;
    private const int DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE = -4;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public int outputTechnology;
        public int rotation;
        public int scaling;
        public uint refreshRateNumerator;
        public uint refreshRateDenominator;
        public int scanLineOrdering;
        public int targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public int type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public int outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SOURCE_DPI_SCALE_GET
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int minScaleRel;
        public int curScaleRel;
        public int maxScaleRel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SOURCE_DPI_SCALE_SET
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int scaleRel;
    }

    [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsExW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettingsEx(string deviceName, int modeNum, ref DEVMODE devMode, uint flags);

    [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string deviceName, ref DEVMODE devMode, IntPtr hwnd, uint flags, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string deviceName, IntPtr devMode, IntPtr hwnd, uint flags, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DISPLAYCONFIG_PATH_INFO[] paths,
        ref uint modeCount, [Out] byte[] modes, IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME request);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME request);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DPI_SCALE_GET request);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DPI_SCALE_SET request);

    #endregion
}
