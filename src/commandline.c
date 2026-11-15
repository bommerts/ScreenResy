#include "app.h"
#include "display.h"

#include <stdarg.h>
#include <stdlib.h>
#include <strsafe.h>

enum {
    EXIT_OK = 0,
    EXIT_INVALID_ARGUMENTS = 1,
    EXIT_CHANGE_FAILED = 2,
};

static const WCHAR Usage[] =
    L"Usage:\n"
    L"  ScreenResy                                  Open the ScreenResy window\n"
    L"  ScreenResy [-x <width> -y <height>] [-r <hz>] [-d <percent>] [-m <display>]\n"
    L"  ScreenResy --list\n"
    L"\n"
    L"Options:\n"
    L"  -x, --width <pixels>     Horizontal resolution. Use together with -y.\n"
    L"  -y, --height <pixels>    Vertical resolution. Use together with -x.\n"
    L"  -r, --refresh <hz>       Refresh rate. Without it, Windows picks one.\n"
    L"  -d, --scale <percent>    Display scale: 100, 125, 150, 175, 200, ...\n"
    L"  -m, --monitor <number>   Display to change (see --list). Default: the primary display.\n"
    L"  -l, --list               Show each display, its current settings and supported modes.\n"
    L"  -h, --help               Show this help.\n"
    L"  -v, --version            Show the version.\n"
    L"\n"
    L"Examples:\n"
    L"  ScreenResy -x 1920 -y 1080 -d 125\n"
    L"  ScreenResy -x 2560 -y 1440 -r 60 -m 2\n"
    L"  ScreenResy -d 150\n"
    L"\n"
    L"Exit codes:\n"
    L"  0  success    1  invalid arguments    2  Windows refused the change\n";

// What the arguments asked for. A number of 0 means the option was not given.
typedef struct Options {
    int width;
    int height;
    int refresh;
    int scale;
    int monitor;
    BOOL list;
    BOOL help;
    BOOL version;
} Options;

// Writes text to stdout or stderr. A console gets it as Unicode; a file or pipe gets UTF-8 with
// Windows line endings. With no console attached (started from a shortcut) it goes nowhere.
static void Write(DWORD stream, const WCHAR *text)
{
    HANDLE handle = GetStdHandle(stream);
    DWORD mode;
    DWORD written;

    if (handle == NULL || handle == INVALID_HANDLE_VALUE)
        return;

    if (GetConsoleMode(handle, &mode)) {
        WriteConsoleW(handle, text, (DWORD)wcslen(text), &written, NULL);
        return;
    }

    // Worst case every character is a line feed needing a carriage return in front of it.
    size_t length = wcslen(text);
    WCHAR *lines = malloc((length * 2 + 1) * sizeof *lines);
    if (lines == NULL)
        return;
    size_t used = 0;
    for (size_t i = 0; i < length; i++) {
        if (text[i] == L'\n')
            lines[used++] = L'\r';
        lines[used++] = text[i];
    }

    int bytes = WideCharToMultiByte(CP_UTF8, 0, lines, (int)used, NULL, 0, NULL, NULL);
    char *utf8 = bytes > 0 ? malloc((size_t)bytes) : NULL;
    if (utf8 != NULL) {
        WideCharToMultiByte(CP_UTF8, 0, lines, (int)used, utf8, bytes, NULL, NULL);
        WriteFile(handle, utf8, (DWORD)bytes, &written, NULL);
    }
    free(utf8);
    free(lines);
}

static void Print(const WCHAR *format, ...)
{
    WCHAR text[1024];
    va_list args;
    va_start(args, format);
    StringCchVPrintfW(text, ARRAYSIZE(text), format, args);
    va_end(args);
    Write(STD_OUTPUT_HANDLE, text);
}

// Writes "ScreenResy: <message>" and a line break to stderr.
static void PrintError(const WCHAR *format, ...)
{
    WCHAR text[1024];
    va_list args;
    va_start(args, format);
    StringCchVPrintfW(text, ARRAYSIZE(text), format, args);
    va_end(args);
    Write(STD_ERROR_HANDLE, APP_NAME L": ");
    Write(STD_ERROR_HANDLE, text);
    Write(STD_ERROR_HANDLE, L"\n");
}

