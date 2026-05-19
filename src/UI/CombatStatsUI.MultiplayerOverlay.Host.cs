using System;
using System.Collections.Generic;
using System.Linq;
using DevMode.CombatStats;
using Godot;

namespace DevMode.UI;

internal static partial class CombatStatsUI {
    private static class MpOverlayLayout {
        public const float PanelWidth = 400f;
        public const float Margin = 10f;
        public const float BarHeight = 12f;
        public const float BarTrackWidth = 240f;
        public const float RowHeight = 26f;
        public const float NameWidth = 96f;
        public const float ScoreWidth = 40f;
        public const float ScoreRightPadding = 6f;
        public const float BarCornerRadius = 5f;
        public const float SegmentGap = 1f;
        /// <summary>Above browser overlays (1250), below card edit overlays (1400).</summary>
        public const int ZIndex = 1310;
    }

    /// <summary>Top-right floating panel for multiplayer combat score comparison.</summary>
    private sealed partial class MultiplayerOverlayHost : Control {
        private readonly PanelContainer _panel;
        private readonly StyleBoxFlat _panelStyle;
        private readonly VBoxContainer _playerList;
        private readonly DraggablePanelBinding _drag;

        private bool _usingFreePosition;

        public bool IsPanelVisible => _panel.Visible;

        public MultiplayerOverlayHost() {
            Name = MultiplayerOverlayRootName;
            MouseFilter = MouseFilterEnum.Ignore;
            ZIndex = MpOverlayLayout.ZIndex;
            SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            _panel = new PanelContainer {
                Name = "MpStatsPanel",
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false,
                CustomMinimumSize = new Vector2(MpOverlayLayout.PanelWidth, 0),
            };
            ApplyDefaultPanelLayout();

            _panelStyle = CreatePanelStyle();
            _panel.AddThemeStyleboxOverride("panel", _panelStyle);

            _drag = new DraggablePanelBinding(this, _panel, () => _usingFreePosition, v => _usingFreePosition = v);

            var body = new VBoxContainer();
            body.AddThemeConstantOverride("separation", 4);
            body.AddChild(BuildTitleRow());
            _playerList = new VBoxContainer();
            _playerList.AddThemeConstantOverride("separation", 5);
            body.AddChild(_playerList);
            _panel.AddChild(body);

            AddChild(_panel);

            ThemeManager.OnThemeChanged += OnThemeChanged;
            TreeExiting += () => ThemeManager.OnThemeChanged -= OnThemeChanged;
        }

        public void Refresh() {
            if (!CanShowMultiplayerOverlay()) {
                HidePanel();
                return;
            }

            var players = ResolvePlayers();
            if (players.Count < 2) {
                HidePanel();
                return;
            }

            int maxScore = Math.Max(1, players.Max(CombatScoreCalculator.TotalScore));
            SyncPlayerRows(players, maxScore);

            _panel.Visible = true;
            MoveToFront();
        }

        public void HidePanel() => _panel.Visible = false;

        private void OnThemeChanged() {
            var theme = ThemeManager.Current;
            _panelStyle.BgColor = theme.RailBg;
            _panelStyle.BorderColor = theme.RailBorder;

            foreach (var child in _playerList.GetChildren()) {
                if (child is MpOverlayPlayerRow row)
                    row.RefreshTheme();
            }
        }

        private static List<PlayerCombatStats> ResolvePlayers() {
            var snap = CombatStatsTracker.IsTracking
                ? CombatStatsTracker.Current
                : CombatStatsTracker.Last;
            return snap?.Players.Values.ToList() ?? new List<PlayerCombatStats>();
        }

        private void SyncPlayerRows(List<PlayerCombatStats> players, int maxScore) {
            var ordered = players
                .OrderByDescending(CombatScoreCalculator.TotalScore)
                .ThenBy(p => p.DisplayName)
                .ToList();

            var existing = new Dictionary<string, MpOverlayPlayerRow>(StringComparer.Ordinal);
            foreach (var child in _playerList.GetChildren()) {
                if (child is MpOverlayPlayerRow row)
                    existing[row.PlayerKey] = row;
            }

            var keepKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ordered.Count; i++) {
                var player = ordered[i];
                keepKeys.Add(player.Key);

                if (!existing.TryGetValue(player.Key, out var row) || !GodotObject.IsInstanceValid(row)) {
                    row = new MpOverlayPlayerRow();
                    _playerList.AddChild(row);
                }

                row.Bind(
                    player,
                    CombatScoreCalculator.TotalScore(player),
                    maxScore,
                    isLeader: i == 0);
                if (row.GetIndex() != i)
                    _playerList.MoveChild(row, i);
            }

