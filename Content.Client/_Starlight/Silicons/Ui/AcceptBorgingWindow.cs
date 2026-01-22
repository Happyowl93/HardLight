using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Starlight.Silicons.UI
{
    public sealed class AcceptBorgingWindow : DefaultWindow
    {
        public readonly Button DenyButton;
        public readonly Button AcceptButton;

        public AcceptBorgingWindow()
        {

            Title = Loc.GetString("accept-borging-window-title");

            ContentsContainer.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children =
                        {
                            new Label()
                            {
                                Text = Loc.GetString("accept-borging-window-prompt-text-part")
                            },
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Align = AlignMode.Center,
                                Children =
                                {
                                    (AcceptButton = new Button
                                    {
                                        Text = Loc.GetString("accept-borging-window-accept-button"),
                                    }),

                                    new Control()
                                    {
                                        MinSize = new Vector2(20, 0)
                                    },

                                    (DenyButton = new Button
                                    {
                                        Text = Loc.GetString("accept-borging-window-deny-button"),
                                    })
                                }
                            },
                        }
                    },
                }
            });
        }
    }
}
