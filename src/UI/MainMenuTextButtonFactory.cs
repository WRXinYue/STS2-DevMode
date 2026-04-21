using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace DevMode.UI;

internal static class MainMenuTextButtonFactory {
    // Cleared on the clone so NMainMenuTextButton.RefreshLabel (fired on locale change) won't replace our text with the template's.
    private static readonly FieldInfo? LocStringField =
        AccessTools.Field(typeof(NMainMenuTextButton), "_locString");

    // Groups | Scripts | UseInstantiation — omitting Signals (1) so the template's Released handler doesn't carry over.
    private const int DuplicateFlags = 14;

    public static NMainMenuTextButton CreateFrom(
        NMainMenuTextButton template,
        string name,
        string text,
        Action<NButton> onReleased
    ) {
        var btn = (NMainMenuTextButton)template.Duplicate(DuplicateFlags);
        btn.Name = name;
        btn.Visible = true;

        LocStringField?.SetValue(btn, null);
        if (btn.label != null)
            btn.label.Text = text;

        btn.Connect(NClickableControl.SignalName.Released, Callable.From(onReleased));
        return btn;
    }
}
