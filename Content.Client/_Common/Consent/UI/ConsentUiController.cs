// SPDX-FileCopyrightText: Copyright (c) 2024-2025 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Client._Common.Consent.UI.Windows;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Common.Consent.UI;

[UsedImplicitly]
public sealed class ConsentUiController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private ConsentWindow? _window;

    private MenuButton? ConsentButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.ConsentButton;

    public void OnStateEntered(GameplayState state)
    {
        EnsureWindow();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenConsentWindow,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<ConsentUiController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }

        CommandBinds.Unregister<ConsentUiController>();
    }

    internal void UnloadButton()
    {
        if (ConsentButton == null)
            return;

        ConsentButton.OnPressed -= ConsentButtonPressed;
    }

    internal void LoadButton()
    {
        if (ConsentButton == null)
            return;

        ConsentButton.OnPressed += ConsentButtonPressed;
    }

    private void ConsentButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<ConsentWindow>();
        _window.OnOpen += () =>
        {
            if (ConsentButton is not null)
                ConsentButton.Pressed = true;
        };
        _window.OnClose += () =>
        {
            if (ConsentButton is not null)
                ConsentButton.Pressed = false;
            _window?.UpdateUi(); // Discard unsaved changes
        };
    }

    private void ToggleWindow()
    {
        if (_window is null)
            return;

        UIManager.ClickSound();
        if (_window.IsOpen != true)
        {
            _window.OpenCentered();
        }
        else
        {
            _window.Close();
        }
    }
}
