using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared._RNMC14.AdminLighting;
using Content.Shared.Eui;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._RNMC14.AdminLighting
{
    public sealed class LightingTabEui : BaseEui
    {
        private readonly LightningManagerSystem _manager;

        public LightingTabEui()
        {
            _manager = IoCManager.Resolve<IEntityManager>().System<LightningManagerSystem>();
        }

        public override void Opened()
        {
            base.Opened();
            _manager.RegisterOpenEui(this);
            StateDirty();
        }

        public override EuiStateBase GetNewState()
        {
            return new AdminLightingEuiState(_manager.ActiveHexValues, _manager.ShouldFade, _manager.FadeDuration);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case AdminLightingEuiMsg.ResetToDefault:
                    _manager.ResetArrayMemoryOnly();
                    break;

                case AdminLightingEuiMsg.CancelActiveFade:
                    _manager.CancelActiveFadeMemory(Player);
                    break;

                case AdminLightingEuiMsg.UpdateUiState updateMsg:
                    _manager.UpdateArrayMemoryDirect(updateMsg.StepIndex, updateMsg.HexValue, updateMsg.ShouldFade, updateMsg.FadeDuration);
                    break;

                case AdminLightingEuiMsg.SetMapLightStep lightMsg:
                    _manager.ExecuteMapLightShift(Player, lightMsg.StepIndex, lightMsg.HexValue, lightMsg.ShouldFade, lightMsg.FadeDuration);
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();
            _manager.UnregisterOpenEui(this);
        }
    }
}
