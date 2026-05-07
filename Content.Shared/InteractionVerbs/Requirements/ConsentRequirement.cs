using Content.Shared._Common.Consent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.InteractionVerbs.Requirements;

/// <summary>
///     Requires the target to have consented to a specific toggle.
///     Used to gate NSFW interaction verbs (e.g. Kiss, Lick) on the recipient's consent.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConsentRequirement : InvertableInteractionRequirement
{
    [DataField(required: true)]
    public ProtoId<ConsentTogglePrototype> Consent;

    public override bool IsMet(InteractionArgs args, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        var entSysMan = IoCManager.Resolve<IEntitySystemManager>();
        var consentSys = entSysMan.GetEntitySystem<SharedConsentSystem>();
        return consentSys.HasConsent(args.Target, Consent) ^ Inverted;
    }
}
