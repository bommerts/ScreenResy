#include "display.h"

#include <stdarg.h>
#include <stdlib.h>
#include <strsafe.h>

const int ScaleSteps[] = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };
const int ScaleStepCount = ARRAYSIZE(ScaleSteps);

// Not in the Windows SDK. These are the requests the Settings app itself uses to read and set
// per-display scale; there is no documented API for it. See
// https://github.com/lihas/windows-DPI-scaling-sample
#define DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE ((DISPLAYCONFIG_DEVICE_INFO_TYPE)-3)
#define DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE ((DISPLAYCONFIG_DEVICE_INFO_TYPE)-4)

typedef struct DpiScaleGet {
    DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    INT32 minScaleRel;
    INT32 curScaleRel;
    INT32 maxScaleRel;
} DpiScaleGet;

typedef struct DpiScaleSet {
    DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    INT32 scaleRel;
} DpiScaleSet;

// Windows rejects these requests unless the sizes are exactly what it expects.
C_ASSERT(sizeof(DpiScaleGet) == 0x20);
C_ASSERT(sizeof(DpiScaleSet) == 0x18);

static BOOL Fail(WCHAR *error, const WCHAR *format, ...)
{
    va_list args;
    va_start(args, format);
    StringCchVPrintfW(error, DISPLAY_ERROR_LENGTH, format, args);
    va_end(args);
    return FALSE;
}

static int StepIndex(int percent)
{
    for (int i = 0; i < ScaleStepCount; i++) {
        if (ScaleSteps[i] == percent)
            return i;
    }
    return -1;
}

BOOL DisplayIsScaleStep(int percent)
{
    return StepIndex(percent) >= 0;
}

void DisplayFormatScaleSteps(WCHAR *text, size_t length)
{
    text[0] = L'\0';
    for (int i = 0; i < ScaleStepCount; i++) {
        WCHAR step[16];
        StringCchPrintfW(step, ARRAYSIZE(step), i == 0 ? L"%d" : L", %d", ScaleSteps[i]);
        StringCchCatW(text, length, step);
    }
}

static const WCHAR *DescribeChangeResult(LONG result, WCHAR *buffer, size_t length)
{
    switch (result) {
    case DISP_CHANGE_BADMODE:     return L"the display does not support that mode";
    case DISP_CHANGE_RESTART:     return L"Windows needs a restart before it can use that mode";
    case DISP_CHANGE_NOTUPDATED:  return L"the setting could not be saved";
    case DISP_CHANGE_BADDUALVIEW: return L"the display is in a dual-view configuration that does not allow it";
    default:
        StringCchPrintfW(buffer, length, L"the display driver refused the change (code %ld)", result);
        return buffer;
    }
}

// Reads the active display paths. The caller frees `*paths`.
static BOOL QueryActivePaths(DISPLAYCONFIG_PATH_INFO **paths, UINT32 *pathCount)
{
    // The display configuration can change between asking for its size and reading it.
    for (int attempt = 0; attempt < 3; attempt++) {
        UINT32 modeCount = 0;
        if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, pathCount, &modeCount) != ERROR_SUCCESS)
            break;

        DISPLAYCONFIG_PATH_INFO *pathBuffer = calloc(*pathCount + 1, sizeof *pathBuffer);
        DISPLAYCONFIG_MODE_INFO *modeBuffer = calloc(modeCount + 1, sizeof *modeBuffer);
        LONG result = ERROR_OUTOFMEMORY;
        if (pathBuffer != NULL && modeBuffer != NULL)
            result = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, pathCount, pathBuffer, &modeCount, modeBuffer, NULL);
        free(modeBuffer);

        if (result == ERROR_SUCCESS) {
            *paths = pathBuffer;
            return TRUE;
        }
        free(pathBuffer);
        if (result != ERROR_INSUFFICIENT_BUFFER)
            break;
    }
    return FALSE;
}

static int CompareDisplays(const void *left, const void *right)
{
    return ((const Display *)left)->number - ((const Display *)right)->number;
}

