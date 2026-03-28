using System.Collections.Concurrent;
using System.Speech.Synthesis;

namespace Lumina.Samples;

public sealed class SpeechQueue : IDisposable
{
    private readonly SpeechSynthesizer _synth = new();
    private readonly ConcurrentQueue<SpeechItem> _queue = new();

    public void Enqueue(string text, int priority = 0, bool interrupt = false)
    {
        if (interrupt)
        {
            _synth.SpeakAsyncCancelAll();
        }

        _queue.Enqueue(new SpeechItem(text, priority));
        Drain();
    }

    private void Drain()
    {
        while (_queue.TryDequeue(out SpeechItem? item))
        {
            _synth.SpeakAsync(item.Text);
        }
    }

    public void Dispose()
    {
        _synth.Dispose();
    }

    private sealed record SpeechItem(string Text, int Priority);
}
