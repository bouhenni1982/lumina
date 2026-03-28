using System.Runtime.InteropServices;
using Accessibility;

namespace Lumina.Accessibility.Windows;

internal sealed partial class MsaaFallbackProbe
{
    private static readonly Guid IAccessibleGuid = typeof(IAccessible).GUID;
    private const uint ObjectIdClient = 0xFFFFFFFC;
    private const int ChildIdSelf = 0;

    public MsaaAccessibleInfo? TryGetInfo(int nativeWindowHandle)
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

            object childId = ChildIdSelf;
            string? name = SafeGet(() => accessible.get_accName(childId));
            string? value = SafeGet(() => accessible.get_accValue(childId));
            string role = ResolveRole(accessible, childId);

            return new MsaaAccessibleInfo(
                Name: name,
                Role: role,
                Value: value);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveRole(IAccessible accessible, object childId)
    {
        try
        {
            object roleObject = accessible.get_accRole(childId);
            if (roleObject is int roleId)
            {
                return GetRoleText(roleId);
            }

            return roleObject?.ToString()?.ToLowerInvariant() ?? "control";
        }
        catch
        {
            return "control";
        }
    }

    private static string GetRoleText(int roleId)
    {
        Span<char> buffer = stackalloc char[128];
        uint written = GetRoleTextW((uint)roleId, buffer, (uint)buffer.Length);
        if (written == 0)
        {
            return "control";
        }

        return new string(buffer[..(int)written]).ToLowerInvariant().Replace(' ', '_');
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

    [LibraryImport("oleacc.dll")]
    private static partial int AccessibleObjectFromWindow(
        IntPtr hwnd,
        uint objectId,
        ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out object? accessibleObject);

    [LibraryImport("oleacc.dll", EntryPoint = "GetRoleTextW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint GetRoleTextW(
        uint role,
        Span<char> roleText,
        uint roleTextMax);
}

internal sealed record MsaaAccessibleInfo(
    string? Name,
    string Role,
    string? Value);
