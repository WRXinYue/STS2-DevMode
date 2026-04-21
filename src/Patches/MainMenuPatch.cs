using System;
using DevMode.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace DevMode.Patches;

[HarmonyPatch(typeof(NMainMenu))]
public static class MainMenuPatch {
    private static NMainMenuTextButton? _devModeButton;
    private static NMainMenu? _mainMenuRef;

    // Prefix, not Postfix: NMainMenu._Ready caches focus/signal wiring for the buttons present at that moment.
    [HarmonyPrefix]
    [HarmonyPatch("_Ready")]
    public static void AddDevModeButtonPrefix(NMainMenu __instance) {
        _mainMenuRef = __instance;

        var settingsBtn = __instance.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton");
        if (settingsBtn == null) {
            MainFile.Logger.Warn("DevMode: Could not find Settings button.");
            return;
        }

        var container = settingsBtn.GetParent();

        _devModeButton = MainMenuTextButtonFactory.CreateFrom(
            settingsBtn,
            "DevModeButton",
            I18N.T("menu.developerMode", "Developer Mode"),
            OnDevModeButtonPressed);

        container.AddChild(_devModeButton);
        container.MoveChild(_devModeButton, settingsBtn.GetIndex() + 1);

        MainFile.Logger.Info("DevMode: Button added to main menu (Prefix, before NMainMenu._Ready).");
    }

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void AddDevModeButtonPostfix(NMainMenu __instance) {
        if (__instance != _mainMenuRef || _devModeButton == null || !GodotObject.IsInstanceValid(_devModeButton))
            return;

        // Without "." self-pointers, Godot's geometry-based auto-focus sends Left/Right to the widest row.
        var textRow = __instance.GetNodeOrNull<Control>("%MainMenuTextButtons")
            ?? __instance.GetNodeOrNull<Control>("MainMenuTextButtons");
        if (textRow != null) {
            foreach (var child in textRow.GetChildren()) {
                if (child is NMainMenuTextButton button) {
                    button.FocusNeighborLeft = new NodePath(".");
                    button.FocusNeighborRight = new NodePath(".");
                }
            }
        }

        // SubmenuStack is only valid post-_Ready.
        if (DevModeState.AutoProceedToCharSelect) {
            DevModeState.AutoProceedToCharSelect = false;
            MainFile.Logger.Info("DevMode: Auto-proceeding to character select (Restart with Seed).");
            var charSelect = __instance.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
            charSelect.InitializeSingleplayer();
            __instance.SubmenuStack.Push(charSelect);
        }
    }

    // RefreshButtons toggles visibility on rows it owns; it doesn't know about our row or the dev-submenu override.
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu.RefreshButtons))]
    public static void KeepDevButtonVisible(NMainMenu __instance) {
        if (__instance != _mainMenuRef) return;

        if (_devModeButton != null && GodotObject.IsInstanceValid(_devModeButton)) {
            var settingsBtn = __instance.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton");
            if (settingsBtn != null)
                _devModeButton.Visible = settingsBtn.Visible;
        }

        if (DevMenuUI.IsVisible)
            DevMenuUI.ReapplyHide();
    }

    private static void OnDevModeButtonPressed(NButton _) {
        if (_mainMenuRef == null) return;

        MainFile.Logger.Info("DevMode: Opening dev mode menu...");

        DevMenuUI.Show(_mainMenuRef, new UI.DevMenuActions {
            OnNewTest = () => {
                DevModeState.IsActive = true;
                var charSelect = _mainMenuRef.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
                charSelect.InitializeSingleplayer();
                _mainMenuRef.SubmenuStack.Push(charSelect);
            },
            OnCardLibrary = () => {
                DevModeState.InMenuPreview = true;
                var stack = _mainMenuRef.SubmenuStack;
                DevModeState.OnMenuPreviewClosed = () => {
                    stack.Pop();
                    OnDevModeButtonPressed(null!);
                };
                AccessTools.Method(typeof(NMainMenu), "OpenCompendiumSubmenu")
                    ?.Invoke(_mainMenuRef, new object?[] { null });
                var compendium = stack.Peek();
                AccessTools.Method(compendium.GetType(), "OpenCardLibrary")
                    ?.Invoke(compendium, new object?[] { null });
            },
            OnRelicCollection = () => {
                DevModeState.InMenuPreview = true;
                var stack = _mainMenuRef.SubmenuStack;
                DevModeState.OnMenuPreviewClosed = () => {
                    stack.Pop();
                    OnDevModeButtonPressed(null!);
                };
                AccessTools.Method(typeof(NMainMenu), "OpenCompendiumSubmenu")
                    ?.Invoke(_mainMenuRef, new object?[] { null });
                var compendium = stack.Peek();
                AccessTools.Method(compendium.GetType(), "OpenRelicCollection")
                    ?.Invoke(compendium, new object?[] { null });
            }
        });
    }
}
