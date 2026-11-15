#include "app.h"
#include "display.h"
#include "resource.h"

#include <commctrl.h>
#include <shellapi.h>
#include <stdlib.h>
#include <strsafe.h>

#define KEEP_TIMEOUT_SECONDS 15
#define KEEP_TIMER           1

// The displays in the Display list; each list item carries its index here.
static Display g_displays[DISPLAY_MAX];
static int g_displayCount;

// The modes of the selected display.
static DisplayMode *g_modes;
static int g_modeCount;

static void ShowError(HWND dialog, const WCHAR *message)
{
    MessageBoxW(dialog, message, APP_NAME, MB_OK | MB_ICONWARNING);
}

// Each list item carries a number (its "data") saying what it stands for.

static void ComboAdd(HWND combo, const WCHAR *text, LPARAM data)
{
    LRESULT index = SendMessageW(combo, CB_ADDSTRING, 0, (LPARAM)text);
    if (index >= 0)
        SendMessageW(combo, CB_SETITEMDATA, (WPARAM)index, data);
}

// Selects the item carrying `data`. Returns FALSE, with nothing selected, if there is none.
static BOOL ComboSelect(HWND combo, LPARAM data)
{
    LRESULT count = SendMessageW(combo, CB_GETCOUNT, 0, 0);
    for (LRESULT i = 0; i < count; i++) {
        if (SendMessageW(combo, CB_GETITEMDATA, (WPARAM)i, 0) == data) {
            SendMessageW(combo, CB_SETCURSEL, (WPARAM)i, 0);
            return TRUE;
        }
    }
    SendMessageW(combo, CB_SETCURSEL, (WPARAM)-1, 0);
    return FALSE;
}

static BOOL ComboSelected(HWND combo, LPARAM *data)
{
    LRESULT index = SendMessageW(combo, CB_GETCURSEL, 0, 0);
    if (index == CB_ERR)
        return FALSE;
    *data = SendMessageW(combo, CB_GETITEMDATA, (WPARAM)index, 0);
    return TRUE;
}

// Lists the refresh rates of the selected resolution, keeping `preferred` if it has it.
static void LoadRefreshRates(HWND dialog, int preferred)
{
    HWND refresh = GetDlgItem(dialog, IDC_REFRESH);
    LPARAM resolution;

    SendMessageW(refresh, CB_RESETCONTENT, 0, 0);
    if (!ComboSelected(GetDlgItem(dialog, IDC_RESOLUTION), &resolution))
        return;

    for (int i = 0; i < g_modeCount; i++) {
        if (MAKELPARAM(g_modes[i].width, g_modes[i].height) == resolution) {
            WCHAR text[32];
            StringCchPrintfW(text, ARRAYSIZE(text), L"%d Hz", g_modes[i].frequency);
            ComboAdd(refresh, text, g_modes[i].frequency);
        }
    }

    if (!ComboSelect(refresh, preferred))
        SendMessageW(refresh, CB_SETCURSEL, 0, 0); // the highest
}

