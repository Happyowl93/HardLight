using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Xenobiology.MiscItems;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeFireproofPotionComponent : Component
{
    /// <summary>
    /// How many uses of this potion remain.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int RemainingUses = 3;
}