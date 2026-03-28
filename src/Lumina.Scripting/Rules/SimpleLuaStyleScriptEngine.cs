using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Scripting.Rules;

public sealed class SimpleLuaStyleScriptEngine : IScriptEngine
{
    public SpeechRequest Handle(ScreenEvent screenEvent)
    {
        AccessibleNode node = screenEvent.Node;

        string text = node.Role switch
        {
            "button" => $"زر {node.Name}",
            "edit" => $"حقل تحرير {node.Name}",
            "document" => $"مستند {node.Name}",
            _ => $"{node.Role} {node.Name}"
        };

        if (!string.IsNullOrWhiteSpace(node.Value))
        {
            text = $"{text}. القيمة {node.Value}";
        }

        return new SpeechRequest(
            Text: text,
            Priority: screenEvent.Priority,
            Interrupt: true);
    }
}
