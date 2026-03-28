using System.Collections.Concurrent;
using System.Speech.Synthesis;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Speech;

public sealed class SapiSpeechService : ISpeechService
{
    private readonly SpeechSynthesizer _synthesizer = new();
    private readonly ConcurrentQueue<SpeechRequest> _queue = new();
    private int _isDraining;
    private string? _lastSpokenText;

    public SapiSpeechService()
    {
        _synthesizer.Rate = 0;
        _synthesizer.Volume = 100;
    }

    public void Enqueue(SpeechRequest request)
    {
        if (request.Interrupt)
        {
            _synthesizer.SpeakAsyncCancelAll();
        }

        _lastSpokenText = request.Text;
        _queue.Enqueue(request);
        Drain();
    }

    public void RepeatLast()
    {
        if (string.IsNullOrWhiteSpace(_lastSpokenText))
        {
            return;
        }

        Enqueue(new SpeechRequest(_lastSpokenText, 100, true));
    }

    private void Drain()
    {
        if (Interlocked.Exchange(ref _isDraining, 1) == 1)
        {
            return;
        }

        try
        {
            while (_queue.TryDequeue(out SpeechRequest? item))
            {
                _synthesizer.SpeakAsync(item.Text);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isDraining, 0);
        }
    }

    public void Dispose()
    {
        _synthesizer.Dispose();
    }
}
