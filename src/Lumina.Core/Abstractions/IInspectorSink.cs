using Lumina.Core.Models;

namespace Lumina.Core.Abstractions;

public interface IInspectorSink : IDisposable
{
    void Record(ScreenEvent screenEvent, SpeechRequest speechRequest);
}
