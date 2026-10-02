using Content.Server.Chat.Managers;
using Content.Shared._RNMC14.AdminLighting;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._RNMC14.AdminLighting
{
    public sealed class LightningManagerSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        private static readonly string[] DefaultHexValues = new string[]
        {
            "#F6CDB4FF", "#D1B1A6FF", "#AB9698FF", "#887B88FF", "#666177FF",
            "#484764FF", "#2E2F4EFF", "#181936FF", "#101125FF", "#000000FF"
        };

        public bool ShouldFade { get; private set; } = true;
        public float FadeDuration { get; private set; } = 5.0f;
        public string[] ActiveHexValues { get; } = (string[])DefaultHexValues.Clone();

        private readonly List<LightingTabEui> _openEuis = new();

        public void RegisterOpenEui(LightingTabEui eui) => _openEuis.Add(eui);
        public void UnregisterOpenEui(LightingTabEui eui) => _openEuis.Remove(eui);
        private void SyncAllOpenPanels() => _openEuis.ForEach(eui => eui.StateDirty());

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<MapLightComponent>();
            while (query.MoveNext(out var uid, out var mapLight))
            {
                if (mapLight.FadeDuration <= 0f)
                    continue;

                mapLight.FadeProgress += frameTime / mapLight.FadeDuration;

                if (mapLight.FadeProgress >= 1f)
                {
                    mapLight.AmbientLightColor = mapLight.FadeTargetColor;
                    mapLight.FadeDuration = 0f;
                    mapLight.FadeProgress = 0f;
                }
                else
                {
                    mapLight.AmbientLightColor = Color.InterpolateBetween(mapLight.FadeStartColor, mapLight.FadeTargetColor, mapLight.FadeProgress);
                }

                _entManager.Dirty(uid, mapLight);
            }
        }

        public void ResetArrayMemoryOnly()
        {
            for (int i = 0; i < DefaultHexValues.Length; i++)
            {
                ActiveHexValues[i] = DefaultHexValues[i];
            }
            ShouldFade = true;
            KeepFadeDuration(5.0f);
            SyncAllOpenPanels();
        }

        private void KeepFadeDuration(float dur) => FadeDuration = dur;

        public void UpdateArrayMemoryDirect(int index, string hex, bool fade, float duration)
        {
            ShouldFade = fade;
            FadeDuration = duration;
            if (index >= 0 && index < ActiveHexValues.Length)
            {
                ActiveHexValues[index] = hex;
            }
            SyncAllOpenPanels();
        }

        public void CancelActiveFadeMemory(ICommonSession? player)
        {
            if (player?.AttachedEntity is not { } adminEntity)
                return;

            if (!_entManager.TryGetComponent<TransformComponent>(adminEntity, out var adminXform))
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var mapUid = mapManager.GetMapEntityId(adminXform.MapID);

            if (!mapUid.IsValid() || !_entManager.TryGetComponent<MapLightComponent>(mapUid, out var mapLight))
                return;

            if (mapLight.FadeDuration <= 0f)
                return;

            mapLight.FadeDuration = 0f;
            mapLight.FadeProgress = 0f;
            _entManager.Dirty(mapUid, mapLight);
            _chat.SendAdminAlert($"{player.Name.ToString()} cancelled the active lighting fade on map {adminXform.MapID.ToString()}. Frozen at {mapLight.AmbientLightColor.ToHex()}");
        }

        public void ExecuteMapLightShift(ICommonSession? player, int index, string hex, bool fade, float duration)
        {
            UpdateArrayMemoryDirect(index, hex, fade, duration);

            if (player?.AttachedEntity is not { } adminEntity)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!_entManager.TryGetComponent<TransformComponent>(adminEntity, out var adminXform))
                return;

            var adminMapId = adminXform.MapID;
            var mapUid = mapManager.GetMapEntityId(adminMapId);

            if (!mapUid.IsValid())
                return;

            if (!_entManager.HasComponent<MapLightComponent>(mapUid))
            {
                _entManager.AddComponent<MapLightComponent>(mapUid);
            }

            var mapLight = _entManager.GetComponent<MapLightComponent>(mapUid);
            Color targetColor = Color.FromHex(hex);

            if (!fade || duration <= 0f)
            {
                mapLight.AmbientLightColor = targetColor;
                mapLight.FadeDuration = 0f;
                mapLight.FadeProgress = 0f;
                _entManager.Dirty(mapUid, mapLight);
                _chat.SendAdminAlert($"{player.Name.ToString()} snapped map {adminMapId.ToString()} light to {hex.ToString()}");
                return;
            }

            mapLight.FadeStartColor = mapLight.AmbientLightColor;
            mapLight.FadeTargetColor = targetColor;
            mapLight.FadeProgress = 0f;
            mapLight.FadeDuration = duration;

            _entManager.Dirty(mapUid, mapLight);
            _chat.SendAdminAlert($"{player.Name.ToString()} initiated a smooth {duration.ToString()}s delta-tick fade on map {adminMapId.ToString()} to {hex.ToString()}");
        }
    }
}