static BOOL Is(const WCHAR *option, const WCHAR *first, const WCHAR *second, const WCHAR *third)
{
    return _wcsicmp(option, first) == 0
        || (second != NULL && _wcsicmp(option, second) == 0)
        || (third != NULL && _wcsicmp(option, third) == 0);
}

// Reads the whole-number value that follows option `*index`, and moves `*index` onto it.
static BOOL ReadValue(int argc, wchar_t **argv, int *index, int *value)
{
    const WCHAR *option = argv[*index];
    if (*index + 1 >= argc) {
        PrintError(L"%ls needs a value.", option);
        return FALSE;
    }

    const WCHAR *text = argv[++*index];
    WCHAR *end;
    long number = wcstol(text, &end, 10);
    if (text[0] == L'\0' || *end != L'\0' || number <= 0 || number > 1000000) {
        PrintError(L"'%ls' is not a valid value for %ls; it must be a whole number above zero.", text, option);
        return FALSE;
    }
    *value = (int)number;
    return TRUE;
}

// On failure the reason has already been written to stderr.
static BOOL Parse(int argc, wchar_t **argv, Options *options)
{
    ZeroMemory(options, sizeof *options);

    for (int i = 0; i < argc; i++) {
        const WCHAR *option = argv[i];
        BOOL ok = TRUE;

        if (Is(option, L"-x", L"--width", NULL))
            ok = ReadValue(argc, argv, &i, &options->width);
        else if (Is(option, L"-y", L"--height", NULL))
            ok = ReadValue(argc, argv, &i, &options->height);
        else if (Is(option, L"-r", L"--refresh", NULL))
            ok = ReadValue(argc, argv, &i, &options->refresh);
        else if (Is(option, L"-d", L"--scale", L"--dpi"))
            ok = ReadValue(argc, argv, &i, &options->scale);
        else if (Is(option, L"-m", L"--monitor", L"--display"))
            ok = ReadValue(argc, argv, &i, &options->monitor);
        else if (Is(option, L"-l", L"--list", NULL))
            options->list = TRUE;
        else if (Is(option, L"-h", L"--help", L"-?") || Is(option, L"/?", NULL, NULL))
            options->help = TRUE;
        else if (Is(option, L"-v", L"--version", NULL))
            options->version = TRUE;
        else {
            PrintError(L"unknown option '%ls'.", option);
            ok = FALSE;
        }

        if (!ok)
            return FALSE;
    }

    if (options->help || options->version || options->list)
        return TRUE;

    if ((options->width == 0) != (options->height == 0)) {
        PrintError(L"-x and -y must be used together.");
        return FALSE;
    }
    if (options->width == 0 && options->refresh == 0 && options->scale == 0) {
        PrintError(L"nothing to change. Give a resolution (-x and -y), a refresh rate (-r) or a scale (-d).");
        return FALSE;
    }
    return TRUE;
}

static BOOL Apply(const Display *display, const Options *options, WCHAR *error)
{
    // Catch a mistyped scale before the resolution is changed, not after.
    if (options->scale != 0 && !DisplayIsScaleStep(options->scale)) {
        WCHAR steps[128];
        DisplayFormatScaleSteps(steps, ARRAYSIZE(steps));
        StringCchPrintfW(error, DISPLAY_ERROR_LENGTH, L"%d%% is not a scale Windows offers. Use one of: %ls.",
            options->scale, steps);
        return FALSE;
    }

    if (options->width != 0 || options->refresh != 0) {
        DisplayMode mode;
        if (!DisplayGetCurrentMode(display, &mode, error))
            return FALSE;
        if (options->width != 0) {
            mode.width = options->width;
            mode.height = options->height;
        }
        if (!DisplaySetMode(display, mode.width, mode.height, options->refresh, TRUE, error))
            return FALSE;
        if (!DisplayGetCurrentMode(display, &mode, error))
            return FALSE;
        Print(L"Display %d: resolution set to %dx%d at %d Hz\n", display->number, mode.width, mode.height, mode.frequency);
    }

    // After the resolution, because the highest scale a display allows depends on it.
    if (options->scale != 0) {
        if (!DisplaySetScale(display, options->scale, error))
            return FALSE;
        Print(L"Display %d: scale set to %d%%\n", display->number, options->scale);
    }
    return TRUE;
}

