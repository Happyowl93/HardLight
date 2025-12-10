using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
public sealed partial class DantalionComponent : Component
{
	/// <summary>
	///     Base thrall limit before blood / power bonuses
	/// </summary>
	[DataField]
	public int BaseThrallLimit = 1;

	/// <summary>
	///     Runtime tracking of enthralled entities
	/// </summary>
	[ViewVariables(VVAccess.ReadOnly)]
	public HashSet<EntityUid> Thralls = new();

	/// <summary>
	///     Whether Blood Bond is currently active
	/// </summary>
	[ViewVariables(VVAccess.ReadOnly)]
	public bool BloodBondActive = false;

	/// <summary>
	///     Loop id for Blood Bond to prevent duplicate loops
	/// </summary>
	public int BloodBondLoopId = 0;

	/// <summary>
	///     Thralls currently linked via Blood Bond
	/// </summary>
	[ViewVariables(VVAccess.ReadOnly)]
	public HashSet<EntityUid> BloodBondLinkedThralls = new();
	
	[DataField]
	public EntProtoId rallyOverlayEffect = "VampireRallyOverlayEffect";
}