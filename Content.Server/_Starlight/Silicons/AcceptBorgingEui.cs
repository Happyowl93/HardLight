using Content.Server.EUI;
using Content.Server.Silicons.Borgs;
using Content.Shared._Starlight.Silicons;
using Content.Shared.Eui;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs;

namespace Content.Server._Starlight.Silicons;
{
    public sealed class AcceptBorgingEui : BaseEui
    {
        private readonly EntityUid _mindId;
        private readonly MindComponent _mind;
        private readonly SharedBorgSystem _borgingSystem;

        public AcceptBorgingEui(EntityUid mindId, MindComponent mind, SharedBorgSystem borgingSys)
        {
            _mindId = mindId;
            _mind = mind;
            _borgingSystem = borgingSys;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptBorgingChoiceMessage choice ||
                choice.Button == AcceptBorgingUiButton.Deny)
            {
                Close();
                return;
            }

            _borgingSystem.TransferMindToClone(_mindId, _mind);
            Close();
        }
    }
}
