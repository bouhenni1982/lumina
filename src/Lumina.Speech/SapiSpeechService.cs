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

        _queue.Enqueue(request);
        Drain();
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
