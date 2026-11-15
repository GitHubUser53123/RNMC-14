using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
using Content.Shared.Popups;
using Content.Shared._RMC14.Weapons.Ranged;

namespace Content.Shared._RNMC14.TacticalReload;
// NOTE: ALL OF THIS IS EVIL AND SHITCODE. All is how it's supposed to be.
public sealed class TacticalReloadSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TacticalReloadComponent, TacticalReloadDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<TacticalReloadComponent, TacticalReloadEjectDoAfterEvent>(OnEjectDoAfter);
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var gun = args.Used;
        var ammo = args.Target;

        if (!HasComp<GunComponent>(gun))
            return;

        if (!TryComp<TacticalReloadComponent>(gun, out var tactical))
            return;

        if (!_containers.TryGetContainingContainer(ammo, out var container) ||
            !TryComp<TacticalReloadStorageComponent>(container.Owner, out _))
        {
            return;
        }

        // I'm too lazy to make this a bool to change in the comp, deal with it.
        if (TryComp<WieldableComponent>(gun, out var wieldable) && wieldable.Wielded)
        {
            _popup.PopupClient(Loc.GetString("tactical-reload-wielded"), gun, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<BreechLoadedComponent>(gun, out var breech) && !breech.Open)
        {
            _popup.PopupClient(Loc.GetString("rmc-breech-loaded-closed-load-attempt"), args.User, args.User);
            args.Handled = true;
            return;
        }

        var storageEnt = container.Owner;

        if (TryComp<ChamberMagazineAmmoProviderComponent>(gun, out var chamber) && chamber.BoltClosed == true)
            return;

        bool hasExistingMagazine = TryComp<MagazineAmmoProviderComponent>(gun, out _) && _slots.GetItemOrNull(gun, SharedGunSystem.MagazineSlot) != null;

        if (!hasExistingMagazine && IsGunFull(gun))
            return;

        args.Handled = true;

        if (hasExistingMagazine)
        {
            var ejectDelay = TimeSpan.FromSeconds(tactical.MagazineEjectTime);
            var ejectDoAfterArgs = new DoAfterArgs(EntityManager, args.User, ejectDelay, new TacticalReloadEjectDoAfterEvent(), gun, target: storageEnt, used: ammo)
            {
                BreakOnMove = tactical.BreakOnMove,
                BreakOnDamage = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTarget,
            };
            _doAfter.TryStartDoAfter(ejectDoAfterArgs);
        }
        else
        {
            StartInsertDoAfter(gun, ammo, tactical, args.User, storageEnt);
        }
    }

    private void StartInsertDoAfter(EntityUid gun, EntityUid ammo, TacticalReloadComponent tactical, EntityUid user, EntityUid storageEnt)
    {
        var isMagazine = (HasComp<BallisticAmmoProviderComponent>(ammo) || HasComp<ContainerAmmoProviderComponent>(ammo) || HasComp<SpeedLoaderComponent>(ammo)) && !HasComp<CartridgeAmmoComponent>(ammo);
        var delaySeconds = isMagazine ? tactical.MagazineReloadTime : tactical.CartridgeReloadTime;
        var delay = TimeSpan.FromSeconds(delaySeconds);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new TacticalReloadDoAfterEvent(), gun, target: storageEnt, used: ammo)
        {
            BreakOnMove = tactical.BreakOnMove,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private bool IsGunFull(EntityUid gun)
    {
        if (TryComp<MagazineAmmoProviderComponent>(gun, out _) && _slots.TryGetSlot(gun, SharedGunSystem.MagazineSlot, out _))
        {
            return _slots.GetItemOrNull(gun, SharedGunSystem.MagazineSlot) != null;
        }
        if (TryComp<BallisticAmmoProviderComponent>(gun, out var ballistic))
        {
            var count = ballistic.Entities.Count + ballistic.UnspawnedCount;
            if (count >= ballistic.Capacity)
                return true;
        }
        // Revolvers are evil and they use cartridges and magazines. I know, dastardly.
        if (TryComp<RevolverAmmoProviderComponent>(gun, out var revolver))
        {
            var count = 0;
            for (var i = 0; i < revolver.Capacity; i++)
            {
                if (revolver.Chambers[i] != null || revolver.AmmoSlots[i] != null)
                    count++;
            }
            if (count >= revolver.Capacity)
                return true;
        }
        return false;
    }

    private EntityUid? FindCompatibleAmmo(EntityUid gun, EntityUid storageEnt)
    {
        if (!TryComp<StorageComponent>(storageEnt, out var storage))
            return null;

        foreach (var ent in storage.Container.ContainedEntities)
        {
            if (TryComp<MagazineAmmoProviderComponent>(gun, out _) && _slots.TryGetSlot(gun, SharedGunSystem.MagazineSlot, out var slot))
            {
                if (slot.Whitelist == null || _whitelistSystem.IsWhitelistPass(slot.Whitelist, ent))
                    return ent;
            }
            else if (TryComp<BallisticAmmoProviderComponent>(gun, out var ballistic))
            {
                if (ballistic.Whitelist == null || _whitelistSystem.IsWhitelistPass(ballistic.Whitelist, ent))
                    return ent;
            }
            else if (TryComp<RevolverAmmoProviderComponent>(gun, out var revolver))
            {
                if (HasComp<SpeedLoaderComponent>(ent) || revolver.Whitelist == null || _whitelistSystem.IsWhitelistPass(revolver.Whitelist, ent))
                    return ent;
            }
        }
        return null;
    }

    private void OnEjectDoAfter(Entity<TacticalReloadComponent> ent, ref TacticalReloadEjectDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.Args.User;
        var gun = ent;

        if (TryComp<WieldableComponent>(gun, out var wieldable) && wieldable.Wielded)
        {
            _popup.PopupClient(Loc.GetString("tactical-reload-wielded"), gun, user);
            return;
        }
        if (TryComp<BreechLoadedComponent>(gun, out var breech) && !breech.Open)
        {
            _popup.PopupClient(Loc.GetString("rmc-breech-loaded-closed-load-attempt"), user, user);
            return;
        }

        if (_slots.TryGetSlot(gun, SharedGunSystem.MagazineSlot, out _))
        {
            _slots.TryEject(gun, SharedGunSystem.MagazineSlot, null, out _, excludeUserAudio: true);
        }

        args.Handled = true;

        if (args.Args.Used is { } ammo && !Deleted(ammo))
        {
            var storageEnt = args.Args.Target ?? gun;
            StartInsertDoAfter(gun, ammo, ent.Comp, user, storageEnt);
        }
    }

    private void OnDoAfter(Entity<TacticalReloadComponent> ent, ref TacticalReloadDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.Args.User;
        var gun = ent;

        if (TryComp<WieldableComponent>(gun, out var wieldable) && wieldable.Wielded)
        {
            _popup.PopupClient(Loc.GetString("tactical-reload-wielded"), gun, user);
            args.Repeat = false;
            return;
        }

        if (TryComp<BreechLoadedComponent>(gun, out var breech) && !breech.Open)
        {
            _popup.PopupClient(Loc.GetString("rmc-breech-loaded-closed-load-attempt"), user, user);
            args.Repeat = false;
            return;
        }

        if (TryComp<MagazineAmmoProviderComponent>(gun, out _) && _slots.GetItemOrNull(gun, SharedGunSystem.MagazineSlot) != null)
        {
            args.Repeat = false;
            args.Handled = true;
            return;
        }

        EntityUid? ammo = args.Args.Used;
        var storageEnt = args.Args.Target;

        if (ammo == null || Deleted(ammo.Value) || (_containers.TryGetContainingContainer(ammo.Value, out var curContainer) && curContainer.Owner == (EntityUid) gun))
        {
            if (storageEnt != null && TryComp<TacticalReloadStorageComponent>(storageEnt.Value, out _))
            {
                ammo = FindCompatibleAmmo(gun, storageEnt.Value);
            }
        }

        if (ammo == null || Deleted(ammo.Value) || storageEnt == null || !TryComp<TacticalReloadStorageComponent>(storageEnt.Value, out _))
        {
            args.Repeat = false;
            return;
        }

        bool success = false;
        // For in cases of weapons with insert delay, so when the doafter is done it just does the normal insert delay logic, instead of forcefully jamming it in.
        bool hasInsertDelay = false;
        if (TryComp<MagazineAmmoProviderComponent>(gun, out _) && _slots.TryGetSlot(gun, SharedGunSystem.MagazineSlot, out var slot))
        {
            success = _slots.TryInsert(gun, SharedGunSystem.MagazineSlot, ammo.Value, user);
        }
        else if (TryComp<BallisticAmmoProviderComponent>(gun, out var ballistic))
        {
            hasInsertDelay = ballistic.InsertDelay > 0;
            success = _gunSystem.TryAmmoInsert(gun, ballistic, ammo.Value, user, gun, ballistic.InsertDelay);
        }
        else if (TryComp<RevolverAmmoProviderComponent>(gun, out var revolver))
        {
            success = _gunSystem.TryRevolverInsert(gun, revolver, ammo.Value, user);
        }

        args.Handled = true;
        args.Repeat = success && !hasInsertDelay && !IsGunFull(gun);
    }
}
