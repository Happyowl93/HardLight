using Content.Server.Hands.Systems;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Player;
using Content.Server.Mind;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Administration.Managers;

namespace Content.Shared.Starlight.Economy.Atm;
public sealed partial class ATMSystem : SharedATMSystem
{
    [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private static readonly EntProtoId<StackComponent> _cash = "NTCredit";
    private readonly object _transferLock = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<ATMComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<NTCashComponent, AfterInteractEvent>(OnAfterInteract);
        Subs.BuiEvents<ATMComponent>(ATMUIKey.Key, subs =>
        {
            subs.Event<ATMWithdrawBuiMsg>(OnWithdraw);
            subs.Event<ATMTransferBuiMsg>(OnTransfer);
        });
        base.Initialize();
    }

    private void OnWithdraw(EntityUid uid, ATMComponent component, ATMWithdrawBuiMsg args)
    {
        if (_playerRolesManager.GetBalance(args.Actor) is not { } balance || balance < args.Amount || args.Amount <= 0)
            return;

        var newBalance = balance -= args.Amount;

        _playerRolesManager.SetBalance(args.Actor, newBalance);
        var cash = SpawnAtPosition(_cash, Transform(uid).Coordinates);
        var stack = EnsureComp<StackComponent>(cash);
        _stack.SetCount((cash, stack), args.Amount);
        _hands.TryPickup(args.Actor, cash);
        _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState() { Balance = newBalance });
        _audioSystem.PlayPvs(component.WithdrawSound, uid);
    }

    private void OnAfterInteract(Entity<NTCashComponent> ent, ref AfterInteractEvent args)
    {
        if (TryComp<StackComponent>(ent.Owner, out var stack)
            && args.Target.HasValue
            && TryComp<ATMComponent>(args.Target, out var atm) 
            && _playerRolesManager.GetBalance(args.User) is { } balance)
        {
            args.Handled = true; // If we don't do this - debug assert and crash at the dev build.
            var newBalance = balance += (int)Math.Floor(stack.Count * 0.9);
            _playerRolesManager.SetBalance(args.User, newBalance);
            QueueDel(ent);
            _uiSystem.SetUiState(args.Target.Value, ATMUIKey.Key, new ATMBuiState() { Balance = newBalance });
            _audioSystem.PlayPvs(atm.DepositSound, args.Target.Value);
        }
    }

    private void OnTransfer(EntityUid uid, ATMComponent component, ATMTransferBuiMsg args)
    {
        if (_playerRolesManager.GetBalance(args.Actor) is not { } balance)
            return;

        if (string.IsNullOrWhiteSpace(args.Recipient))
        {
            _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState
            {
                Balance = balance,
                Message = Loc.GetString("economy-atm-transfer-error-no-recipient"),
                IsError = true
            });
            return;
        }

        if (!_players.TryGetSessionByEntity(args.Actor, out var senderSession))
        {
            _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState
            {
                Balance = balance,
                Message = Loc.GetString("economy-atm-transfer-error-generic"),
                IsError = true
            });
            return;
        }

        var matches = new List<ICommonSession>();
        
        foreach (var reg in _playerRolesManager.Players)
        {
            if (_mind.TryGetMind(reg.Session.UserId, out _, out var mind)
                && !string.IsNullOrWhiteSpace(mind.CharacterName)
                && string.Equals(mind.CharacterName, args.Recipient, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(reg.Session);
            }
        }

        if (matches.Count != 1)
        {
            var key = matches.Count == 0
                ? "economy-atm-transfer-error-no-recipient"
                : "economy-atm-transfer-error-ambiguous";

            _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState
            {
                Balance = balance,
                Message = Loc.GetString(key),
                IsError = true
            });
            return;
        }

        var recipientSession = matches[0];

        if (recipientSession.UserId == senderSession.UserId)
        {
            _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState
            {
                Balance = balance,
                Message = Loc.GetString("economy-atm-transfer-error-self"),
                IsError = true
            });
            return;
        }

        lock (_transferLock)
        {
            if (_playerRolesManager.GetBalance(recipientSession) is not { } recipientBalance)
            {
                _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState
                {
                    Balance = balance,
                    Message = Loc.GetString("economy-atm-transfer-error-no-recipient"),
                    IsError = true
                });
                return;
            }

            var newBalance = balance -= args.Amount;

            _playerRolesManager.SetBalance(recipientSession, recipientBalance += args.Amount);

            var recipientName = _mind.TryGetMind(recipientSession.UserId, out _, out var rMind)
                ? rMind.CharacterName ?? recipientSession.Name
                : recipientSession.Name;

            _uiSystem.SetUiState(uid, ATMUIKey.Key, new ATMBuiState
            {
                Balance = newBalance,
                Message = Loc.GetString("economy-atm-transfer-success", ("amount", args.Amount), ("recipient", recipientName)),
                IsError = false
            });

            _adminLogger.Add(
                LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} transferred {args.Amount} cr. to {recipientName} via {ToPrettyString(uid):entity}");
        }
    }

    private void OnBeforeActivatableUIOpen(Entity<ATMComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        var balance = _playerRolesManager.GetBalance(args.User);

        _uiSystem.SetUiState(ent.Owner, ATMUIKey.Key, new ATMBuiState() { Balance = balance ?? 0 });
    }
}
