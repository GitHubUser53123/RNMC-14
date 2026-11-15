using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RNMC14.TacticalReload;

[Serializable, NetSerializable]
public sealed partial class TacticalReloadDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class TacticalReloadEjectDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
