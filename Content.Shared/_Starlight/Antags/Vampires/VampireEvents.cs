using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Antags.Vampires;

#region Basic Abilities

public sealed partial class VampireGlareActionEvent : InstantActionEvent
{
    /// <summary>
    /// The distance at which entities will be blinded.
    /// </summary>
    [DataField]
    public float Range = 1f;

    /// <summary>
    /// How much we need to apply stamina damage on entity in front of glare source
    /// </summary>
    [DataField]
    public float FrontStaminaDamage = 30f;

    /// <summary>
    /// How much we need to apply stamina damage on entity behind of glare source
    /// </summary>
    [DataField]
    public float BehindStaminaDamage = 30f;

    /// <summary>
    /// How much we need to apply stamina damage on entity which is located to the left or right of glare source
    /// </summary>
    [DataField]
    public float SideStaminaDamage = 40f;

    /// <summary>
    /// How much we need to apply additional stamina damage on entity in front of glare source.
    /// </summary>
    [DataField]
    public float DotStaminaDamage = 15f;

    /// <summary>
    /// How many seconds do we need to mute entity in front of glare source.
    /// </summary>
    [DataField]
    public int MuteDuration = 8;
}

public sealed partial class VampireRejuvenateIActionEvent : InstantActionEvent;

public sealed partial class VampireRejuvenateIIActionEvent : InstantActionEvent;

public sealed partial class VampireClassSelectActionEvent : InstantActionEvent;

public sealed partial class VampireToggleFangsActionEvent : InstantActionEvent;

#endregion

#region Hemomancer

// Vampiric Claws
public sealed partial class VampireHemomancerClawsActionEvent : InstantActionEvent;

// Blood Tendril
public sealed partial class VampireHemomancerTendrilsActionEvent : WorldTargetActionEvent
{
    [DataField]
    public float Delay = 1.0f;

    [DataField]
    public float SlowMultiplier = 0.3f;

    [DataField]
    public float SlowDuration = 2.0f;

    [DataField]
    public float ToxinDamage = 33.0f;

    [DataField]
    public bool SpawnVisuals = true;

    [DataField]
    public float PositionOffset = 0.5f;

    [DataField]
    public float TargetRange = 0.9f;

    [DataField]
    public float VisualSpawnDelay = 0.5f;

    [DataField]
    public float MinDelay = 0.0f;

    [DataField]
    public float MinSlowDuration = 0.1f;

    [DataField]
    public float MinSlowMultiplier = 0.05f;
}

// Blood Barrier
public sealed partial class VampireBloodBarrierActionEvent : WorldTargetActionEvent
{
    [DataField]
    public int BarrierCount = 3;
}

// Blood Pool
public sealed partial class VampireSanguinePoolActionEvent : InstantActionEvent
{
    [DataField]
    public float BloodDripInterval = 1.0f;

    [DataField]
    public int Duration = 8;
}

// Blood Eruption
public sealed partial class VampireBloodEruptionActionEvent : InstantActionEvent
{
    [DataField]
    public float Range = 10f;

    [DataField]
    public int Damage = 15;

    [DataField]
    public float TargetRange = 2f;
}

// The Blood Bringer's Rite
public sealed partial class VampireBloodBringersRiteActionEvent : InstantActionEvent
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public float Damage = 5f;

    [DataField]
    public float MaxTargetBlood = 10f;

    [DataField]
    public float HealBrute = 8f;

    [DataField]
    public float HealBurn = 2f;

    [DataField]
    public float HealStamina = 15f;

    [DataField]
    public float ToggleInterval = 2f;

    [DataField]
    public int Cost = 10;
}

#endregion

#region Umbrae

// Cloak of Darkness
public sealed partial class VampireCloakOfDarknessActionEvent : InstantActionEvent;

// Shadow Snare
public sealed partial class VampireShadowSnareActionEvent : WorldTargetActionEvent
{
    /// <summary>
    /// Decay tick interval in seconds
    /// </summary>
    [DataField]
    public float TickInterval = 2f;

    /// <summary>
    /// Health decay per tick in dark areas (light <= 5)
    /// </summary>
    [DataField]
    public float DamageDark = 0f;

    /// <summary>
    /// Health decay per tick in normal light (5 < light <= 10)
    /// </summary>
    [DataField]
    public float DamageNormal = 10f;

    /// <summary>
    /// Health decay per tick in bright light (light > 10)
    /// </summary>
    [DataField]
    public float DamageBright = 25f;

    /// <summary>
    /// Max snares per player before oldest is removed
    /// </summary>
    [DataField]
    public int MaxPerPlayer = 3;

    /// <summary>
    /// Stealth visibility modifier for the trap
    /// </summary>
    [DataField]
    public float StealthVisibility = -0.3f;

    /// <summary>
    /// Position offset for centering on tile
    /// </summary>
    [DataField]
    public float PositionOffset = 0.5f;
}

// Soul Anchor
public sealed partial class VampireShadowAnchorActionEvent : InstantActionEvent;

// Dark Passage
public sealed partial class VampireDarkPassageActionEvent : WorldTargetActionEvent;

// Extinguish
public sealed partial class VampireExtinguishActionEvent : InstantActionEvent
{
    [DataField]
    public float Radius = 8f;
}

// Shadow Boxing
public sealed partial class VampireShadowBoxingActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float Interval = 0.4f;

    [DataField]
    public int BrutePerTick = 6;

    [DataField]
    public float Range = 2.1f;

    [DataField]
    public SoundSpecifier? HitSound;
}

