using System.Runtime.InteropServices;
using Accessibility;

namespace Lumina.Accessibility.Windows;

internal sealed partial class Ia2FallbackProbe
{
    private static readonly Guid IAccessibleGuid = typeof(IAccessible).GUID;
    private static readonly Guid IAccessible2Guid = new("E89F726E-C4F4-4C19-BB19-B647D7FA8478");
    private static readonly HashSet<string> BrowserWindowClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Chrome_WidgetWin_1",
        "Chrome_RenderWidgetHostHWND",
        "MozillaWindowClass",
        "MozillaContentWindowClass",
        "ApplicationFrameWindow"
    };

    public Ia2AccessibleInfo? TryGetInfo(int nativeWindowHandle, string processName)
    {
        if (nativeWindowHandle == 0)
        {
            return null;
        }

        try
        {
            Guid guid = IAccessibleGuid;
            int result = AccessibleObjectFromWindow(
                new IntPtr(nativeWindowHandle),
                ObjectIdClient,
                ref guid,
                out object? accessibleObject);

            if (result != 0 || accessibleObject is not IAccessible accessible)
            {
                return null;
            }

            string windowClass = GetWindowClassName(nativeWindowHandle);
            bool queryServiceAvailable = SupportsIa2ThroughServiceProvider(accessibleObject);
            bool browserLikeWindow = BrowserWindowClasses.Contains(windowClass);
            bool browserLikeProcess = IsIa2FirstProcess(processName);

            if (!queryServiceAvailable && !browserLikeWindow && !browserLikeProcess)
            {
                return null;
            }

            object childId = ChildIdSelf;
            string? description = SafeGet(() => accessible.get_accDescription(childId));
            string? keyboardShortcut = SafeGet(() => accessible.get_accKeyboardShortcut(childId));

            return new Ia2AccessibleInfo(
                Framework: ResolveFramework(windowClass, processName),
                Description: description,
                KeyboardShortcut: keyboardShortcut,
                IsAvailable: queryServiceAvailable,
                WindowClass: windowClass);
        }
        catch
        {
            return null;
        }
    }

    private static bool SupportsIa2ThroughServiceProvider(object accessibleObject)
    {
        if (accessibleObject is not IComServiceProvider serviceProvider)
        {
            return false;
        }

        object? queriedObject = null;

        try
        {
            Guid accessibleGuid = IAccessibleGuid;
            Guid accessible2Guid = IAccessible2Guid;
            int queryResult = serviceProvider.QueryService(
                ref accessibleGuid,
                ref accessible2Guid,
                out queriedObject);

            if (queryResult == 0 && queriedObject is not null)
            {
                return true;
            }

            Guid serviceAccessible2Guid = IAccessible2Guid;
            Guid interfaceAccessible2Guid = IAccessible2Guid;
            queryResult = serviceProvider.QueryService(
                ref serviceAccessible2Guid,
                ref interfaceAccessible2Guid,
                out queriedObject);

            return queryResult == 0 && queriedObject is not null;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (queriedObject is not null && Marshal.IsComObject(queriedObject))
            {
                Marshal.ReleaseComObject(queriedObject);
            }
        }
    }

    private static bool IsIa2FirstProcess(string processName) =>
        processName.Equals("firefox", StringComparison.OrdinalIgnoreCase) ||
        processName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
        processName.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
        processName.Equals("electron", StringComparison.OrdinalIgnoreCase) ||
        processName.EndsWith("helper", StringComparison.OrdinalIgnoreCase);

    private static string ResolveFramework(string windowClass, string processName)
    {
        if (processName.Equals("firefox", StringComparison.OrdinalIgnoreCase) ||
            windowClass.Contains("Mozilla", StringComparison.OrdinalIgnoreCase))
        {
            return "firefox";
        }

        if (processName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
            windowClass.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            return "chromium";
        }

        if (processName.Equals("electron", StringComparison.OrdinalIgnoreCase))
        {
            return "electron";
        }

        return "generic";
    }

    private static string GetWindowClassName(int nativeWindowHandle)
    {
        Span<char> buffer = stackalloc char[256];
        int written = GetClassNameW(new IntPtr(nativeWindowHandle), buffer, buffer.Length);
        if (written <= 0)
        {
            return "unknown";
        }

        return new string(buffer[..written]);
    }

    private static string? SafeGet(Func<string> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            return null;
        }
    }

    private const uint ObjectIdClient = 0xFFFFFFFC;
    private const int ChildIdSelf = 0;

    [LibraryImport("oleacc.dll")]
    private static partial int AccessibleObjectFromWindow(
        IntPtr hwnd,
        uint objectId,
        ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out object? accessibleObject);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetClassNameW(
        IntPtr hwnd,
        Span<char> className,
        int maxCount);
}

[ComImport]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IComServiceProvider
{
    [PreserveSig]
    int QueryService(
        ref Guid serviceGuid,
        ref Guid interfaceGuid,
        [MarshalAs(UnmanagedType.Interface)] out object? queriedObject);
}

internal sealed record Ia2AccessibleInfo(
    string Framework,
    string? Description,
    string? KeyboardShortcut,
    bool IsAvailable,
    string WindowClass);
