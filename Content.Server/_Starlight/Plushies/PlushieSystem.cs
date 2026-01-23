// Plushie interaction system - handles cuddle messages for plushies
using Content.Shared._Starlight.Plushies;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server._Starlight.Plushies;

/// <summary>
/// Server-side system that handles cuddle messages when using plushies in hand.
/// </summary>
public sealed class PlushieSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to UseInHandEvent (triggered by Z key when holding item)
        SubscribeLocalEvent<CuddleMessageComponent, UseInHandEvent>(OnUseInHand);
    }

    /// <summary>
    /// Shows a cuddle message when the plushie is used in hand.
    /// Formats the message with the character's name and displays it only to the user.
    /// </summary>
    private void OnUseInHand(Entity<CuddleMessageComponent> entity, ref UseInHandEvent args)
    {
        // Get the character name from the entity metadata
        var userName = MetaData(args.User).EntityName;
        
        // Format the message template with the character name (replaces {0} placeholder)
        var message = string.Format(entity.Comp.Message, userName);
        
        // Display the popup at the plushie's location, visible only to the user
        _popup.PopupEntity(message, entity, args.User);
        
        // Mark event as handled
        args.Handled = true;
    }
}
