using Content.Server.Medical.SuitSensors;
using Content.Shared.Access.Systems;
using Content.Shared.Delivery;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Popups;
using Content.Shared._Starlight.Cargo.MailCompanion;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Cargo.MailCompanion;

public sealed class MailCompanionSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SuitSensorSystem _suitSensors = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private EntityQuery<SuitSensorComponent> _sensorQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _sensorQuery = GetEntityQuery<SuitSensorComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<MailCompanionComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MailCompanionComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<MailCompanionComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DeliveryComponent, DeliveryOpenedEvent>(OnDeliveryOpened);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MailCompanionComponent>();
        while (query.MoveNext(out var uid, out var companion))
        {
            RefreshTracking(uid, companion, true);

            if (_ui.IsUiOpen(uid, MailCompanionUiKey.Key))
                UpdateUi(uid, companion);
        }
    }

    private void OnAfterInteract(EntityUid uid, MailCompanionComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<DeliveryComponent>(target, out var delivery))
            return;

        args.Handled = true;
        ScanDelivery(uid, component, args.User, (target, delivery));
    }

    private void OnUiOpened(EntityUid uid, MailCompanionComponent component, BoundUIOpenedEvent args)
    {
        RefreshTracking(uid, component);
        UpdateUi(uid, component);
    }

    private void OnExamined(EntityUid uid, MailCompanionComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (component.RecipientName == null)
        {
            args.PushMarkup(Loc.GetString("mail-companion-examine-empty"));
            return;
        }

        args.PushMarkup(Loc.GetString("mail-companion-examine-recipient", ("recipient", component.RecipientName)));
    }

    private void OnDeliveryOpened(EntityUid uid, DeliveryComponent component, ref DeliveryOpenedEvent args)
    {
        var query = EntityQueryEnumerator<MailCompanionComponent>();
        while (query.MoveNext(out var companionUid, out var companion))
        {
            if (companion.TrackedDelivery != uid)
                continue;

            ResetTracker(companionUid, companion, MailCompanionStatus.DeliveryOpened, popupUser: companion.LastUser);
        }
    }

    private void ScanDelivery(EntityUid uid, MailCompanionComponent component, EntityUid user, Entity<DeliveryComponent> delivery)
    {
        if (component.CooldownEndsAt is { } cooldownEndsAt && cooldownEndsAt > _timing.CurTime)
        {
            component.LastUser = user;
            component.RecipientName = null;
            component.TrackedDelivery = null;
            component.TrackedTarget = null;
            component.TrackedSensor = null;
            component.ExpiresAt = null;
            SetStatus(uid, component, MailCompanionStatus.Cooldown);
            _popup.PopupClient(Loc.GetString("mail-companion-popup-cooldown"), uid, user);
            UpdateUi(uid, component);
            return;
        }

        component.LastUser = user;
        component.RecipientName = delivery.Comp.RecipientName;
        component.TrackedDelivery = delivery.Owner;
        component.TrackedTarget = null;
        component.TrackedSensor = null;
        component.ExpiresAt = null;
        component.CooldownEndsAt = null;

        if (delivery.Comp.IsOpened)
        {
            SetStatus(uid, component, MailCompanionStatus.DeliveryAlreadyOpened);
            _popup.PopupClient(Loc.GetString("mail-companion-popup-delivery-opened-already"), uid, user);
            UpdateUi(uid, component);
            return;
        }

        var result = TryResolveDeliveryTarget(delivery, out var target, out var sensor, out var status);
        if (!result)
        {
            SetStatus(uid, component, status);
            _popup.PopupClient(GetPopupForStatus(status), uid, user);
            UpdateUi(uid, component);
            return;
        }

        component.TrackedTarget = target;
        component.TrackedSensor = sensor;
        component.ExpiresAt = _timing.CurTime + component.TrackingDuration;
        SetStatus(uid, component, MailCompanionStatus.Tracking);
        _popup.PopupClient(Loc.GetString("mail-companion-popup-tracking-started", ("recipient", component.RecipientName!)), uid, user);
        UpdateUi(uid, component);
    }

    private bool TryResolveDeliveryTarget(
        Entity<DeliveryComponent> delivery,
        out EntityUid? target,
        out EntityUid? sensorUid,
        out MailCompanionStatus status)
    {
        target = null;
        sensorUid = null;
        status = MailCompanionStatus.RecipientUnavailable;

        if (string.IsNullOrWhiteSpace(delivery.Comp.RecipientName))
            return false;

        var fallback = MailCompanionStatus.RecipientUnavailable;
        var foundMatchingRecipient = false;

        var sensors = EntityQueryEnumerator<SuitSensorComponent, TransformComponent>();
        while (sensors.MoveNext(out var uid, out var sensor, out var xform))
        {
            if (sensor.User is not { } wearer)
                continue;

            if (delivery.Comp.RecipientStation != null && sensor.StationId != delivery.Comp.RecipientStation)
                continue;

            if (!string.Equals(GetWearerName(wearer), delivery.Comp.RecipientName, StringComparison.Ordinal))
                continue;

            foundMatchingRecipient = true;

            if (sensor.Mode == SuitSensorMode.SensorOff)
            {
                fallback = MailCompanionStatus.SensorsOff;
                continue;
            }

            if (sensor.Mode != SuitSensorMode.SensorCords)
            {
                fallback = MailCompanionStatus.TrackingUnavailable;
                continue;
            }

            var state = _suitSensors.GetSensorState((uid, sensor, xform));
            if (state?.Coordinates == null)
            {
                fallback = MailCompanionStatus.TrackingUnavailable;
                continue;
            }

            target = wearer;
            sensorUid = uid;
            status = MailCompanionStatus.Tracking;
            return true;
        }

        status = foundMatchingRecipient ? fallback : MailCompanionStatus.RecipientUnavailable;
        return false;
    }

    private void RefreshTracking(EntityUid uid, MailCompanionComponent component, bool showPopup = false)
    {
        if (component.ExpiresAt is { } expiresAt && expiresAt <= _timing.CurTime)
        {
            StartCooldown(uid, component, popupUser: showPopup ? component.LastUser : null);
            return;
        }

        if (component.CooldownEndsAt is { } cooldownEndsAt)
        {
            if (cooldownEndsAt <= _timing.CurTime)
            {
                component.CooldownEndsAt = null;
                ResetTracker(uid, component, MailCompanionStatus.Idle);
                return;
            }

            if (component.Status == MailCompanionStatus.Cooldown)
                return;
        }

        if (component.TrackedSensor is not { } sensorUid || !_sensorQuery.TryGetComponent(sensorUid, out var sensor))
            return;

        if (sensor.User is null)
        {
            SetStatus(uid, component, MailCompanionStatus.RecipientUnavailable);
            return;
        }

        if (sensor.Mode == SuitSensorMode.SensorOff)
        {
            SetStatus(uid, component, MailCompanionStatus.SensorsOff);
            return;
        }

        if (sensor.Mode != SuitSensorMode.SensorCords)
        {
            SetStatus(uid, component, MailCompanionStatus.TrackingUnavailable);
            return;
        }

        SetStatus(uid, component, MailCompanionStatus.Tracking);
    }

    private void ResetTracker(
        EntityUid uid,
        MailCompanionComponent component,
        MailCompanionStatus status,
        EntityUid? popupUser = null)
    {
        component.TrackedDelivery = null;
        component.TrackedTarget = null;
        component.TrackedSensor = null;
        component.ExpiresAt = null;
        component.CooldownEndsAt = null;
        component.RecipientName = null;
        SetStatus(uid, component, status);

        if (popupUser != null && EntityManager.EntityExists(popupUser.Value))
            _popup.PopupClient(GetPopupForStatus(status), uid, popupUser.Value);

        UpdateUi(uid, component);
    }

    private void SetStatus(EntityUid uid, MailCompanionComponent component, MailCompanionStatus status)
    {
        component.Status = status;
        var visualState = status == MailCompanionStatus.Tracking
            ? MailCompanionVisualState.Active
            : MailCompanionVisualState.Idle;
        _appearance.SetData(uid, MailCompanionVisuals.State, visualState);
    }

    private void UpdateUi(EntityUid uid, MailCompanionComponent component)
    {
        var remaining = TimeSpan.Zero;
        SuitSensorStatus? trackedSensor = null;

        if (component.ExpiresAt is { } expiresAt)
            remaining = expiresAt > _timing.CurTime ? expiresAt - _timing.CurTime : TimeSpan.Zero;
        else if (component.Status == MailCompanionStatus.Cooldown && component.CooldownEndsAt is { } cooldownEndsAt)
            remaining = cooldownEndsAt > _timing.CurTime ? cooldownEndsAt - _timing.CurTime : TimeSpan.Zero;

        if (component.Status == MailCompanionStatus.Tracking &&
            component.TrackedSensor is { } sensorUid &&
            _sensorQuery.TryGetComponent(sensorUid, out var sensor) &&
            _transformQuery.TryGetComponent(sensorUid, out var xform))
        {
            trackedSensor = _suitSensors.GetSensorState((sensorUid, sensor, xform));
        }

        _ui.SetUiState(uid,
            MailCompanionUiKey.Key,
            new MailCompanionState(_timing.CurTime, component.RecipientName, component.Status, remaining, trackedSensor));
    }

    private void StartCooldown(EntityUid uid, MailCompanionComponent component, EntityUid? popupUser = null)
    {
        component.TrackedTarget = null;
        component.TrackedSensor = null;
        component.ExpiresAt = null;
        component.RecipientName = null;
        component.CooldownEndsAt = _timing.CurTime + component.CooldownDuration;
        SetStatus(uid, component, MailCompanionStatus.Cooldown);

        if (popupUser != null && EntityManager.EntityExists(popupUser.Value))
            _popup.PopupClient(Loc.GetString("mail-companion-popup-cooldown"), uid, popupUser.Value);

        UpdateUi(uid, component);
    }

    private string GetWearerName(EntityUid uid)
    {
        if (_idCard.TryFindIdCard(uid, out var card) && card.Comp.FullName != null)
            return card.Comp.FullName;

        return Identity.Name(uid, EntityManager);
    }

    private string GetPopupForStatus(MailCompanionStatus status)
    {
        return status switch
        {
            MailCompanionStatus.SensorsOff => Loc.GetString("mail-companion-popup-sensors-off"),
            MailCompanionStatus.TrackingUnavailable => Loc.GetString("mail-companion-popup-tracking-disabled"),
            MailCompanionStatus.RecipientUnavailable => Loc.GetString("mail-companion-popup-recipient-unavailable"),
            MailCompanionStatus.Cooldown => Loc.GetString("mail-companion-popup-cooldown"),
            MailCompanionStatus.DeliveryOpened => Loc.GetString("mail-companion-popup-delivery-confirmed"),
            MailCompanionStatus.DeliveryAlreadyOpened => Loc.GetString("mail-companion-popup-delivery-opened-already"),
            MailCompanionStatus.Expired => Loc.GetString("mail-companion-popup-expired"),
            _ => Loc.GetString("mail-companion-popup-recipient-unavailable"),
        };
    }
}
