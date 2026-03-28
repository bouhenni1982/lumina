using Lumina.Core.Abstractions;

namespace Lumina.Core.Services;

public sealed class LuminaRuntime : IDisposable
{
    private readonly IAccessibilityService _accessibilityService;
    private readonly IScriptEngine _scriptEngine;
    private readonly ISpeechService _speechService;
    private readonly EventFilter _eventFilter = new();

    public LuminaRuntime(
        IAccessibilityService accessibilityService,
        IScriptEngine scriptEngine,
        ISpeechService speechService)
    {
        _accessibilityService = accessibilityService;
        _scriptEngine = scriptEngine;
        _speechService = speechService;
    }

    public void Start()
    {
        _accessibilityService.EventRaised += OnEventRaised;
        _accessibilityService.Start();
    }

    private void OnEventRaised(object? sender, Models.ScreenEvent screenEvent)
    {
        if (!_eventFilter.ShouldProcess(screenEvent))
        {
            return;
        }

        Models.SpeechRequest speech = _scriptEngine.Handle(screenEvent);
        if (!string.IsNullOrWhiteSpace(speech.Text))
        {
            _speechService.Enqueue(speech);
        }
    }

    public void Dispose()
    {
        _accessibilityService.EventRaised -= OnEventRaised;
        _accessibilityService.Dispose();
        _speechService.Dispose();
    }
}
