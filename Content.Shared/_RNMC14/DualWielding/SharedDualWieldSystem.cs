using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._RNMC14.DualWielding;
// NOTE: ALL OF THIS IS EVIL AND I HAVE LITTLE TO NO IDEA WHAT I AM DOING
public sealed class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public bool TryGetDualWieldGuns(
        EntityUid user,
        out EntityUid mainGunUid,
        out GunComponent mainGun,
        out EntityUid rightGunUid,
        out GunComponent rightGun)
    {
        mainGunUid = default;
        mainGun = default!;
        rightGunUid = default;
        rightGun = default!;

        if (!HasComp<DualWieldSpecComponent>(user))
            return false;

        List<(EntityUid held, GunComponent gunComp)> multishotGuns = [];
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryComp<MultishotComponent>(held, out _) && TryComp<GunComponent>(held, out var gunComp) && gunComp != null)
            {
                multishotGuns.Add((held, gunComp));
            }
        }

        if (multishotGuns.Count < 2)
            return false;

        var activeItem = _hands.GetActiveItem(user);
        EntityUid? mainUid = null;
        GunComponent? mainComp = null;
        EntityUid? rightUid = null;
        GunComponent? rightComp = null;

        foreach (var (ent, comp) in multishotGuns)
        {
            if (ent == activeItem)
            {
                mainUid = ent;
                mainComp = comp;
                break;
            }
        }

        if (mainUid == null)
        {
            mainUid = multishotGuns[0].held;
            mainComp = multishotGuns[0].gunComp;
        }

        foreach (var (ent, comp) in multishotGuns)
        {
            if (ent != mainUid)
            {
                rightUid = ent;
                rightComp = comp;
                break;
            }
        }

        if (mainUid != null && rightUid != null && mainComp != null && rightComp != null)
        {
            mainGunUid = mainUid.Value;
            mainGun = mainComp;
            rightGunUid = rightUid.Value;
            rightGun = rightComp;
            return true;
        }

        return false;
    }
}
