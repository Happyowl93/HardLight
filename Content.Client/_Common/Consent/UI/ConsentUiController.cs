// SPDX-FileCopyrightText: Copyright (c) 2024-2025 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Client._Common.Consent.UI.Windows;
using Content.Client.Gameplay;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using JetBrains.Annotations;

namespace Content.Client._Common.Consent.UI;

[UsedImplicitly]
public sealed class ConsentUiController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IInputManager _input = default!;

    private ConsentWindow? _window;

    public void OnStateEntered(GameplayState state)
    {
        EnsureWindow();

        _input.SetInputCommand(ContentKeyFunctions.OpenConsentWindow,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<ConsentWindow>();
        _window.OnClose += () =>
        {
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
