using Lumina.Core.Models;

namespace Lumina.Core.Abstractions;

public interface IScriptEngine
{
    SpeechRequest Handle(ScreenEvent screenEvent);
}