static BOOL List(const Display *displays, int count, WCHAR *error)
{
    for (int i = 0; i < count; i++) {
        const Display *display = &displays[i];
        DisplayMode current;
        ScaleInfo scale;
        DisplayMode *modes;
        int modeCount;
        WCHAR ignored[DISPLAY_ERROR_LENGTH];

        if (!DisplayGetCurrentMode(display, &current, error) || !DisplayGetModes(display, &modes, &modeCount, error))
            return FALSE;

        Print(L"Display %d: %ls%ls\n", display->number, display->friendlyName, display->isPrimary ? L" (primary)" : L"");
        Print(L"  Current:      %dx%d at %d Hz\n", current.width, current.height, current.frequency);
        if (DisplayGetScale(display, &scale, ignored))
            Print(L"  Scale:        %d%% (recommended %d%%, maximum %d%%)\n", scale.current, scale.recommended, scale.maximum);
        else
            Print(L"  Scale:        not available\n");

        // Modes are sorted, so the refresh rates of one resolution sit next to each other.
        Print(L"  Resolutions:\n");
        for (int m = 0; m < modeCount; m++) {
            BOOL first = m == 0 || modes[m].width != modes[m - 1].width || modes[m].height != modes[m - 1].height;
            BOOL last = m == modeCount - 1 || modes[m].width != modes[m + 1].width || modes[m].height != modes[m + 1].height;
            if (first) {
                WCHAR size[32];
                StringCchPrintfW(size, ARRAYSIZE(size), L"%dx%d", modes[m].width, modes[m].height);
                Print(L"    %-12ls", size);
            }
            Print(first ? L" %d" : L", %d", modes[m].frequency);
            if (last)
                Print(L" Hz\n");
        }
        Print(L"\n");
        free(modes);
    }
    return TRUE;
}

int CommandLineRun(int argc, wchar_t **argv)
{
    Options options;
    Display displays[DISPLAY_MAX];
    int count;
    WCHAR error[DISPLAY_ERROR_LENGTH] = L"";

    if (!Parse(argc, argv, &options)) {
        Write(STD_ERROR_HANDLE, L"\n");
        Write(STD_ERROR_HANDLE, Usage);
        return EXIT_INVALID_ARGUMENTS;
    }

    if (options.help) {
        Print(L"%ls %ls - change screen resolution, refresh rate and scale.\n", APP_NAME, APP_VERSION_W);
        Print(L"%ls - %ls\n", APP_COPYRIGHT_W, APP_AUTHOR_URL);
        Print(L"%ls\n\n", APP_PROJECT_URL);
        Write(STD_OUTPUT_HANDLE, Usage);
        return EXIT_OK;
    }
    if (options.version) {
        Print(L"%ls %ls\n", APP_NAME, APP_VERSION_W);
        return EXIT_OK;
    }

    if (!DisplayGetAll(displays, &count, error)) {
        PrintError(L"%ls", error);
        return EXIT_CHANGE_FAILED;
    }

    if (options.list) {
        if (!List(displays, count, error)) {
            PrintError(L"%ls", error);
            return EXIT_CHANGE_FAILED;
        }
        return EXIT_OK;
    }

    // The display asked for, or else the primary one.
    const Display *display = NULL;
    for (int i = 0; i < count && display == NULL; i++) {
        if (options.monitor != 0 ? displays[i].number == options.monitor : displays[i].isPrimary)
            display = &displays[i];
    }
    if (display == NULL && options.monitor == 0)
        display = &displays[0];
    if (display == NULL) {
        WCHAR available[128] = L"";
        for (int i = 0; i < count; i++) {
            WCHAR number[16];
            StringCchPrintfW(number, ARRAYSIZE(number), i == 0 ? L"%d" : L", %d", displays[i].number);
            StringCchCatW(available, ARRAYSIZE(available), number);
        }
        PrintError(L"there is no display %d. Available: %ls.", options.monitor, available);
        return EXIT_INVALID_ARGUMENTS;
    }

    if (!Apply(display, &options, error)) {
        PrintError(L"%ls", error);
        return EXIT_CHANGE_FAILED;
    }
    return EXIT_OK;
}
