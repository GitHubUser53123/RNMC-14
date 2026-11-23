using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.CombatMode;
using Content.Shared.Vehicle;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
// RNMC Start
using Content.Shared._RNMC14.DualWielding;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
// RNMC End

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

public abstract class SharedGunPredictionSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleRideSurfaceSystem _rideSurface = default!;
    // RNMC Start
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    // RNMC End

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCGunPrediction, v => GunPrediction = v, true);
    }

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session, bool rearmSemiAuto = false)
    {
        var user = session.AttachedEntity;

        if (user == null ||
            !_combatMode.IsInCombatMode(user))
        {
            return null;
        }

        var targetGun = GetEntity(netGun);
        GunComponent? gun = null;
        EntityUid ent = default;

        // RNMC Start
        if (TryComp<GunComponent>(targetGun, out var targetGunComp) && HasComp<MultishotComponent>(targetGun))
        {
            foreach (var held in _hands.EnumerateHeld(user.Value))
            {
                if (held == targetGun)
                {
                    ent = targetGun;
                    gun = targetGunComp;
                    break;
                }
            }
        }
        // RNMC End

        if (gun == null)
        {
            if (!_gun.TryGetGun(user.Value, out ent, out gun))
            {
                return null;
            }

            if (ent != targetGun)
                return null;
        // RNMC (technically I added this } below, I know, flay me for not including it in the above "// RNMC Start/End"
        }

        var shootCoordinates = GetCoordinates(coordinates);
        var targetUid = GetEntity(target);
        if (targetUid is { } clickedTarget)
        {
            var mapCoordinates = _transform.ToMapCoordinates(shootCoordinates);
            if (_rideSurface.TryGetRiderAtCoordinates(clickedTarget, mapCoordinates, out var rider))
                targetUid = rider;
        }

#pragma warning disable RA0002
        gun.ShootCoordinates = shootCoordinates;
        gun.Target = targetUid;
#pragma warning restore RA0002
        if (rearmSemiAuto)
            _gun.ResetShotCounter(ent, gun);

        return _gun.AttemptShoot(user.Value, ent, gun, projectiles, session);
    }
}
