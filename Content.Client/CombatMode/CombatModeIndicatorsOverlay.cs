using System.Numerics;
using Content.Shared._Starlight.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Shared.Graphics.RSI;

namespace Content.Client.CombatMode;

/// <summary>
///   This shows something like crosshairs for the combat mode next to the mouse cursor.
///   For weapons with the gun class, a crosshair of one type is displayed,
///   while for all other types of weapons and items in hand, as well as for an empty hand,
///   a crosshair of a different type is displayed. These crosshairs simply show the state of combat mode (on|off).
/// </summary>
public sealed class CombatModeIndicatorsOverlay : Overlay
{
    private readonly IInputManager _inputManager;
    private readonly IEntityManager _entMan;
    private readonly IEyeManager _eye;
    private readonly CombatModeSystem _combat;
    private readonly HandsSystem _hands = default!;
    private readonly IClyde _clyde = default!;

    private readonly SightPrototype? _gunSight;
    private readonly SightPrototype? _gunBoltSight;
    private readonly SightPrototype? _meleeSight;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CombatModeIndicatorsOverlay(IInputManager input, IEntityManager entMan, IPrototypeManager prototypes,
            IEyeManager eye, CombatModeSystem combatSys, HandsSystem hands, IClyde clyde, SightPrototype gunSight, SightPrototype meleeSight)
    {
        _inputManager = input;
        _entMan = entMan;
        _eye = eye;
        _combat = combatSys;
        _hands = hands;
        _gunSight = gunSight;
        _meleeSight = meleeSight;
        _clyde = clyde;

        if (_gunSight.BoltVariant != null)
            prototypes.TryIndex<SightPrototype>(_gunSight.BoltVariant, out _gunBoltSight);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_combat.IsInCombatMode())
        {
            return false;
            _clyde.SetCursor(null);
        }

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouseScreenPosition = _inputManager.MouseScreenPosition;
        var mousePosMap = _eye.PixelToMap(mouseScreenPosition);
        if (mousePosMap.MapId != args.MapId)
            return;

        var handEntity = _hands.GetActiveHandEntity();
        var isHandGunItem = _entMan.HasComponent<GunComponent>(handEntity);
        var isGunBolted = true;
        if (_entMan.TryGetComponent(handEntity, out ChamberMagazineAmmoProviderComponent? chamber))
            isGunBolted = chamber.BoltClosed ?? true;

        var mousePos = mouseScreenPosition.Position;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var limitedScale = uiScale > 1.25f ? 1.25f : uiScale;

        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();

        var sight = isHandGunItem ? (isGunBolted || _gunBoltSight == null ? _gunSight : _gunBoltSight) : _meleeSight;
        if (sight != null)
        {
            if (!sight.ShowCursor)
                _clyde.SetCursor(_clyde.CreateCursor(new SixLabors.ImageSharp.Image<Rgba32>(32, 32), Vector2i.Zero));
            else
                _clyde.SetCursor(null);
            var rsiState = spriteSys.RsiStateLike(sight.Sprite);
            if (rsiState.IsAnimated && rsiState.AnimationFrameCount == 3)
            {
                var bracket1 = rsiState.GetFrame(RsiDirection.South, 0);
                var sightTexture = rsiState.GetFrame(RsiDirection.South, 1);
                var bracket2 = rsiState.GetFrame(RsiDirection.South, 2);
                DrawSightPartial(sightTexture, bracket1, bracket2, args.ScreenHandle, mousePos, limitedScale * Math.Clamp(sight.Scale, 0f, 1f), sight.MainColor, sight.StrokeColor);
            }
            else
            {
                var sightTexture = spriteSys.Frame0(sight.Sprite);
                DrawSight(sightTexture, args.ScreenHandle, mousePos, limitedScale * Math.Clamp(sight.Scale, 0f, 1f), sight.MainColor, sight.StrokeColor);
            }
        }
    }

    private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale, Color mainColor, Color strokeColor)
    {
        var sightSize = sight.Size * scale;
        var expandedSize = sightSize + new Vector2(7f, 7f);

        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - sightSize * 0.5f, sightSize), strokeColor);
        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - expandedSize * 0.5f, expandedSize), mainColor);
    }

    private void DrawSightPartial(Texture sight, Texture bracket1, Texture bracket2, DrawingHandleScreen screen, Vector2 centerPos, float scale, Color mainColor, Color strokeColor, float spread = 6f)
    {
        var sightSize = sight.Size * scale;
        DrawSight(sight, screen, centerPos, scale, mainColor, strokeColor);

        var bracketSize1 = bracket1.Size * scale;
        var bracketSize2 = bracket2.Size * scale;

        float offset = sightSize.X * 0.5f + spread;

        var bracket1Pos = centerPos + new Vector2(-offset - bracketSize1.X * 0.5f, 0f);
        var bracket2Pos = centerPos + new Vector2(offset + bracketSize2.X * 0.5f, 0f);

        screen.DrawTextureRect(bracket1,
            UIBox2.FromDimensions(bracket1Pos - bracketSize1 * 0.5f, bracketSize1), strokeColor);
        screen.DrawTextureRect(bracket2,
            UIBox2.FromDimensions(bracket2Pos - bracketSize2 * 0.5f, bracketSize2), strokeColor);

        var bracketExpanded1 = bracketSize1 + new Vector2(7f, 7f);
        var bracketExpanded2 = bracketSize2 + new Vector2(7f, 7f);

        screen.DrawTextureRect(bracket1,
            UIBox2.FromDimensions(bracket1Pos - bracketExpanded1 * 0.5f, bracketExpanded1), mainColor);
        screen.DrawTextureRect(bracket2,
            UIBox2.FromDimensions(bracket2Pos - bracketExpanded2 * 0.5f, bracketExpanded2), mainColor);
    }

    private sealed class DummyCursor : ICursor
    {
        public void Dispose()
        {
        }
    }
}
