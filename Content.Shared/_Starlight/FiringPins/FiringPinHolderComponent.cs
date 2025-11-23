using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.FiringPins;

[RegisterComponent, NetworkedComponent]
public sealed partial class FiringPinHolderComponent : Component
{
    /// <summary>
    /// How the pin is removed from the gun
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> PinExtractionMethod = "Screwing";

    /// <summary>
    /// What pins actually fit this gun?
    /// </summary>
    [DataField]
    public List<ProtoId<FiringPinClassPrototype>> SupportedPins = [ "Rifle" ];

    [DataField]
    public SoundSpecifier PinExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [DataField]
    public SoundSpecifier PinInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container PinContainer = default!;
    public const string PinContainerName = "firing_pin";

    /// <summary>
    /// is the firing pin in a state that the gun can fire?
    /// used instead of expensive checks every time the gun is fired
    /// </summary>
    [ViewVariables]
    public bool CanFire = true;
}