using Lumina.Core.Models;

namespace Lumina.Core.Abstractions;

public interface IAccessibilityService : IDisposable
{
    event EventHandler<ScreenEvent>? EventRaised;
    void Start();
}
