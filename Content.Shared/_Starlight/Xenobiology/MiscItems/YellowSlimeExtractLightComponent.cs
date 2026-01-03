using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Xenobiology.MiscItems;

[RegisterComponent, NetworkedComponent]
public sealed partial class YellowSlimeExtractLightComponent : Component
{
    /// <summary>
    /// Whether the light is large or small.
    /// </summary>
    [ViewVariables]
    public bool toggled = false;
}