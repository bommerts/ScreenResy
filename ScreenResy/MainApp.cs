using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenResy
{
    public partial class MainApp : Form
    {
        // app info
        const string AppName = "ScreenResy";
        const string AppVer = "1.01";
        const string AppYear = "2023";
        const string AppAuthor = "Joe Farrell";
        const string AppAuthorURL = "https://www.linkedin.com/in/joefarrell/";

        // console display
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        // COMMANDLINE Arguments
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                Console.Write($"");
                Console.WriteLine($"\n{AppName} version {AppVer} Copyright (C) {AppYear} by {AppAuthor} : {AppAuthorURL}");

                if (!HandleCommandLineArguments(args))
                {
                    ShowUsageInstructions();
                    SendKeys.SendWait("{ENTER}");
                }
            }
            else
            {
                Application.Run(new MainApp());
            }
        }

        static bool HandleCommandLineArguments(string[] args)
        {
            int width = 0, height = 0, dpi = 0;
            bool widthSet = false, heightSet = false, dpiSet = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-x":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out width)) widthSet = true;
                        break;
                    case "-y":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out height)) heightSet = true;
                        break;
                    case "-d":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out dpi)) dpiSet = true;
                        break;
                }
            }

            if (widthSet && heightSet)
            {
                MainApp.SetResolution(width, height);
                Console.WriteLine($"Resolution set to {width}x{height}");
            }

            if (dpiSet)
            {
                MainApp.SetDpiScaling(dpi);
                Console.WriteLine($"DPI Scaling set to {dpi}%");
            }

            SendKeys.SendWait("{ENTER}"); // exit app

            return widthSet && heightSet && dpiSet;
        }

        static void ShowUsageInstructions()
        {
            Console.WriteLine("Invalid arguments. Usage:");
            Console.WriteLine("ScreenResy.exe -x [width] -y [height] -d [dpi]");
            Console.WriteLine("Example: ScreenResy.exe -x 1920 -y 1080 -d 125");
        }

        // DPI Scaling
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

        const uint SPI_GETLOGICALDPIOVERRIDE = 0x009E;
        const uint SPI_SETLOGICALDPIOVERRIDE = 0x009F;
        const uint SPIF_UPDATEINIFILE = 0x01;
        const uint SPIF_SENDCHANGE = 0x02;

        static readonly uint[] DpiVals = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };

        // Screen Resolution
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
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

            public const int DM_PELSWIDTH = 0x80000;
            public const int DM_PELSHEIGHT = 0x100000;
        }

        [DllImport("user32.dll")]
        public static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

        public MainApp()
        {
            InitializeComponent();
        }

        public static void SetResolution(int width, int height)
        {
            DEVMODE vDevMode = new DEVMODE
            {
                dmDeviceName = new string(new char[32]),
                dmFormName = new string(new char[32]),
                dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)),
                dmPelsWidth = width,
                dmPelsHeight = height,
                dmFields = DEVMODE.DM_PELSWIDTH | DEVMODE.DM_PELSHEIGHT
            };

            ChangeDisplaySettings(ref vDevMode, 0);
        }

        private void buttonChangeResolution_Click(object sender, EventArgs e)
        {
            SetResolution(int.Parse(textBoxXRes.Text), int.Parse(textBoxYRes.Text)); // Set to desired resolution
        }

        private void buttonChangeScale_Click(object sender, EventArgs e)
        {
            int dpiPercentage = int.Parse(textBoxScale.Text); // % scaling
            SetDpiScaling(dpiPercentage);
        }

        private static void SetDpiScaling(int percentScaleToSet)
        {
            int recommendedDpiScale = GetRecommendedDPIScaling();
            if (recommendedDpiScale > 0)
            {
                int index = 0, recIndex = 0, setIndex = 0;
                foreach (var scale in DpiVals)
                {
                    if (recommendedDpiScale == scale)
                    {
                        recIndex = index;
                    }
                    if (percentScaleToSet == scale)
                    {
                        setIndex = index;
                    }
                    index++;
                }

                int relativeIndex = setIndex - recIndex;
                SystemParametersInfo(SPI_SETLOGICALDPIOVERRIDE, (uint)relativeIndex, ref relativeIndex, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
        }

        private static int GetRecommendedDPIScaling()
        {
            int dpi = 0;
            bool retval = SystemParametersInfo(SPI_GETLOGICALDPIOVERRIDE, 0, ref dpi, 0);

            if (retval)
            {
                int currDPI = (int)DpiVals[dpi * -1];
                return currDPI;
            }

            return -1;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
