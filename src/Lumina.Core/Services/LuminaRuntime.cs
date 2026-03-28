using Lumina.Core.Abstractions;

namespace Lumina.Core.Services;

public sealed class LuminaRuntime : IDisposable
{
    private readonly IAccessibilityService _accessibilityService;
    private readonly IScriptEngine _scriptEngine;
    private readonly ISpeechService _speechService;
    private readonly IInspectorSink? _inspectorSink;
    private readonly EventFilter _eventFilter = new();

    public LuminaRuntime(
        IAccessibilityService accessibilityService,
        IScriptEngine scriptEngine,
        ISpeechService speechService,
        IInspectorSink? inspectorSink = null)
    {
        _accessibilityService = accessibilityService;
        _scriptEngine = scriptEngine;
        _speechService = speechService;
        _inspectorSink = inspectorSink;
    }

    public void Start()
    {
        _accessibilityService.EventRaised += OnEventRaised;
        _accessibilityService.Start();
    }

    private void OnEventRaised(object? sender, Models.ScreenEvent screenEvent)
    {
        try
        {
            if (!_eventFilter.ShouldProcess(screenEvent))
            {
                return;
            }

            Models.SpeechRequest speech = _scriptEngine.Handle(screenEvent);
            _inspectorSink?.Record(screenEvent, speech);
            if (!string.IsNullOrWhiteSpace(speech.Text))
            {
                _speechService.Enqueue(speech);
            }
        }
        catch (Exception exception)
        {
            ErrorLogger.LogError(
                source: nameof(LuminaRuntime),
                message: "حدث خطأ أثناء معالجة حدث إمكانية الوصول.",
                exception: exception,
                context: new
                {
                    screenEvent.EventType,
                    NodeName = screenEvent.Node.Name,
                    NodeRole = screenEvent.Node.Role,
                    screenEvent.Node.SemanticRole,
                    screenEvent.Node.ContextKind,
                    screenEvent.Node.SourceProcess
                });
        }
    }

    public void Dispose()
    {
        _accessibilityService.EventRaised -= OnEventRaised;
        _accessibilityService.Dispose();
        if (_scriptEngine is IDisposable disposableScriptEngine)
        {
            disposableScriptEngine.Dispose();
        }
        _inspectorSink?.Dispose();
        _speechService.Dispose();
    }
}
