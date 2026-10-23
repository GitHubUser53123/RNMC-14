using Content.Shared._RNMC14.DualWielding;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Client.Weapons.Ranged.Systems;
using Content.Client._RMC14.Weapons.Ranged.Prediction;
using Content.Client._RMC14.ItemPickup;
using Content.Client._RMC14.Movement;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Client.GameObjects;
using Content.Client.Gameplay;
using Content.Shared.CombatMode;
using Content.Shared.Item;
using Robust.Shared.Timing;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using System.Linq;
using System.Numerics;

namespace Content.Client._RNMC14.DualWielding;
// NOTE: ALL OF THIS IS EVIL AND I HAVE LITTLE TO NO IDEA WHAT I AM DOING
public sealed class ClientDualWieldSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedDualWieldSystem _dualWield = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly GunPredictionSystem _gunPrediction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemPickupSystem _itemPickup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly RMCLagCompensationSystem _rmcLagCompensation = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        if (!TryComp<CombatModeComponent>(user, out var combat) || !combat.IsInCombatMode)
            return;

        if (!_dualWield.TryGetDualWieldGuns(user, out var mainGunUid, out var mainGun, out var rightGunUid, out var rightGun))
            return;

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) != BoundKeyState.Down)
        {
            if (rightGun.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(rightGunUid) });
            return;
        }

        if (rightGun.NextFire > _timing.CurTime)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
        {
            if (rightGun.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(rightGunUid) });
            return;
        }

        var coordinateEntity = HasComp<GunUseGunOriginComponent>(rightGunUid) ? rightGunUid : user;
        var coordinates = _transformSystem.ToCoordinates(coordinateEntity, mousePos);

        NetEntity? target = null;
        if (_state.CurrentState is GameplayStateBase screen)
            target = GetNetEntity(screen.GetClickedEntity(mousePos));

        if (_player.LocalSession is not { } session)
            return;

        if (_itemPickup.RecentItemPickUp)
            return;

        var rearmSemiAuto =
            _cfg.GetCVar(CCVars.ControlHoldToAttackRanged) &&
            rightGun.SelectedMode == SelectiveFire.SemiAuto &&
            !HasComp<GunClickToFireComponent>(rightGunUid) &&
            (rightGun.AvailableModes & SelectiveFire.FullAuto) == 0;

        var projectiles = _gunPrediction.ShootRequested(GetNetEntity(rightGunUid), GetNetCoordinates(coordinates), target, null, session, rearmSemiAuto);

        RaisePredictiveEvent(new RequestShootEvent()
        {
            Target = target,
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(rightGunUid),
            RearmSemiAuto = rearmSemiAuto,
            Shot = projectiles?.Select(e => e.Id).ToList(),
            LastRealTick = _rmcLagCompensation.GetLastRealTick(null),
        });
    }
}
