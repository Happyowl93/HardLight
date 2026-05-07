using Content.Shared._Common.Consent;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if the target entity has consented to a specific toggle.
/// Entities without a <see cref="ConsentComponent"/> are treated as consenting to everything.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class ConsentEntityConditionSystem : EntityConditionSystem<TransformComponent, ConsentCondition>
{
    [Dependency] private readonly SharedConsentSystem _consent = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<ConsentCondition> args)
    {
        args.Result = _consent.HasConsent(entity.Owner, args.Condition.Consent);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class ConsentCondition : EntityConditionBase<ConsentCondition>
{
    [DataField(required: true)]
    public ProtoId<ConsentTogglePrototype> Consent = default!;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-consent", ("consent", Consent));
}
