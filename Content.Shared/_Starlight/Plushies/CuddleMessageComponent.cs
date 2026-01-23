// Component for items that display cuddle messages when used in hand
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Plushies;

/// <summary>
/// Component for items that show a configurable cuddle message when used in hand (Z key).
/// The message supports a {0} placeholder that will be replaced with the character's name.
/// This component is networked so both server and client have access to the message data.
/// </summary>
/// <example>
/// Example usage in YAML:
/// <code>
/// - type: CuddleMessage
///   message: "{0} cuddles the blue shark."
/// </code>
/// </example>
[RegisterComponent, NetworkedComponent]
public sealed partial class CuddleMessageComponent : Component
{
    /// <summary>
    /// The message template to display when the item is used in hand.
    /// Use {0} as a placeholder for the character's name.
    /// Example: "{0} cuddles the plushie." becomes "John Smith cuddles the plushie."
    /// </summary>
    [DataField]
    public string Message = "{0} cuddles the plushie.";

    /// <summary>
    /// Whether this item should be prevented from being eaten.
    /// Set to true to block ingestion and prevent the eating doafter.
    /// </summary>
    [DataField]
    public bool PreventEating = true;
}
