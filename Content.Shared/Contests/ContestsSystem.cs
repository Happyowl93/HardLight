namespace Content.Shared.Contests;

/// <summary>
/// Minimal stub of upstream Wizards' contests system. NovaSector forked before contests
/// landed; this stub returns 1f (no advantage, no penalty) so consumers like
/// <c>SharedInteractionVerbsSystem</c> compile and behave neutrally.
/// </summary>
public sealed class ContestsSystem : EntitySystem
{
    /// <summary>
    /// Always returns 1f — no contest advantage. Replace with a real implementation
    /// if/when NovaSector imports the full upstream contests system.
    /// </summary>
    public float MassContest(EntityUid? roller, EntityUid? target, bool useRange = false, float rangeFactor = 0f)
    {
        return 1f;
    }
}