[Serializable, NetSerializable]
public sealed class VampireShadowBoxingPunchEvent : EntityEventArgs
{
    public NetEntity Source { get; }
    public NetEntity Target { get; }

    public VampireShadowBoxingPunchEvent(NetEntity source, NetEntity target)
    {
        Source = source;
        Target = target;
    }
}

// Eternal Darkness
public sealed partial class VampireEternalDarknessActionEvent : InstantActionEvent
{
    [DataField]
    public int MaxTicks = 360;

    [DataField]
    public int BloodPerTick = 5;

    [DataField]
    public float FreezeRadius = 6f;

    [DataField]
    public float LightOffRadius = 4f;

    [DataField]
    public float TargetFreezeTemp = 233.15f;

    [DataField]
    public int TempDropInterval = 2;

    [DataField]
    public float TempDropPerInterval = 60f;
}

#endregion

#region Dantalion

public sealed partial class VampireEnthrallActionEvent : EntityTargetActionEvent
{
    /// <summary>
    ///     Channel duration, in seconds, before the target is enthralled
    /// </summary>
    [DataField]
    public float ChannelTime = 1f;
}

[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class VampireEnthrallDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int BloodCost;
}

public sealed partial class VampirePacifyActionEvent : EntityTargetActionEvent
{
    [DataField("pacifyDuration")]
    public float PacifyDuration = 40f;
}
        
public sealed partial class VampireSubspaceSwapActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float SlowDuration = 4f;

    [DataField]
    public float SlowMultiplier = 0.4f;
    [DataField]
    public float HysteriaDuration = 15f;
}

public sealed partial class VampireDecoyActionEvent : InstantActionEvent
{
    [DataField]
    public float DecoyDuration = 6f;
    [DataField]
    public float InvisibilityDuration = 6f;
}

public sealed partial class VampireRallyThrallsActionEvent : InstantActionEvent
{
    /// <summary>
    ///     Range in tiles to find thralls
    /// </summary>
    [DataField]
    public float Range = 7f;
}

public sealed partial class VampireBloodBondActionEvent : InstantActionEvent
{
    /// <summary>
    ///     Range in tiles for blood bond link
    /// </summary>
    [DataField]
    public float Range = 7f;

    /// <summary>
    ///     Blood cost per second while active
    /// </summary>
    [DataField]
    public float BloodCostPerSecond = 2.5f;

    /// <summary>
    ///     Tick interval in seconds
    /// </summary>
    [DataField]
    public float TickInterval = 1f;
}

public sealed partial class VampireMassHysteriaActionEvent : InstantActionEvent
{
    /// <summary>
    ///     Range in tiles to affect targets
    /// </summary>
    [DataField]
    public float Range = 8f;

    /// <summary>
    ///     Duration of the flash effect in seconds
    /// </summary>
    [DataField]
    public float FlashDuration = 3f;

    /// <summary>
    ///     Duration of the hysteria vision effect in seconds
    /// </summary>
    [DataField]
    public float HysteriaDuration = 30f;
}

#endregion

#region Gargantua

public sealed partial class VampireBloodSwellActionEvent : InstantActionEvent
{
    [DataField]
    public float Duration = 30f;
}

public sealed partial class VampireBloodRushActionEvent : InstantActionEvent
{
    [DataField]
    public float Duration = 10f;
}

public sealed partial class VampireSeismicStompActionEvent : InstantActionEvent
{
    /// <summary>
    ///     Radius of the stomp effect in tiles
    /// </summary>
    [DataField]
    public float Radius = 3f;

    /// <summary>
    ///     Distance to throw targets in tiles
    /// </summary>
    [DataField]
    public float ThrowDistance = 3f;
}

public sealed partial class VampireOverwhelmingForceActionEvent : InstantActionEvent;

public sealed partial class VampireDemonicGraspActionEvent : WorldTargetActionEvent
{
    /// <summary>
    ///     Maximum range of the grasp projectile
    /// </summary>
    [DataField]
    public float Range = 15f;

    /// <summary>
    ///     Duration of immobilization in seconds
    /// </summary>
    [DataField]
    public float ImmobilizeDuration = 5f;

    /// <summary>
    ///     Speed of the grasp projectile
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 15f;
}

public sealed partial class VampireChargeActionEvent : WorldTargetActionEvent
{
    /// <summary>
    ///     Brute damage dealt to creatures on impact
    /// </summary>
    [DataField]
    public float CreatureDamage = 60f;

    /// <summary>
    ///     Distance to throw creatures on impact
    /// </summary>
    [DataField]
    public float CreatureThrowDistance = 5f;

    /// <summary>
    ///     Structural damage dealt to structures/machinery
    /// </summary>
    [DataField]
    public float StructuralDamage = 150f;

    /// <summary>
    ///     Charge movement speed
    /// </summary>
    [DataField]
    public float ChargeSpeed = 25f;
}

/// <summary>
/// Event for syncing demonic grasp projectile visuals
/// </summary>
[Serializable, NetSerializable]
public sealed class VampireDemonicGraspVisualEvent : EntityEventArgs
{
    public NetEntity Source { get; }
    public NetCoordinates Target { get; }
    public float Speed { get; }

    public VampireDemonicGraspVisualEvent(NetEntity source, NetCoordinates target, float speed)
    {
        Source = source;
        Target = target;
        Speed = speed;
    }
}

#endregion