BOOL DisplayGetAll(Display *displays, int *count, WCHAR *error)
{
    DISPLAYCONFIG_PATH_INFO *paths = NULL;
    UINT32 pathCount = 0;

    *count = 0;
    if (!QueryActivePaths(&paths, &pathCount))
        return Fail(error, L"Could not read the display configuration from Windows.");

    for (UINT32 i = 0; i < pathCount && *count < DISPLAY_MAX; i++) {
        DISPLAYCONFIG_SOURCE_DEVICE_NAME source = { 0 };
        source.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
        source.header.size = sizeof source;
        source.header.adapterId = paths[i].sourceInfo.adapterId;
        source.header.id = paths[i].sourceInfo.id;
        if (DisplayConfigGetDeviceInfo(&source.header) != ERROR_SUCCESS)
            continue;

        // Duplicated ("cloned") monitors share one source, and so one resolution and scale.
        BOOL known = FALSE;
        for (int j = 0; j < *count; j++) {
            if (wcscmp(displays[j].deviceName, source.viewGdiDeviceName) == 0)
                known = TRUE;
        }
        if (known)
            continue;

        DISPLAYCONFIG_TARGET_DEVICE_NAME target = { 0 };
        target.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        target.header.size = sizeof target;
        target.header.adapterId = paths[i].targetInfo.adapterId;
        target.header.id = paths[i].targetInfo.id;
        const WCHAR *name = L"";
        if (DisplayConfigGetDeviceInfo(&target.header) == ERROR_SUCCESS)
            name = target.monitorFriendlyDeviceName;
        if (name[0] == L'\0')
            name = L"Display"; // built-in laptop panels often report no name

        Display *display = &displays[(*count)++];
        ZeroMemory(display, sizeof *display);
        StringCchCopyW(display->deviceName, ARRAYSIZE(display->deviceName), source.viewGdiDeviceName);
        StringCchCopyW(display->friendlyName, ARRAYSIZE(display->friendlyName), name);
        display->adapterId = paths[i].sourceInfo.adapterId;
        display->sourceId = paths[i].sourceInfo.id;

        // "\\.\DISPLAY2" -> 2
        for (const WCHAR *p = display->deviceName; *p != L'\0'; p++) {
            if (*p >= L'0' && *p <= L'9')
                display->number = display->number * 10 + (*p - L'0');
        }

        // Windows always places the primary display at the origin of the desktop.
        DEVMODEW current = { .dmSize = sizeof current };
        if (EnumDisplaySettingsExW(display->deviceName, ENUM_CURRENT_SETTINGS, &current, 0))
            display->isPrimary = current.dmPosition.x == 0 && current.dmPosition.y == 0;
    }
    free(paths);

    if (*count == 0)
        return Fail(error, L"No active displays were found.");

    qsort(displays, (size_t)*count, sizeof *displays, CompareDisplays);
    return TRUE;
}

BOOL DisplayGetCurrentMode(const Display *display, DisplayMode *mode, WCHAR *error)
{
    DEVMODEW current = { .dmSize = sizeof current };
    if (!EnumDisplaySettingsExW(display->deviceName, ENUM_CURRENT_SETTINGS, &current, 0))
        return Fail(error, L"Could not read the current mode of %ls.", display->deviceName);

    mode->width = (int)current.dmPelsWidth;
    mode->height = (int)current.dmPelsHeight;
    mode->frequency = (int)current.dmDisplayFrequency;
    return TRUE;
}

// Largest first.
static int CompareModes(const void *left, const void *right)
{
    const DisplayMode *a = left;
    const DisplayMode *b = right;
    if (a->width != b->width)
        return b->width - a->width;
    if (a->height != b->height)
        return b->height - a->height;
    return b->frequency - a->frequency;
}

BOOL DisplayGetModes(const Display *display, DisplayMode **modes, int *count, WCHAR *error)
{
    DisplayMode *list = NULL;
    int used = 0;
    int capacity = 0;
    DEVMODEW mode = { .dmSize = sizeof mode };

    *modes = NULL;
    *count = 0;
    for (DWORD i = 0; EnumDisplaySettingsExW(display->deviceName, i, &mode, 0); i++) {
        if (used == capacity) {
            capacity = capacity == 0 ? 256 : capacity * 2;
            DisplayMode *grown = realloc(list, (size_t)capacity * sizeof *list);
            if (grown == NULL) {
                free(list);
                return Fail(error, L"Not enough memory to list the display modes.");
            }
            list = grown;
        }
        list[used].width = (int)mode.dmPelsWidth;
        list[used].height = (int)mode.dmPelsHeight;
        list[used].frequency = (int)mode.dmDisplayFrequency;
        used++;
    }
    if (used == 0)
        return TRUE;

    // The driver lists each mode several times (once per colour depth and scaling option).
    qsort(list, (size_t)used, sizeof *list, CompareModes);
    int unique = 1;
    for (int i = 1; i < used; i++) {
        if (CompareModes(&list[i], &list[unique - 1]) != 0)
            list[unique++] = list[i];
    }

    *modes = list;
    *count = unique;
    return TRUE;
}