// Shows what the selected display supports, with its current settings selected.
static void LoadSettings(HWND dialog)
{
    HWND resolution = GetDlgItem(dialog, IDC_RESOLUTION);
    HWND scale = GetDlgItem(dialog, IDC_SCALE);
    LPARAM index;
    DisplayMode current;
    ScaleInfo scaleInfo;
    WCHAR error[DISPLAY_ERROR_LENGTH];
    WCHAR summary[128];
    WCHAR text[64];

    SendMessageW(resolution, CB_RESETCONTENT, 0, 0);
    SendMessageW(GetDlgItem(dialog, IDC_REFRESH), CB_RESETCONTENT, 0, 0);
    SendMessageW(scale, CB_RESETCONTENT, 0, 0);
    SetDlgItemTextW(dialog, IDC_CURRENT, L"");
    EnableWindow(GetDlgItem(dialog, IDOK), FALSE);
    free(g_modes);
    g_modes = NULL;
    g_modeCount = 0;

    if (!ComboSelected(GetDlgItem(dialog, IDC_DISPLAY), &index))
        return;
    const Display *display = &g_displays[index];

    if (!DisplayGetCurrentMode(display, &current, error) || !DisplayGetModes(display, &g_modes, &g_modeCount, error)) {
        ShowError(dialog, error);
        return;
    }

    // Modes are sorted, so each resolution's refresh rates sit together: list its first one.
    for (int i = 0; i < g_modeCount; i++) {
        if (i == 0 || g_modes[i].width != g_modes[i - 1].width || g_modes[i].height != g_modes[i - 1].height) {
            StringCchPrintfW(text, ARRAYSIZE(text), L"%d \x00D7 %d", g_modes[i].width, g_modes[i].height);
            ComboAdd(resolution, text, MAKELPARAM(g_modes[i].width, g_modes[i].height));
        }
    }
    BOOL listed = ComboSelect(resolution, MAKELPARAM(current.width, current.height));
    LoadRefreshRates(dialog, current.frequency);

    StringCchPrintfW(summary, ARRAYSIZE(summary), L"Current:  %d \x00D7 %d,  %d Hz",
        current.width, current.height, current.frequency);

    // Scale comes from an undocumented part of Windows, so the rest must work without it.
    if (DisplayGetScale(display, &scaleInfo, error)) {
        for (int i = 0; i < ScaleStepCount && ScaleSteps[i] <= scaleInfo.maximum; i++) {
            StringCchPrintfW(text, ARRAYSIZE(text),
                ScaleSteps[i] == scaleInfo.recommended ? L"%d%% (recommended)" : L"%d%%", ScaleSteps[i]);
            ComboAdd(scale, text, ScaleSteps[i]);
        }
        ComboSelect(scale, scaleInfo.current);
        StringCchPrintfW(text, ARRAYSIZE(text), L",  %d%%", scaleInfo.current);
        StringCchCatW(summary, ARRAYSIZE(summary), text);
    }
    EnableWindow(scale, SendMessageW(scale, CB_GETCOUNT, 0, 0) > 0);

    SetDlgItemTextW(dialog, IDC_CURRENT, summary);
    EnableWindow(GetDlgItem(dialog, IDOK), listed);
}

// Fills the display list, then the settings of the selected display. Selects the display named
// `deviceName`, or the primary one if that is NULL or gone.
static void LoadDisplays(HWND dialog, const WCHAR *deviceName)
{
    HWND combo = GetDlgItem(dialog, IDC_DISPLAY);
    WCHAR error[DISPLAY_ERROR_LENGTH];
    int selected = 0;
    BOOL found = FALSE;

    SendMessageW(combo, CB_RESETCONTENT, 0, 0);
    if (!DisplayGetAll(g_displays, &g_displayCount, error)) {
        g_displayCount = 0;
        ShowError(dialog, error);
    }

    for (int i = 0; i < g_displayCount; i++) {
        WCHAR text[128];
        StringCchPrintfW(text, ARRAYSIZE(text), L"%d: %ls%ls", g_displays[i].number, g_displays[i].friendlyName,
            g_displays[i].isPrimary ? L" (primary)" : L"");
        ComboAdd(combo, text, i);

        if (deviceName != NULL && wcscmp(g_displays[i].deviceName, deviceName) == 0) {
            selected = i;
            found = TRUE;
        } else if (!found && g_displays[i].isPrimary) {
            selected = i;
        }
    }
    if (g_displayCount > 0)
        ComboSelect(combo, selected);

    LoadSettings(dialog);
}

static void UpdateCountdown(HWND dialog, int secondsLeft)
{
    WCHAR text[96];
    StringCchPrintfW(text, ARRAYSIZE(text), L"Reverting to the previous display settings in %d second%ls.",
        secondsLeft, secondsLeft == 1 ? L"" : L"s");
    SetDlgItemTextW(dialog, IDC_COUNTDOWN, text);
}

