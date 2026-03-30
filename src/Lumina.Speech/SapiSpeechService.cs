using System.Collections.Concurrent;
using System.Speech.Synthesis;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;
using Lumina.Core.Services;

namespace Lumina.Speech;

public sealed class SapiSpeechService : ISpeechService
{
    private readonly SpeechSynthesizer _synthesizer = new();
    private readonly ConcurrentQueue<SpeechRequest> _queue = new();
    private readonly object _sync = new();
    private int _isDraining;
    private string? _lastSpokenText;

    public SapiSpeechService()
    {
        _synthesizer.Rate = 0;
        _synthesizer.Volume = 100;
    }

    public void Enqueue(SpeechRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return;
            }

            lock (_sync)
            {
                if (request.Interrupt)
                {
                    _synthesizer.SpeakAsyncCancelAll();
                    ClearQueue();
                }

                _lastSpokenText = request.Text;
            }

            _queue.Enqueue(request);
            Drain();
        }
        catch (Exception exception)
        {
            ErrorLogger.LogError(
                source: nameof(SapiSpeechService),
                message: "فشل إدراج طلب النطق في محرك SAPI.",
                exception: exception,
                context: new
                {
                    request.Text,
                    request.Priority,
                    request.Interrupt
                });
        }
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
                lock (_sync)
                {
                    _synthesizer.SpeakAsync(item.Text);
                }
            }
        }
        catch (Exception exception)
        {
            ErrorLogger.LogError(
                source: nameof(SapiSpeechService),
                message: "فشل تصريف طابور النطق.",
                exception: exception);
        }
        finally
        {
            Interlocked.Exchange(ref _isDraining, 0);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _synthesizer.SpeakAsyncCancelAll();
            ClearQueue();
            _synthesizer.Dispose();
        }
    }

    private void ClearQueue()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }
}
