namespace Lumina.Samples;

public sealed class LuaHost
{
    public string HandleFocus(string role, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "عنصر غير مسمى";
        }

        return role switch
        {
            "button" => $"زر {name}",
            "edit" => $"حقل تحرير {name}",
            _ => $"{role} {name}"
        };
    }
}
