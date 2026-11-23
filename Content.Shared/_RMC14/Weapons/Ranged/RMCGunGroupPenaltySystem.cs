using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
// RNMC Start
using Content.Shared._RNMC14.DualWielding;
// RNMC End

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class RMCGunGroupPenaltySystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly CMGunSystem _rmcGun = default!;
    // RNMC Start
    [Dependency] private readonly SharedDualWieldSystem _dualWield = default!;
    // RNMC End

    private EntityQuery<GunGroupPenaltyComponent> _gunGroupPenalty;
    private EntityQuery<ProjectileComponent> _projectileQuery;

    public override void Initialize()
    {
        _gunGroupPenalty = GetEntityQuery<GunGroupPenaltyComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<GunGroupPenaltyComponent, GotEquippedHandEvent>(OnGroupSpreadPenaltyEquippedHand);
        SubscribeLocalEvent<GunGroupPenaltyComponent, GotUnequippedHandEvent>(OnGroupSpreadPenaltyUnequippedHand);
        SubscribeLocalEvent<GunGroupPenaltyComponent, EntParentChangedMessage>(OnGroupSpreadPenaltyParentChanged);
        SubscribeLocalEvent<GunGroupPenaltyComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<GunGroupPenaltyComponent, GunRefreshModifiersEvent>(OnGroupSpreadPenaltyRefreshModifiers);
        SubscribeLocalEvent<GunGroupPenaltyComponent, AmmoShotEvent>(OnGroupSpreadPenaltyAmmoShot, before: [typeof(CMGunSystem)]);
    }

    private void OnGroupSpreadPenaltyEquippedHand(Entity<GunGroupPenaltyComponent> ent, ref GotEquippedHandEvent args)
    {
        RefreshGunHolderModifiers(ent);
    }

    private void OnGroupSpreadPenaltyUnequippedHand(Entity<GunGroupPenaltyComponent> ent, ref GotUnequippedHandEvent args)
    {
        RefreshGunHolderModifiers(ent);
    }

    private void OnGroupSpreadPenaltyParentChanged(Entity<GunGroupPenaltyComponent> ent, ref EntParentChangedMessage args)
    {
        if (args.OldParent == null)
            return;

        foreach (var held in _hands.EnumerateHeld(args.OldParent.Value))
        {
            _gun.RefreshModifiers(held);
        }
    }

    private void OnInsertedIntoContainer(Entity<GunGroupPenaltyComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        foreach (var held in _hands.EnumerateHeld(args.Container.Owner))
        {
            _gun.RefreshModifiers(held);
        }
    }

    private void OnGroupSpreadPenaltyRefreshModifiers(Entity<GunGroupPenaltyComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!_rmcGun.TryGetGunUser(ent, out var user))
            return;

        // RNMC Start
        EntityUid mainGunUid, rightGunUid;
        GunComponent mainGun, rightGun;
        if (_dualWield.TryGetDualWieldGuns(user, out mainGunUid, out mainGun, out rightGunUid, out rightGun) && HasComp<MultishotComponent>(ent.Owner))
            return;
        // RNMC End

        foreach (var held in _hands.EnumerateHeld((user, user)))
        {
            if (held == ent.Owner ||
                !_gunGroupPenalty.HasComp(held))
            {
                continue;
            }

            args.CameraRecoilScalar += ent.Comp.Recoil;
            args.AngleIncrease += ent.Comp.AngleIncrease;
            args.MinAngle += ent.Comp.AngleIncrease / 2;
            args.MaxAngle += ent.Comp.AngleIncrease;
            break;
        }
    }

    private void OnGroupSpreadPenaltyAmmoShot(Entity<GunGroupPenaltyComponent> ent, ref AmmoShotEvent args)
    {
        if (!_rmcGun.TryGetGunUser(ent, out var user))
            return;

        // RNMC Start (This specific block is evil and bites if you glance at it wrong, I wish I knew why, as it's honestly a pretty simple block.)
        EntityUid mainGunUid, rightGunUid;
        GunComponent mainGun, rightGun;
        if (_dualWield.TryGetDualWieldGuns(user, out mainGunUid, out mainGun, out rightGunUid, out rightGun) && HasComp<MultishotComponent>(ent.Owner))
            return;
        // RNMC End

        var other = false;
        foreach (var held in _hands.EnumerateHeld((user, user)))
        {
            if (held != ent.Owner &&
                _gunGroupPenalty.HasComp(held))
            {
                other = true;
                break;
            }
        }

        if (!other)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_projectileQuery.TryComp(projectile, out var projectileComp))
                continue;

            projectileComp.Damage *= ent.Comp.DamageMultiplier;
        }
    }


    private void RefreshGunHolderModifiers(Entity<GunGroupPenaltyComponent> gun)
    {
        _gun.RefreshModifiers(gun.Owner);
        if (!_rmcGun.TryGetGunUser(gun, out var user))
            return;

        foreach (var held in _hands.EnumerateHeld((user, user)))
        {
            if (held != gun.Owner)
                _gun.RefreshModifiers(held);
        }
    }
}
