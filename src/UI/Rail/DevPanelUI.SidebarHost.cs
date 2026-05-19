using System;
using System.Collections.Generic;
using Godot;

namespace DevMode.UI;

/// <summary>Swappable content for a <see cref="DevPanelSidebarHost"/> pane.</summary>
internal interface IDevPanelSidebarProvider {
    Control Root { get; }
    string Title { get; }
    string Hint { get; }
    bool HasContent { get; }
    void Refresh();
}

/// <summary>Title + hint + content slot for a permanent panel sidebar.</summary>
internal sealed partial class DevPanelSidebarHost : VBoxContainer {
    private readonly Label _title;
    private readonly Label _hint;
    private readonly VBoxContainer _content;
    private readonly Dictionary<string, IDevPanelSidebarProvider> _providers = new();
    private readonly bool _railCompact;
    private string? _activeId;

    public DevPanelSidebarHost(string name, bool railCompact = false) {
        Name = name;
        _railCompact = railCompact;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", railCompact ? 0 : 8);

        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", railCompact ? 9 : 12);
        _title.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        _title.Visible = !railCompact;
        AddChild(_title);

        _hint = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _hint.AddThemeFontSizeOverride("font_size", 10);
        _hint.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        _hint.Visible = !railCompact;
        AddChild(_hint);

        _content = new VBoxContainer {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _content.AddThemeConstantOverride("separation", railCompact ? 2 : 8);
        AddChild(_content);
    }

    public void Register(string id, IDevPanelSidebarProvider provider) {
        _providers[id] = provider;
    }

    public void SetActive(string id) {
        if (!_providers.ContainsKey(id))
            return;
        _activeId = id;
        MountActiveProvider();
        RefreshChrome();
    }

    public void RefreshChrome() {
        if (_activeId == null || !_providers.TryGetValue(_activeId, out var active))
            return;
        if (!_railCompact) {
            _title.Text = active.Title;
            _hint.Text = active.Hint;
        }
        else {
            _title.Text = "";
            _hint.Text = "";
        }
    }

    public void RefreshActive() {
        if (_activeId == null || !_providers.TryGetValue(_activeId, out var active))
            return;
        active.Refresh();
        RefreshChrome();
    }

    public bool ActiveHasContent =>
        _activeId != null
        && _providers.TryGetValue(_activeId, out var active)
        && active.HasContent;

    private void MountActiveProvider() {
        while (_content.GetChildCount() > 0) {
            var child = _content.GetChild(0);
            _content.RemoveChild(child);
        }
        if (_activeId == null || !_providers.TryGetValue(_activeId, out var active))
            return;

        active.Root.SizeFlagsVertical = SizeFlags.ExpandFill;
        active.Root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        active.Root.Visible = true;
        _content.AddChild(active.Root);
    }
}
