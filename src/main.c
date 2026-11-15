#include "app.h"

// Before Windows 11 24H2 the consoleAllocationPolicy in app.manifest is ignored, so starting the
// app from Explorer opens a console window just for it. Being the only process attached to the
// console means it is that one, and not a terminal the user started the app from.
static void CloseOwnConsole(void)
{
    DWORD processes[2];
    if (GetConsoleProcessList(processes, ARRAYSIZE(processes)) == 1)
        FreeConsole();
}

// With no arguments opens the window; otherwise runs as a command-line tool.
int wmain(int argc, wchar_t **argv)
{
    if (argc > 1)
        return CommandLineRun(argc - 1, argv + 1);

    CloseOwnConsole();
    return WindowRun();
}