            foreach (var child in _playerList.GetChildren()) {
                if (child is MpOverlayPlayerRow row && !keepKeys.Contains(row.PlayerKey))
                    row.QueueFree();
            }
        }

        private Control BuildTitleRow() {
            var titleRow = new HBoxContainer {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.Move,
                TooltipText = I18N.T("combatStats.mpOverlay.dragHint", "Drag to move panel"),
            };
            titleRow.AddThemeConstantOverride("separation", 4);
            _drag.WireHandle(titleRow);

            var title = new Label {
                Text = I18N.T("combatStats.sidebar.players", "Player scores"),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            title.AddThemeFontSizeOverride("font_size", 10);
            title.AddThemeColorOverride("font_color", DevModeTheme.TextSecondary);
            titleRow.AddChild(title);
            return titleRow;
        }

        private void ApplyDefaultPanelLayout() {
            _panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
            _panel.OffsetTop = MpOverlayLayout.Margin;
            _panel.OffsetRight = -MpOverlayLayout.Margin;
            _panel.OffsetLeft = -(MpOverlayLayout.PanelWidth + MpOverlayLayout.Margin);
            _usingFreePosition = false;
        }

        private static StyleBoxFlat CreatePanelStyle() {
            var theme = ThemeManager.Current;
            return new StyleBoxFlat {
                BgColor = theme.RailBg,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 10,
                ContentMarginRight = 10,
                ContentMarginTop = 8,
                ContentMarginBottom = 8,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderColor = theme.RailBorder,
                ShadowColor = new Color(0, 0, 0, 0.25f),
                ShadowSize = 6,
            };
        }

        public override void _Process(double delta) => _drag.Process();
    }

    /// <summary>Drag title bar → move panel within host bounds.</summary>
    private sealed class DraggablePanelBinding {
        private readonly Control _host;
        private readonly PanelContainer _panel;
        private readonly Func<bool> _isFreePosition;
        private readonly Action<bool> _setFreePosition;
        private bool _dragging;
        private Vector2 _dragOffset;

        public DraggablePanelBinding(
            Control host,
            PanelContainer panel,
            Func<bool> isFreePosition,
            Action<bool> setFreePosition) {
            _host = host;
            _panel = panel;
            _isFreePosition = isFreePosition;
            _setFreePosition = setFreePosition;
        }

        public void WireHandle(Control handle) {
            handle.GuiInput += e => {
                if (e is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left)
                    return;

                if (mb.Pressed) {
                    EnsureFreePosition();
                    var mouseLocal = _host.GetGlobalTransformWithCanvas().AffineInverse()
                        * _host.GetGlobalMousePosition();
                    _dragOffset = mouseLocal - _panel.Position;
                    _dragging = true;
                    handle.AcceptEvent();
                    return;
                }

                if (_dragging) {
                    _dragging = false;
                    ClampPanel();
                    handle.AcceptEvent();
                }
            };
        }

        public void Process() {
            if (!_dragging)
                return;

            if (!Input.IsMouseButtonPressed(MouseButton.Left)) {
                _dragging = false;
                ClampPanel();
                return;
            }

            var mouseLocal = _host.GetGlobalTransformWithCanvas().AffineInverse()
                * _host.GetGlobalMousePosition();
            _panel.Position = mouseLocal - _dragOffset;
        }

        private void EnsureFreePosition() {
            if (_isFreePosition())
                return;

            var pos = _panel.Position;
            var size = _panel.Size;
            if (size.X <= 0f)
                size.X = MpOverlayLayout.PanelWidth;
            if (size.Y <= 0f)
                size.Y = _panel.GetCombinedMinimumSize().Y;

            _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
            _panel.Size = size;
            _panel.Position = pos;
            _setFreePosition(true);
        }

        private void ClampPanel() {
            var size = _panel.Size;
            if (size.X <= 0f || size.Y <= 0f)
                return;

            var pos = _panel.Position;
            pos.X = Math.Clamp(pos.X, 0f, Math.Max(0f, _host.Size.X - size.X));
            pos.Y = Math.Clamp(pos.Y, 0f, Math.Max(0f, _host.Size.Y - size.Y));
            _panel.Position = pos;
        }
    }
}
