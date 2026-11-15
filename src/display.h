// Reads and changes the resolution, refresh rate and scale of the active displays.
//
// Every function that can fail returns FALSE and writes a message fit to show the user into
// `error`, which must have room for DISPLAY_ERROR_LENGTH characters.
#pragma once

#include <windows.h>

#define DISPLAY_ERROR_LENGTH 256
#define DISPLAY_MAX          16

typedef struct DisplayMode {
    int width;
    int height;
    int frequency;
} DisplayMode;

// Scale percentages for one display. The maximum depends on its current resolution.
typedef struct ScaleInfo {
    int current;
    int recommended;
    int maximum;
} ScaleInfo;

typedef struct Display {
    int number;                      // the number in the device name: 2 for \\.\DISPLAY2
    WCHAR deviceName[CCHDEVICENAME];
    WCHAR friendlyName[64];
    BOOL isPrimary;
    LUID adapterId;
    UINT32 sourceId;
} Display;

// Every scale Windows offers. A display supports the steps up to its maximum.
extern const int ScaleSteps[];
extern const int ScaleStepCount;

// Fills `displays` (room for DISPLAY_MAX) with the active displays, ordered by number.
BOOL DisplayGetAll(Display *displays, int *count, WCHAR *error);

BOOL DisplayGetCurrentMode(const Display *display, DisplayMode *mode, WCHAR *error);

// The modes the display reports for its current orientation, largest first, without duplicates.
// The caller frees `*modes`.
BOOL DisplayGetModes(const Display *display, DisplayMode **modes, int *count, WCHAR *error);

// Changes the resolution and, unless `frequency` is 0, the refresh rate. With a frequency of 0
// Windows picks one. With `persist` the mode is saved and survives a restart; without it the
// mode lasts until DisplayRestoreSavedMode or sign-out.
BOOL DisplaySetMode(const Display *display, int width, int height, int frequency, BOOL persist, WCHAR *error);

// Returns to the saved mode, undoing a DisplaySetMode made without `persist`.
BOOL DisplayRestoreSavedMode(const Display *display, WCHAR *error);

BOOL DisplayGetScale(const Display *display, ScaleInfo *scale, WCHAR *error);
BOOL DisplaySetScale(const Display *display, int percent, WCHAR *error);

// Whether `percent` is one of ScaleSteps.
BOOL DisplayIsScaleStep(int percent);

// Writes "100, 125, 150, ..." into `text`.
void DisplayFormatScaleSteps(WCHAR *text, size_t length);
