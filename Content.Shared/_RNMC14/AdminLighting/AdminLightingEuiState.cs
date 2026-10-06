using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RNMC14.AdminLighting
{
    [Serializable, NetSerializable]
    public sealed class AdminLightingEuiState : EuiStateBase
    {
        public string[] HexValues { get; }
        public bool ShouldFade { get; }
        public float FadeDuration { get; }

        public AdminLightingEuiState(string[] hexValues, bool shouldFade, float fadeDuration)
        {
            HexValues = hexValues;
            ShouldFade = shouldFade;
            FadeDuration = fadeDuration;
        }
    }

    public static class AdminLightingEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class SetMapLightStep : EuiMessageBase
        {
            public int StepIndex { get; }
            public string HexValue { get; }
            public bool ShouldFade { get; }
            public float FadeDuration { get; }

            public SetMapLightStep(int stepIndex, string hexValue, bool shouldFade, float fadeDuration)
            {
                StepIndex = stepIndex;
                HexValue = hexValue;
                ShouldFade = shouldFade;
                FadeDuration = fadeDuration;
            }
        }

        [Serializable, NetSerializable]
        public sealed class UpdateUiState : EuiMessageBase
        {
            public int StepIndex { get; }
            public string HexValue { get; }
            public bool ShouldFade { get; }
            public float FadeDuration { get; }

            public UpdateUiState(int stepIndex, string hexValue, bool shouldFade, float fadeDuration)
            {
                StepIndex = stepIndex;
                HexValue = hexValue;
                ShouldFade = shouldFade;
                FadeDuration = fadeDuration;
            }
        }

        [Serializable, NetSerializable]
        public sealed class ResetToDefault : EuiMessageBase { }

        [Serializable, NetSerializable]
        public sealed class CancelActiveFade : EuiMessageBase { }
    }
}
