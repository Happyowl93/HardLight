
using System.Diagnostics.CodeAnalysis;

namespace Content.Client._NullLink;

public interface INullLinkPlayerResourcesManager
{
    event Action PlayerResourcesChanged;

    bool TryGetResource(string id, [NotNullWhen(true)] out double? value);
    void Initialize();
}