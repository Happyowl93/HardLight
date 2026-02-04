using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// A newline-separated list of saved labels for the hand labeler tool
    /// </summary>
    public static readonly CVarDef<string> HandLabelerSavedLabels =
        CVarDef.Create("interface.hand_labeler_saved_labels", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> RangedSight = 
        CVarDef.Create("interface.ranged_sight", "GunSight", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> MeleeSight =
        CVarDef.Create("interface.melee_sight", "MeleeSight", CVar.CLIENTONLY | CVar.ARCHIVE);
}