BOOL DisplaySetMode(const Display *display, int width, int height, int frequency, BOOL persist, WCHAR *error)
{
    WCHAR description[64];
    WCHAR reason[96];

    if (frequency == 0)
        StringCchPrintfW(description, ARRAYSIZE(description), L"%dx%d", width, height);
    else
        StringCchPrintfW(description, ARRAYSIZE(description), L"%dx%d at %d Hz", width, height, frequency);

    // Some drivers accept a refresh rate they never offered and then run at something else,
    // so a requested rate has to be one the display lists.
    if (frequency != 0) {
        DisplayMode *modes;
        int count;
        BOOL listed = FALSE;
        if (!DisplayGetModes(display, &modes, &count, error))
            return FALSE;
        for (int i = 0; i < count; i++) {
            if (modes[i].width == width && modes[i].height == height && modes[i].frequency == frequency)
                listed = TRUE;
        }
        free(modes);
        if (!listed)
            return Fail(error, L"Could not set %ls: the display does not list that mode.", description);
    }

    DEVMODEW mode = { .dmSize = sizeof mode };
    mode.dmPelsWidth = (DWORD)width;
    mode.dmPelsHeight = (DWORD)height;
    mode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
    if (frequency != 0) {
        mode.dmDisplayFrequency = (DWORD)frequency;
        mode.dmFields |= DM_DISPLAYFREQUENCY;
    }

    // Ask the driver whether it can do this before touching the screen.
    LONG result = ChangeDisplaySettingsExW(display->deviceName, &mode, NULL, CDS_TEST, NULL);
    if (result == DISP_CHANGE_SUCCESSFUL)
        result = ChangeDisplaySettingsExW(display->deviceName, &mode, NULL, persist ? CDS_UPDATEREGISTRY : 0, NULL);

    if (result != DISP_CHANGE_SUCCESSFUL) {
        return Fail(error, L"Could not set %ls: %ls.", description,
            DescribeChangeResult(result, reason, ARRAYSIZE(reason)));
    }
    return TRUE;
}

BOOL DisplayRestoreSavedMode(const Display *display, WCHAR *error)
{
    WCHAR reason[96];
    LONG result = ChangeDisplaySettingsExW(display->deviceName, NULL, NULL, 0, NULL);
    if (result != DISP_CHANGE_SUCCESSFUL) {
        return Fail(error, L"Could not restore the previous display mode: %ls.",
            DescribeChangeResult(result, reason, ARRAYSIZE(reason)));
    }
    return TRUE;
}

BOOL DisplayGetScale(const Display *display, ScaleInfo *scale, WCHAR *error)
{
    DpiScaleGet request = { 0 };
    request.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE;
    request.header.size = sizeof request;
    request.header.adapterId = display->adapterId;
    request.header.id = display->sourceId;

    LONG result = DisplayConfigGetDeviceInfo(&request.header);
    if (result != ERROR_SUCCESS)
        return Fail(error, L"Could not read the scale of display %d (error %ld).", display->number, result);

    // Windows reports scale as steps relative to the recommended one. The lowest step is always
    // 100%, so minScaleRel says how many steps the recommended scale sits above 100%.
    int recommended = -request.minScaleRel;
    int maximum = recommended + request.maxScaleRel;
    if (recommended < 0 || maximum < recommended || maximum >= ScaleStepCount) {
        return Fail(error, L"Windows reported a scale range for display %d that ScreenResy does not understand.",
            display->number);
    }

    int relative = request.curScaleRel;
    if (relative < request.minScaleRel)
        relative = request.minScaleRel;
    if (relative > request.maxScaleRel)
        relative = request.maxScaleRel;

    scale->current = ScaleSteps[recommended + relative];
    scale->recommended = ScaleSteps[recommended];
    scale->maximum = ScaleSteps[maximum];
    return TRUE;
}

BOOL DisplaySetScale(const Display *display, int percent, WCHAR *error)
{
    int step = StepIndex(percent);
    if (step < 0) {
        WCHAR steps[128];
        DisplayFormatScaleSteps(steps, ARRAYSIZE(steps));
        return Fail(error, L"%d%% is not a scale Windows offers. Use one of: %ls.", percent, steps);
    }

    ScaleInfo info;
    if (!DisplayGetScale(display, &info, error))
        return FALSE;
    if (percent > info.maximum) {
        return Fail(error, L"%d%% is above the maximum scale of display %d at its current resolution (%d%%).",
            percent, display->number, info.maximum);
    }

    DpiScaleSet request = { 0 };
    request.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE;
    request.header.size = sizeof request;
    request.header.adapterId = display->adapterId;
    request.header.id = display->sourceId;
    request.scaleRel = step - StepIndex(info.recommended);

    LONG result = DisplayConfigSetDeviceInfo(&request.header);
    if (result != ERROR_SUCCESS) {
        return Fail(error, L"Could not set the scale of display %d to %d%% (error %ld).",
            display->number, percent, result);
    }
    return TRUE;
}
