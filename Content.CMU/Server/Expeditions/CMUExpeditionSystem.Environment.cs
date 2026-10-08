using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.Atmos;
using Content.Shared.CMU14.Expeditions;
using Content.Shared.Gravity;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.CMU14.Expeditions;

public sealed partial class CMUExpeditionSystem
{
    [Dependency] private ChatSystem _chat = default!;

    private void AnnounceExpedition(EntityUid map, CMUExpeditionMapComponent expedition)
    {
        _chat.DispatchGlobalAnnouncement(Loc.GetString("cmu-expedition-announcement",
                ("sector", Name(map)), ("lz", Name(expedition.LandingBeacon!.Value))),
            Loc.GetString("cmu-expedition-announcement-sender"), playSound: true,
            announcementSound: new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg"));
    }
}