// Asks whether to keep a new display mode and answers "no" by itself after a few seconds, so a
// mode the monitor cannot show never leaves the user stuck with a blank screen.
// Ends with IDOK to keep and IDCANCEL to revert.
static INT_PTR CALLBACK KeepProc(HWND dialog, UINT message, WPARAM wParam, LPARAM lParam)
{
    static int secondsLeft;
    UNREFERENCED_PARAMETER(lParam);

    switch (message) {
    case WM_INITDIALOG:
        secondsLeft = KEEP_TIMEOUT_SECONDS;
        UpdateCountdown(dialog, secondsLeft);
        SetTimer(dialog, KEEP_TIMER, 1000, NULL);
        return TRUE;

    case WM_TIMER:
        if (--secondsLeft <= 0) {
            KillTimer(dialog, KEEP_TIMER);
            EndDialog(dialog, IDCANCEL);
        } else {
            UpdateCountdown(dialog, secondsLeft);
        }
        return TRUE;

    case WM_COMMAND:
        if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL) {
            KillTimer(dialog, KEEP_TIMER);
            EndDialog(dialog, LOWORD(wParam));
            return TRUE;
        }
        break;
    }
    return FALSE;
}

// Applies `mode` and, unless it is 0, `scale` to the display. Returns TRUE when everything asked
// for was done, and also when the user chose to revert.
static BOOL ApplySettings(HWND dialog, const Display *display, DisplayMode mode, int scale, WCHAR *error)
{
    DisplayMode previousMode;
    ScaleInfo previousScale = { 0 };

    if (!DisplayGetCurrentMode(display, &previousMode, error))
        return FALSE;
    if (scale != 0 && !DisplayGetScale(display, &previousScale, error))
        return FALSE;

    BOOL changeMode = mode.width != previousMode.width || mode.height != previousMode.height
        || mode.frequency != previousMode.frequency;
    BOOL changeScale = scale != 0 && scale != previousScale.current;

    if (!changeMode)
        return changeScale ? DisplaySetScale(display, scale, error) : TRUE;

    // The new mode is applied without being saved. If the monitor can't show it, nobody can
    // confirm it, and restoring the saved mode puts the picture back.
    if (!DisplaySetMode(display, mode.width, mode.height, mode.frequency, FALSE, error))
        return FALSE;

    BOOL ok = TRUE;
    BOOL scaleChanged = FALSE;
    BOOL keep = FALSE;

    if (changeScale) {
        ok = DisplaySetScale(display, scale, error);
        scaleChanged = ok;
    }
    if (ok) {
        keep = DialogBoxParamW(GetModuleHandleW(NULL), MAKEINTRESOURCEW(IDD_KEEP), dialog, KeepProc, 0) == IDOK;
        if (keep)
            ok = DisplaySetMode(display, mode.width, mode.height, mode.frequency, TRUE, error);
    }

    if (!keep) {
        WCHAR revertError[DISPLAY_ERROR_LENGTH];
        BOOL reverted = DisplayRestoreSavedMode(display, revertError);
        if (reverted && scaleChanged)
            reverted = DisplaySetScale(display, previousScale.current, revertError);
        if (!reverted && ok) {
            StringCchCopyW(error, DISPLAY_ERROR_LENGTH, revertError);
            ok = FALSE;
        }
    }
    return ok;
}

