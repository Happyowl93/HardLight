using Content.Shared.Hands.EntitySystems;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Roles.Components;
using Content.Server._Starlight.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using SELFRuleComponent = Content.Server._Starlight.GameTicking.Rules.Components.SELFRuleComponent;

namespace Content.Server._Starlight.GameTicking.Rules;

public sealed class SELFRuleSystem : GameRuleSystem<SELFRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SELFRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<SELFRuleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon SELF activation
    private void AfterAntagSelected(Entity<SELFRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<SELFRuleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        
        int hands = 0;
        
        if (ent == null)
            return;
        
        foreach (var hand in _hands.EnumerateHands(ent.Value))
            hands += 1;
            
        if (hands == 0)
            return;
        
        Logger.Warning($"Hands count: {hands}");
            
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
        var briefing = isHuman
            ? Loc.GetString("self-role-greeting-human")
            : Loc.GetString("self-role-greeting-animal");

        if (isHuman)
            briefing += "\n \n" + Loc.GetString("self-role-greeting-equipment") + "\n";

        return briefing;
    }
}
