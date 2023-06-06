#include <stdio.h>
#include <Windows.h>
#include "Detours/detours.h"

#pragma comment(lib, "Detours/lib_detours.lib")

#ifdef EDITOR_REFRESH_EXPORTS
#define EDITOR_REFRESH_API __declspec(dllexport)
#else
#define EDITOR_REFRESH_API __declspec(dllimport)
#endif

#define DEBUGLOG
bool LogEnabled = false;
#define LOG_STR(str) "EditorRefresh: " str "\n"

static void debugLog(const char* format, ...)
{
#ifdef DEBUGLOG
    if (!LogEnabled)
        return;

    va_list arglist;
    va_start(arglist, format);
    vprintf(format, arglist);
    va_end(arglist);
#else
    UNREFERENCED_PARAMETER(format);
#endif
}

bool Detouring = false;

bool Enabled = false;

HWND MainWindow;

const wchar_t* MainWindowClassName = L"UnityContainerWndClass";

static auto real_GetForegroundWindow = ::GetForegroundWindow;



EDITOR_REFRESH_API void SetDebugLog(bool enabled)
{
    LogEnabled = enabled;
}

EDITOR_REFRESH_API void SetEnabled(bool enabled)
{
    Enabled = enabled;
}




static HWND WINAPI detour_GetForegroundWindow()
{
    debugLog(LOG_STR("detour_GetForegroundWindow"));

    return Enabled
        ? MainWindow
        : real_GetForegroundWindow();
}




bool DoDetourAttach()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());

    DetourAttach(&(PVOID&)real_GetForegroundWindow, detour_GetForegroundWindow);

    LONG error = DetourTransactionCommit();

    if (error == NO_ERROR)
    {
        debugLog(LOG_STR("Detour attached successfully"), error);
        Detouring = true;
        return true;
    }
    else
    {
        debugLog(LOG_STR("Error in detour attach: %ld"), error);
        return false;
    }
}

bool DoDetourDetach()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());

    DetourDetach(&(PVOID&)real_GetForegroundWindow, detour_GetForegroundWindow);

    LONG error = DetourTransactionCommit();

    if (error == NO_ERROR)
    {
        debugLog(LOG_STR("Detour detached successfully"), error);
        Detouring = false;
        return true;
    }
    else
    {
        debugLog(LOG_STR("Error in detour detach: %ld"), error);
        return false;
    }
}

BOOL CALLBACK SearchForMainWindowProc(HWND hwnd, LPARAM lParam)
{
    DWORD lpdwProcessId;
    GetWindowThreadProcessId(hwnd, &lpdwProcessId);

    // If the window belongs to our process
    if (lpdwProcessId == lParam)
    {
        HWND ancestor = GetParent(hwnd);

        // Check that this is not an owned window (filters out secondary windows)
        if (ancestor == NULL)
        {
            wchar_t className[256];

            GetClassName(hwnd, className, 256);

            // Check for the desired class name (filters out IME windows)
            if (wcscmp(className, MainWindowClassName) == 0)
            {
                wchar_t title[256];
                GetWindowText(hwnd, title, 256);

                printf(LOG_STR("SearchForMainWindowProc: Found main window handle: %zx | %ls | %ls"), (size_t)hwnd, title, className);

                MainWindow = hwnd;
                return FALSE;
            }
        }
    }
    return TRUE;
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD dwReason, LPVOID reserved)
{
    UNREFERENCED_PARAMETER(hinst);
    UNREFERENCED_PARAMETER(reserved);

    if (DetourIsHelperProcess())
        return TRUE;

    if (dwReason == DLL_PROCESS_ATTACH)
    {
        DetourRestoreAfterWith();
        DoDetourAttach();

        HWND g_HWND = NULL;
        
        DWORD procID = GetCurrentProcessId();
        EnumWindows(SearchForMainWindowProc, procID);
    }
    else if (dwReason == DLL_PROCESS_DETACH)
    {
        DoDetourDetach();
    }
    return TRUE;
}
