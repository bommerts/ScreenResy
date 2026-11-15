#pragma once

#include <windows.h>
#include "version.h"

#define WIDEN_(text) L##text
#define WIDEN(text)  WIDEN_(text)

#define APP_NAME        L"ScreenResy"
#define APP_VERSION_W   WIDEN(APP_VERSION)
#define APP_COPYRIGHT_W WIDEN(APP_COPYRIGHT)
#define APP_AUTHOR_URL  L"https://www.linkedin.com/in/joefarrell/"
#define APP_PROJECT_URL L"https://github.com/bommerts/ScreenResy"

// Runs ScreenResy as a command-line tool. The arguments exclude the program name.
// Returns the process exit code.
int CommandLineRun(int argc, wchar_t **argv);

// Opens the ScreenResy window and returns when it is closed. Returns the process exit code.
int WindowRun(void);
