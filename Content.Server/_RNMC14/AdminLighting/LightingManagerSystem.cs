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
        private readonly Dictionary<EntityUid, FadeState> _activeFades = new();

        private struct FadeState
        {
            public Color StartColor;
            public Color TargetColor;
            public float Duration;
            public float Progress;
        }

        public void RegisterOpenEui(LightingTabEui eui) => _openEuis.Add(eui);
        public void UnregisterOpenEui(LightingTabEui eui) => _openEuis.Remove(eui);
        private void SyncAllOpenPanels() => _openEuis.ForEach(eui => eui.StateDirty());

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_activeFades.Count == 0)
                return;

            var toRemove = new List<EntityUid>();
            var uids = new List<EntityUid>(_activeFades.Keys);

            foreach (var uid in uids)
            {
                if (!_entManager.TryGetComponent<MapLightComponent>(uid, out var mapLight))
                {
                    toRemove.Add(uid);
                    continue;
                }

                var fade = _activeFades[uid];
                fade.Progress += frameTime / fade.Duration;

                if (fade.Progress >= 1f)
                {
                    mapLight.AmbientLightColor = fade.TargetColor;
                    toRemove.Add(uid);
                }
                else
                {
                    mapLight.AmbientLightColor = Color.InterpolateBetween(fade.StartColor, fade.TargetColor, fade.Progress);
                }

                _entManager.Dirty(uid, mapLight);
                _activeFades[uid] = fade;
            }

            foreach (var uid in toRemove)
            {
                _activeFades.Remove(uid);
            }
        }

        public void ResetArrayMemoryOnly()
        {
            for (int i = 0; i < DefaultHexValues.Length; i++)
            {
                ActiveHexValues[i] = DefaultHexValues[i];
            }
            ShouldFade = true;
            FadeDuration = 5.0f;
            SyncAllOpenPanels();
        }

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

            if (!mapUid.IsValid() || !_activeFades.ContainsKey(mapUid))
                return;

            _activeFades.Remove(mapUid);

            if (_entManager.TryGetComponent<MapLightComponent>(mapUid, out var mapLight))
            {
                _entManager.Dirty(mapUid, mapLight);
                _chat.SendAdminAlert($"{player.Name.ToString()} cancelled the active lighting fade on map {adminXform.MapID.ToString()}. Frozen at {mapLight.AmbientLightColor.ToHex()}");
            }
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
                _activeFades.Remove(mapUid);
                mapLight.AmbientLightColor = targetColor;
                _entManager.Dirty(mapUid, mapLight);
                _chat.SendAdminAlert($"{player.Name.ToString()} snapped map {adminMapId.ToString()} light to {hex.ToString()}");
                return;
            }

            var newFade = new FadeState
            {
                StartColor = mapLight.AmbientLightColor,
                TargetColor = targetColor,
                Duration = duration,
                Progress = 0f
            };

            _activeFades[mapUid] = newFade;
            _chat.SendAdminAlert($"{player.Name.ToString()} initiated a smooth {duration.ToString()}s self-contained fade on map {adminMapId.ToString()} to {hex.ToString()}");
        }
    }
}
