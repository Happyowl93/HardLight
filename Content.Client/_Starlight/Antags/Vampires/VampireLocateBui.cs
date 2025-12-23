using System.Linq;
using System.Numerics;
using Content.Shared._Starlight.Antags.Vampires;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Starlight.Antags.Vampires;

[UsedImplicitly]
public sealed class VampireLocateBui : BoundUserInterface
{
    private VampireLocateWindow? _window;

    public VampireLocateBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VampireLocateWindow>();
        _window.TargetSelected += OnTargetSelected;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null)
        {
            _window.TargetSelected -= OnTargetSelected;
        }

        base.Dispose(disposing);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not VampireLocateBuiState msg)
            return;

        _window?.SetTargets(msg.Targets);
    }

    private void OnTargetSelected(VampireLocateTarget target)
    {
        SendMessage(new VampireLocateSelectedBuiMsg { Target = target.Target });
        Close();
    }
}
// im xaml intolerant sowy
public sealed class VampireLocateWindow : DefaultWindow
{
    public event Action<VampireLocateTarget>? TargetSelected;

    private readonly BoxContainer _list;
    private string _filter = string.Empty;
    private List<VampireLocateTarget> _targets = new();

    public VampireLocateWindow()
    {
        Title = Loc.GetString("action-vampire-predator-sense-name");
        MinSize = new Vector2(340f, 420f);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 4,
        };

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 2,
        };

        var search = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("vampire-locate-search-placeholder"),
        };

        search.OnTextChanged += args =>
        {
            _filter = args.Text ?? string.Empty;
            Populate();
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        scroll.AddChild(_list);
        root.AddChild(search);
        root.AddChild(scroll);
        Contents.AddChild(root);

        Populate();
    }

    public void SetTargets(IEnumerable<VampireLocateTarget> targets)
    {
        _targets = targets.ToList();
        Populate();
    }

    private void Populate()
    {
        _list.DisposeAllChildren();

        var filter = _filter.Trim();
        var view = string.IsNullOrWhiteSpace(filter)
            ? _targets
            : _targets.Where(t => t.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase));

        var any = false;
        foreach (var target in view)
        {
            any = true;

            var captured = target;
            var button = new Button
            {
                Text = captured.DisplayName,
                HorizontalAlignment = HAlignment.Stretch,
                ClipText = true,
            };

            button.OnPressed += _ => TargetSelected?.Invoke(captured);
            _list.AddChild(button);
        }

        if (!any)
        {
            _list.AddChild(new Label
            {
                Text = Loc.GetString("vampire-locate-no-targets"),
                HorizontalAlignment = HAlignment.Center,
            });
            return;
        }
    }
}
