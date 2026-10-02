using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._RNMC14.AdminLighting;

namespace Content.Client._RNMC14.RNMCLightingTab
{
    public sealed class RNMCLightingTabEui : BaseEui
    {
        private RNMCLightingWindow? _window;

        public override void Opened()
        {
            base.Opened();

            _window = new RNMCLightingWindow();
            _window.OnClose += () => SendMessage(new CloseEuiMessage());

            _window.OnStepExecuted += (index, hexValue, shouldFade, fadeDuration) =>
            {
                SendMessage(new AdminLightingEuiMsg.SetMapLightStep(index, hexValue, shouldFade, fadeDuration));
            };

            _window.OnValueModified += (index, hexValue, shouldFade, fadeDuration) =>
            {
                SendMessage(new AdminLightingEuiMsg.UpdateUiState(index, hexValue, shouldFade, fadeDuration));
            };

            _window.OnResetRequested += () =>
            {
                SendMessage(new AdminLightingEuiMsg.ResetToDefault());
            };

            _window.OnCancelRequested += () =>
            {
                SendMessage(new AdminLightingEuiMsg.CancelActiveFade());
            };

            _window.OpenCentered();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not AdminLightingEuiState castState)
                return;

            _window?.UpdateState(castState);
        }

        public override void Closed()
        {
            base.Closed();
            _window?.Close();
        }
    }
}
