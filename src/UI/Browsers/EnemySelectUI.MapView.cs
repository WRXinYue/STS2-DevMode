using System;
using System.Collections.Generic;
using System.Linq;
using DevMode;
using DevMode.Actions;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.UI;

internal static partial class EnemySelectUI {
    private sealed class MapEditorSession {
        public required RunState RunState;
        public required MainBrowserState Browser;
        public required EnemyMapCanvas Canvas;
        public required VBoxContainer DetailHost;
        public MapPoint? SelectedPoint;
        public required Action RefreshAll;
    }

    private static void BuildMapTab(MainBrowserState state) {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState?.Map == null || !DevModeState.InDevRun) {
            state.ContentHost.AddChild(new Label {
                Text = I18N.T("enemy.mapUnavailable", "Start a dev run to edit encounters on the map."),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            return;
        }

        var split = new HBoxContainer {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        split.AddThemeConstantOverride("separation", 12);

        var mapScroll = new ScrollContainer {
            CustomMinimumSize = new Vector2(360, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };

        MapEditorSession? session = null;
        session = new MapEditorSession {
            RunState = runState,
            Browser = state,
            Canvas = null!,
            DetailHost = new VBoxContainer(),
            RefreshAll = () => {
                if (session == null) return;
                session.Canvas.Rebuild(session.SelectedPoint);
                if (session.SelectedPoint != null)
                    BuildMapNodeDetail(session, session.SelectedPoint);
                else
                    BuildMapEmptyDetail(session);
            },
        };

        session.Canvas = new EnemyMapCanvas(runState, point => {
            session!.SelectedPoint = point;
            session.Canvas.Rebuild(point);
            BuildMapNodeDetail(session, point);
        });
        mapScroll.AddChild(session.Canvas);
        split.AddChild(mapScroll);

        var detailPanel = new PanelContainer {
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        detailPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(DevModeTheme.PanelBg.R, DevModeTheme.PanelBg.G, DevModeTheme.PanelBg.B, 0.85f),
            BorderColor = DevModeTheme.PanelBorder,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        });

        session.DetailHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        session.DetailHost.AddThemeConstantOverride("separation", 8);
        detailPanel.AddChild(session.DetailHost);
        split.AddChild(detailPanel);

        state.ContentHost.AddChild(split);
        session.Canvas.Rebuild(null);
        BuildMapEmptyDetail(session);
    }

    private static void BuildMapEmptyDetail(MapEditorSession session) {
        ClearDetailHost(session.DetailHost);
        var hint = new Label {
            Text = I18N.T("enemy.mapSelectHint", "Click a combat node on the map to view or edit its encounter."),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hint.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        hint.AddThemeFontSizeOverride("font_size", 12);
        session.DetailHost.AddChild(hint);
        AddMapClearButtons(session);
    }

    private static void BuildMapNodeDetail(MapEditorSession session, MapPoint point) {
        ClearDetailHost(session.DetailHost);

        if (!MapEncounterPreview.IsCombatNode(point.PointType)) {
            session.DetailHost.AddChild(new Label {
                Text = I18N.T("enemy.mapNonCombat", "This node is not a combat room."),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            AddMapClearButtons(session);
            return;
        }

        var preview = MapEncounterPreview.Build(session.RunState, point);
        if (preview == null) {
            session.DetailHost.AddChild(new Label {
                Text = I18N.T("enemy.mapNoPreview", "Could not preview this node."),
            });
            AddMapClearButtons(session);
            return;
        }

        string typeTag = preview.CombatRoomType switch {
            RoomType.Monster => I18N.T("map.roomNormal", "Normal"),
            RoomType.Elite => I18N.T("map.roomElite", "Elite"),
            RoomType.Boss => I18N.T("map.roomBoss", "Boss"),
            _ => preview.CombatRoomType.ToString(),
        };

        var header = new Label {
            Text = I18N.T("map.tooltipHeader", "[{0}] Floor {1}", typeTag, preview.Floor),
        };
        header.AddThemeFontSizeOverride("font_size", 14);
        header.AddThemeColorOverride("font_color", RoomTypeColor(preview.CombatRoomType));
        session.DetailHost.AddChild(header);

        if (preview.IsCurrentRoom) {
            session.DetailHost.AddChild(MakeSubtleLabel(
                I18N.T("enemy.mapCurrentRoom", "You are currently in this room.")));
        }

        if (preview.Encounter is { } encounter) {
            string encounterName = encounter.Title?.GetFormattedText()
                ?? ((AbstractModel)encounter).Id.Entry;
            session.DetailHost.AddChild(new Label {
                Text = preview.IsOverride
                    ? I18N.T("map.override", "{0} (Override)", encounterName)
                    : encounterName,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });

            string encId = ((AbstractModel)encounter).Id.Entry;
            if (encId != encounterName)
                session.DetailHost.AddChild(MakeSubtleLabel(encId));

            var monsters = encounter.AllPossibleMonsters?.ToList();
            if (monsters is { Count: > 0 }) {
                session.DetailHost.AddChild(BuildMonsterPreviewRow(monsters));
                session.DetailHost.AddChild(MakeSubtleLabel(string.Join(", ",
                    monsters.Select(m => m.Title?.GetFormattedText() ?? ((AbstractModel)m).Id.Entry).Distinct())));
            }
        }
        else {
            session.DetailHost.AddChild(MakeSubtleLabel(
                I18N.T("enemy.mapNoEncounter", "No encounter predicted for this node.")));
        }

        if (preview.IsFloorOverride)
            session.DetailHost.AddChild(MakeSubtleLabel(
                I18N.T("enemy.mapFloorOverride", "Floor override is active for this node.")));
        else if (preview.IsGlobalOrTypeOverride)
            session.DetailHost.AddChild(MakeSubtleLabel(
                I18N.T("enemy.mapInheritedOverride", "Using global or per-type override (no floor override).")));

        session.DetailHost.AddChild(new HSeparator());

        if (!preview.IsCurrentRoom) {
            var replaceBtn = new Button {
                Text = I18N.T("enemy.mapReplaceEncounter", "Replace encounter"),
                CustomMinimumSize = new Vector2(0, 34),
                FocusMode = Control.FocusModeEnum.None,
            };
            replaceBtn.Pressed += () => {
                RoomType? filter = preview.CombatRoomType;
                int floor = preview.Floor;
                ShowEncounterOverlay(session.Browser.GlobalUi, filter, enc => {
                    EnemyActions.SetFloorOverride(floor, enc);
                    session.Browser.StatusLabel.Text = I18N.T(
                        "enemy.appliedFloor",
                        "Floor {0} set to {1}.",
                        floor,
                        EnemyActions.GetShortName(enc));
                    session.RefreshAll();
                });
            };
            session.DetailHost.AddChild(replaceBtn);

            if (preview.IsFloorOverride) {
                var clearFloorBtn = new Button {
                    Text = I18N.T("enemy.mapClearFloor", "Clear floor override"),
                    CustomMinimumSize = new Vector2(0, 32),
                    FocusMode = Control.FocusModeEnum.None,
                };
                clearFloorBtn.Pressed += () => {
                    EnemyActions.ClearFloorOverride(preview.Floor);
                    session.Browser.StatusLabel.Text = I18N.T(
                        "enemy.clearedFloor",
                        "Cleared floor {0} override.",
                        preview.Floor);
                    session.RefreshAll();
                };
                session.DetailHost.AddChild(clearFloorBtn);
            }
        }

        AddMapClearButtons(session);
    }

    private static void AddMapClearButtons(MapEditorSession session) {
        session.DetailHost.AddChild(new HSeparator());

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var clearFloorsBtn = new Button {
            Text = I18N.T("enemy.clearFloors", "Clear floor overrides"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
        };
        clearFloorsBtn.Pressed += () => {
            DevModeState.FloorOverrides.Clear();
            session.Browser.StatusLabel.Text = I18N.T("enemy.byFloorHint",
                "Floor overrides apply on top of global / per-type settings. Right-click a combat node on the map to replace its encounter.");
            session.RefreshAll();
        };

        var clearAllBtn = new Button {
            Text = I18N.T("enemy.clearAllOverrides", "Clear all overrides"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
        };
        clearAllBtn.Pressed += () => {
            DevModeState.ClearEnemyOverrides();
            session.Browser.StatusLabel.Text = I18N.T(
                "enemy.clearedOverrides", "All enemy overrides cleared.");
            session.RefreshAll();
        };

        row.AddChild(clearFloorsBtn);
        row.AddChild(clearAllBtn);
        session.DetailHost.AddChild(row);
    }

    private static Control BuildMonsterPreviewRow(IList<MonsterModel> monsters) {
        var container = new SubViewportContainer {
            CustomMinimumSize = new Vector2(Math.Min(monsters.Count * 72, 216), 90),
            StretchShrink = 1,
            Stretch = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var viewport = new SubViewport {
            Size = new Vector2I(Math.Min(monsters.Count * 72, 216), 90),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        container.AddChild(viewport);
        LoadVisualsIntoViewport(viewport, monsters, maxCount: 3);
        return container;
    }

    private static Label MakeSubtleLabel(string text) {
        var label = new Label {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        label.AddThemeFontSizeOverride("font_size", 11);
        return label;
    }

    private static void ClearDetailHost(VBoxContainer host) {
        foreach (var child in host.GetChildren())
            ((Node)child).QueueFree();
    }

    private static Color RoomTypeColor(RoomType roomType) => roomType switch {
        RoomType.Elite => new Color(1f, 0.8f, 0.27f),
        RoomType.Boss => new Color(1f, 0.27f, 0.27f),
        _ => new Color(0.53f, 0.8f, 0.53f),
    };

    private static Color NodeFillColor(MapPointType type) => type switch {
        MapPointType.Monster => new Color(0.18f, 0.38f, 0.22f, 0.95f),
        MapPointType.Elite => new Color(0.38f, 0.28f, 0.08f, 0.95f),
        MapPointType.Boss => new Color(0.42f, 0.12f, 0.12f, 0.95f),
        MapPointType.Shop => new Color(0.16f, 0.20f, 0.34f, 0.85f),
        MapPointType.RestSite => new Color(0.14f, 0.28f, 0.34f, 0.85f),
        MapPointType.Treasure => new Color(0.30f, 0.24f, 0.10f, 0.85f),
        _ => new Color(0.16f, 0.16f, 0.22f, 0.80f),
    };

    private sealed partial class EnemyMapCanvas : Control {
        private const float Pad = 24f;
        private const float CellW = 52f;
        private const float CellH = 58f;
        private const float NodeR = 14f;

        private readonly RunState _runState;
        private readonly Action<MapPoint> _onSelect;
        private readonly List<(MapPoint from, MapPoint to)> _edges = new();
        private readonly Dictionary<MapCoord, MapPoint> _pointsByCoord = new();
        private MapCoord? _currentCoord;
        private MapPoint? _selectedPoint;

        public EnemyMapCanvas(RunState runState, Action<MapPoint> onSelect) {
            _runState = runState;
            _onSelect = onSelect;
            MouseFilter = MouseFilterEnum.Ignore;
            ClipContents = false;
            Rebuild(null);
        }

        public void Rebuild(MapPoint? selectedPoint) {
            _selectedPoint = selectedPoint;
            _currentCoord = _runState.CurrentMapCoord;

            foreach (var child in GetChildren())
                child.QueueFree();

            _edges.Clear();
            _pointsByCoord.Clear();

            var map = _runState.Map;
            if (map == null) return;

            var points = map.GetAllMapPoints().ToList();
            if (points.Count == 0) return;

            foreach (var point in points)
                _pointsByCoord[point.coord] = point;

            int minCol = points.Min(p => p.coord.col);
            int maxCol = points.Max(p => p.coord.col);
            int minRow = points.Min(p => p.coord.row);
            int maxRow = points.Max(p => p.coord.row);

            CustomMinimumSize = new Vector2(
                Pad * 2 + (maxCol - minCol + 1) * CellW,
                Pad * 2 + (maxRow - minRow + 1) * CellH);
            Size = CustomMinimumSize;

            foreach (var point in points) {
                foreach (var child in point.Children) {
                    if (_pointsByCoord.ContainsKey(child.coord))
                        _edges.Add((point, child));
                }
            }

            var lines = new MapEdgeLayer(_edges, minCol, maxRow, Pad, CellW, CellH);
            AddChild(lines);
            MoveChild(lines, 0);

            foreach (var point in points)
                AddChild(CreateNodeButton(point, minCol, maxRow));

            QueueRedraw();
        }

        private Control CreateNodeButton(MapPoint point, int minCol, int maxRow) {
            bool isCombat = MapEncounterPreview.IsCombatNode(point.PointType);
            bool isCurrent = _currentCoord.HasValue && point.coord.Equals(_currentCoord.Value);
            bool isSelected = _selectedPoint != null && point.coord.Equals(_selectedPoint.coord);
            bool hasFloorOverride = DevModeState.FloorOverrides.ContainsKey(point.coord.row + 1);

            var btn = new Button {
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = isCombat ? CursorShape.PointingHand : CursorShape.Arrow,
                Text = NodeGlyph(point.PointType),
                Position = NodePos(point.coord, minCol, maxRow) - new Vector2(NodeR, NodeR),
                Size = new Vector2(NodeR * 2, NodeR * 2),
                Disabled = !isCombat,
                TooltipText = BuildNodeTooltip(point),
            };
            btn.AddThemeFontSizeOverride("font_size", 10);

            var fill = NodeFillColor(point.PointType);
            if (isSelected)
                fill = fill.Lightened(0.18f);

            var border = isSelected
                ? DevModeTheme.Accent
                : isCurrent
                    ? new Color(0.95f, 0.85f, 0.35f)
                    : hasFloorOverride && isCombat
                        ? new Color(DevModeTheme.Accent.R, DevModeTheme.Accent.G, DevModeTheme.Accent.B, 0.85f)
                        : DevModeTheme.PanelBorder;

            int bw = isSelected || isCurrent ? 2 : 1;
            btn.AddThemeStyleboxOverride("normal", MakeNodeStyle(fill, border, bw));
            btn.AddThemeStyleboxOverride("hover", MakeNodeStyle(fill.Lightened(0.12f), border.Lightened(0.1f), bw));
            btn.AddThemeStyleboxOverride("pressed", MakeNodeStyle(fill.Darkened(0.08f), border, bw));
            btn.AddThemeStyleboxOverride("disabled", MakeNodeStyle(fill.Darkened(0.15f), border, bw));
            btn.AddThemeStyleboxOverride("focus", MakeNodeStyle(fill, border, bw));
            btn.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
            btn.AddThemeColorOverride("font_disabled_color", DevModeTheme.Subtle);

            if (isCombat) {
                btn.Pressed += () => _onSelect(point);
            }

            return btn;
        }

        private static StyleBoxFlat MakeNodeStyle(Color bg, Color border, int borderWidth) => new() {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 999,
            CornerRadiusTopRight = 999,
            CornerRadiusBottomLeft = 999,
            CornerRadiusBottomRight = 999,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };

        private static Vector2 NodePos(MapCoord coord, int minCol, int maxRow) =>
            new(
                Pad + (coord.col - minCol) * CellW + CellW * 0.5f,
                Pad + (maxRow - coord.row) * CellH + CellH * 0.5f);

        private string BuildNodeTooltip(MapPoint point) {
            int floor = point.coord.row + 1;
            if (!MapEncounterPreview.IsCombatNode(point.PointType))
                return I18N.T("enemy.mapNodeFloor", "Floor {0} · {1}", floor, point.PointType);

            var preview = MapEncounterPreview.Build(_runState, point);
            if (preview?.Encounter == null)
                return I18N.T("enemy.mapNodeFloor", "Floor {0} · {1}", floor, point.PointType);

            string name = preview.Encounter.Title?.GetFormattedText()
                ?? ((AbstractModel)preview.Encounter).Id.Entry;
            if (preview.IsOverride)
                name = I18N.T("map.override", "{0} (Override)", name);
            return I18N.T("enemy.mapNodeTooltip", "Floor {0}: {1}", floor, name);
        }

        private static string NodeGlyph(MapPointType type) => type switch {
            MapPointType.Monster => "M",
            MapPointType.Elite => "E",
            MapPointType.Boss => "B",
            MapPointType.Shop => "$",
            MapPointType.RestSite => "R",
            MapPointType.Treasure => "T",
            _ => "·",
        };

        private sealed partial class MapEdgeLayer : Control {
            private readonly List<(MapPoint from, MapPoint to)> _edges;
            private readonly int _minCol;
            private readonly int _maxRow;
            private readonly float _pad;
            private readonly float _cellW;
            private readonly float _cellH;

            public MapEdgeLayer(
                List<(MapPoint from, MapPoint to)> edges,
                int minCol,
                int maxRow,
                float pad,
                float cellW,
                float cellH) {
                _edges = edges;
                _minCol = minCol;
                _maxRow = maxRow;
                _pad = pad;
                _cellW = cellW;
                _cellH = cellH;
                MouseFilter = MouseFilterEnum.Ignore;
                SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            }

            public override void _Draw() {
                var lineColor = new Color(0.45f, 0.48f, 0.58f, 0.55f);
                foreach (var (from, to) in _edges) {
                    Vector2 a = NodePos(from.coord, _minCol, _maxRow);
                    Vector2 b = NodePos(to.coord, _minCol, _maxRow);
                    DrawLine(a, b, lineColor, 2f, true);
                }
            }
        }
    }
}
