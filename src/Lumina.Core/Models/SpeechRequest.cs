namespace Lumina.Core.Models;

public sealed record SpeechRequest(
    string Text,
    int Priority,
    bool Interrupt);