static void Apply(HWND dialog)
{
    LPARAM index;
    LPARAM resolution;
    LPARAM refresh;
    LPARAM scale = 0;
    WCHAR error[DISPLAY_ERROR_LENGTH] = L"";
    WCHAR deviceName[CCHDEVICENAME];

    if (!ComboSelected(GetDlgItem(dialog, IDC_DISPLAY), &index)
        || !ComboSelected(GetDlgItem(dialog, IDC_RESOLUTION), &resolution)
        || !ComboSelected(GetDlgItem(dialog, IDC_REFRESH), &refresh))
        return;
    ComboSelected(GetDlgItem(dialog, IDC_SCALE), &scale); // stays 0 when scale is not available

    const Display *display = &g_displays[index];
    DisplayMode mode = { LOWORD(resolution), HIWORD(resolution), (int)refresh };

    if (!ApplySettings(dialog, display, mode, (int)scale, error))
        ShowError(dialog, error);

    // Reloading replaces g_displays, which `display` points into.
    StringCchCopyW(deviceName, ARRAYSIZE(deviceName), display->deviceName);
    LoadDisplays(dialog, deviceName);
}

static INT_PTR CALLBACK MainProc(HWND dialog, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message) {
    case WM_INITDIALOG:
        SetWindowTextW(dialog, APP_NAME L" " APP_VERSION_W);
        // "(C)" in the shared copyright text becomes a real copyright sign here.
        SetDlgItemTextW(dialog, IDC_ABOUT, APP_NAME L" " APP_VERSION_W L"  \x00B7  Copyright \x00A9 2023-2026 Joe Farrell");
        SetDlgItemTextW(dialog, IDC_AUTHOR, L"<a>" APP_AUTHOR_URL L"</a>");
        SendDlgItemMessageW(dialog, IDC_RESOLUTION, CB_SETMINVISIBLE, 16, 0);
        SendDlgItemMessageW(dialog, IDC_REFRESH, CB_SETMINVISIBLE, 16, 0);
        LoadDisplays(dialog, NULL);
        return TRUE;

    case WM_COMMAND:
        switch (LOWORD(wParam)) {
        case IDC_DISPLAY:
            if (HIWORD(wParam) == CBN_SELCHANGE)
                LoadSettings(dialog);
            return TRUE;

        case IDC_RESOLUTION:
            if (HIWORD(wParam) == CBN_SELCHANGE) {
                LPARAM refresh = 0;
                ComboSelected(GetDlgItem(dialog, IDC_REFRESH), &refresh);
                LoadRefreshRates(dialog, (int)refresh);
            }
            return TRUE;

        case IDOK:
            Apply(dialog);
            return TRUE;

        case IDCANCEL:
            EndDialog(dialog, 0);
            return TRUE;
        }
        break;

    case WM_NOTIFY: {
        const NMHDR *header = (const NMHDR *)lParam;
        if (header->idFrom == IDC_AUTHOR && (header->code == NM_CLICK || header->code == NM_RETURN)) {
            ShellExecuteW(dialog, L"open", APP_AUTHOR_URL, NULL, NULL, SW_SHOWNORMAL);
            return TRUE;
        }
        break;
    }

    case WM_CTLCOLORSTATIC: {
        // The two informational lines are drawn in grey.
        int id = GetDlgCtrlID((HWND)lParam);
        if (id == IDC_CURRENT || id == IDC_ABOUT) {
            SetTextColor((HDC)wParam, GetSysColor(COLOR_GRAYTEXT));
            SetBkColor((HDC)wParam, GetSysColor(COLOR_BTNFACE));
            return (INT_PTR)GetSysColorBrush(COLOR_BTNFACE);
        }
        break;
    }
    }
    return FALSE;
}

int WindowRun(void)
{
    INITCOMMONCONTROLSEX controls = { sizeof controls, ICC_STANDARD_CLASSES | ICC_LINK_CLASS };
    InitCommonControlsEx(&controls);

    INT_PTR result = DialogBoxParamW(GetModuleHandleW(NULL), MAKEINTRESOURCEW(IDD_MAIN), NULL, MainProc, 0);
    free(g_modes);
    g_modes = NULL;

    if (result == -1) {
        MessageBoxW(NULL, L"The ScreenResy window could not be opened.", APP_NAME, MB_OK | MB_ICONERROR);
        return 1;
    }
    return 0;
}
