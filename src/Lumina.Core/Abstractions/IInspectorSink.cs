using Lumina.Core.Models;

namespace Lumina.Core.Abstractions;

public interface IInspectorSink : IDisposable
{
    bool IsEnabled { get; }
    void Record(ScreenEvent screenEvent, SpeechRequest speechRequest);
    void Toggle();
}
