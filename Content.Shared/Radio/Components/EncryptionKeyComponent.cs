using Content.Shared.Chat;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility; // Starlight
using Content.Shared._Starlight.Radio; // Starlight

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is currently used for providing access to channels for "HeadsetComponent"s.
///     It should be used for intercoms and other radios in future.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)] // Starlight edit
public sealed partial class EncryptionKeyComponent : Component
{
    [DataField, AutoNetworkedField] // Starlight edit
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    ///     This is the channel that will be used when using the default/department prefix (<see cref="SharedChatSystem.DefaultChannelKey"/>).
    /// </summary>
    [DataField, AutoNetworkedField] // Starlight edit
    public string? DefaultChannel; // Starlight edit | Use string to support custom channels
    
    //Starlight begin
    /// <summary>
    /// Set of custom channel data
    /// </summary>
    [DataField, AutoNetworkedField] public HashSet<CustomRadioChannelData> CustomChannels = [];

    // TODO: someone please make so you can modify the sprite layers through this component. Use ClientEncryptionKeySystem. 
    
    // [AutoNetworkedField] public string? OriginalBaseRsiPath;
    // [AutoNetworkedField] public string? OriginalIconRsiPath;
    // [AutoNetworkedField] public string? OriginalBaseState;
    // [AutoNetworkedField] public string? OriginalIconState;
    //
    // [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public string? BaseRsiPath;
    // [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public string? IconRsiPath;
    // [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public string? BaseState;
    // [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public string? IconState;
    //Starlight end
}
