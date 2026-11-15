using Robust.Shared.GameStates;

namespace Content.Shared._RNMC14.TacticalReload;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TacticalReloadComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CartridgeReloadTime = 0.5f;

    [DataField, AutoNetworkedField]
    public float MagazineReloadTime = 2.0f;

    [DataField, AutoNetworkedField]
    public float MagazineEjectTime = 1.0f;

    [DataField, AutoNetworkedField]
    public bool BreakOnMove = false;
}
