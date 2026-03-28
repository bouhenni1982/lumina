using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Output.Inspection;

public sealed class CompositeInspectorSink : IInspectorSink
{
    private readonly IInspectorSink[] _sinks;

    public CompositeInspectorSink(params IInspectorSink[] sinks)
    {
        _sinks = sinks;
    }

    public bool IsEnabled => _sinks.Any(static sink => sink.IsEnabled);

    public void Record(ScreenEvent screenEvent, SpeechRequest speechRequest)
    {
        foreach (IInspectorSink sink in _sinks)
        {
            sink.Record(screenEvent, speechRequest);
        }
    }

    public void Toggle()
    {
        foreach (IInspectorSink sink in _sinks)
        {
            sink.Toggle();
        }
    }

    public void Dispose()
    {
        foreach (IInspectorSink sink in _sinks)
        {
            sink.Dispose();
        }
    }
}
