using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Silicons.Borgs;
{
    [Serializable, NetSerializable]
    public enum AcceptBorgingUiButton
    {
        Deny,
        Accept,
    }

    [Serializable, NetSerializable]
    public sealed class AcceptBorgingChoiceMessage : EuiMessageBase
    {
        public readonly AcceptBorgingUiButton Button;

        public AcceptBorgingChoiceMessage(AcceptBorgingUiButton button)
        {
            Button = button;
        }
    }
}
