using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.SurveillanceCamera;
using Content.Shared.Emp;

namespace Content.Server.Emp;

public sealed class EmpSystem : SharedEmpSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpImmuneComponent, EmpAttemptEvent>(OnEmpAttempt); //SL edit

        SubscribeLocalEvent<EmpDisabledComponent, RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, CustomRadioSendAttemptEvent>(OnCustomRadioSendAttempt); //Starlight
        SubscribeLocalEvent<EmpDisabledComponent, CustomRadioReceiveAttemptEvent>(OnCustomRadioReceiveAttempt); //Starlight
        SubscribeLocalEvent<EmpDisabledComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker);
        SubscribeLocalEvent<EmpDisabledComponent, SurveillanceCameraSetActiveAttemptEvent>(OnCameraSetActive);
    }

    private void OnRadioSendAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnRadioReceiveAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioReceiveAttemptEvent args) => args.Cancelled = true;

    //Starlight begin
    private void OnCustomRadioSendAttempt(EntityUid uid, EmpDisabledComponent component,
        ref CustomRadioSendAttemptEvent args) =>
        args.Cancelled = true;

    private void OnCustomRadioReceiveAttempt(EntityUid uid, EmpDisabledComponent component,
        ref CustomRadioReceiveAttemptEvent args) =>
        args.Cancelled = true;
    //Starlight end
    
    private void OnApcToggleMainBreaker(EntityUid uid, EmpDisabledComponent component, ref ApcToggleMainBreakerAttemptEvent args) => args.Cancelled = true;

    private void OnCameraSetActive(EntityUid uid, EmpDisabledComponent component, ref SurveillanceCameraSetActiveAttemptEvent args) => args.Cancelled = true;
    
    private void OnEmpAttempt(EntityUid uid, EmpImmuneComponent comp, EmpAttemptEvent args) => args.Cancelled = true;
}
