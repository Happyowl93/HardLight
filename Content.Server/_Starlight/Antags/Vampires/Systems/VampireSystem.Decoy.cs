using Robust.Shared.Audio;

namespace Content.Server._Starlight.Antags.Vampires;
// shitcode
public sealed partial class VampireSystem
{
    private const string DecoyFlashEffectId = "GrenadeFlashEffect";
    private const float DecoyFlashRange = 3f;
    private static readonly TimeSpan _decoyFlashDuration = TimeSpan.FromSeconds(4);
    private static readonly SoundSpecifier _decoyFlashSound = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

    private void TriggerDecoyFlash(EntityUid uid)
    {
        var coords = _transform.GetMapCoordinates(uid);
        var entityCoords = Transform(uid).Coordinates;
        
        // Apply real flash effect (blindness + slowdown) to nearby entities
        _flash.FlashArea(uid, null, DecoyFlashRange, _decoyFlashDuration, slowTo: 0.5f, displayPopup: true, probability: 1f);
        _audio.PlayPvs(_decoyFlashSound, entityCoords, AudioParams.Default.WithVolume(1f).WithMaxDistance(DecoyFlashRange));
        
        // Spawn visual effect
        EntityManager.SpawnEntity(DecoyFlashEffectId, coords);
        QueueDel(uid);
    }
}
