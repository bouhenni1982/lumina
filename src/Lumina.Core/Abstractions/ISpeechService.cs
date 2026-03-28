using Lumina.Core.Models;

namespace Lumina.Core.Abstractions;

public interface ISpeechService : IDisposable
{
    void Enqueue(SpeechRequest request);
    void RepeatLast();
}
