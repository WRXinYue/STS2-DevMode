using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using DevMode.Actions;
using DevMode.UI;

namespace DevMode.Patches;

/// <summary>
/// Adds encounter preview tooltip on map node hover and right-click to replace encounter.
/// </summary>
[HarmonyPatch(typeof(NMapPoint), "OnFocus")]
public static class MapPointHoverPatch
{
    private const string TooltipName = "DevModeMapTooltip";
    private static readonly FieldInfo? _roomsField =
        typeof(ActModel).GetField("_rooms", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Postfix(NMapPoint __instance)
    {
        if (!DevModeState.InDevRun) return;

        var point = __instance.Point;
        if (point == null) return;

        // Only show for combat nodes
        var pointType = point.PointType;
        if (pointType is not (MapPointType.Monster or MapPointType.Elite or MapPointType.Boss))
            return;

        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null) return;

        var roomType = pointType switch
        {
            MapPointType.Monster => RoomType.Monster,
            MapPointType.Elite   => RoomType.Elite,
            MapPointType.Boss    => RoomType.Boss,
            _                    => RoomType.Unassigned
        };
        if (roomType == RoomType.Unassigned) return;

        int floor = point.coord.row + 1;

        // Check for floor override first
        var overrideEnc = DevModeState.ResolveOverride(roomType, floor);
        EncounterModel? encounter = overrideEnc ?? PredictEncounter(state, point, roomType);
        if (encounter == null) return;

        bool isOverride = overrideEnc != null;
        ShowTooltip(__instance, encounter, floor, roomType, isOverride);
    }

    private static EncounterModel? PredictEncounter(RunState state, MapPoint point, RoomType roomType)
    {
        try
        {
            var act = state.Act;
            if (act == null) return null;

            if (roomType == RoomType.Boss)
                return act.BossEncounter;

            var roomSet = _roomsField?.GetValue(act) as RoomSet;
            if (roomSet == null)
                return act.PullNextEncounter(roomType);

            int offset = CountSameTypeNodesBefore(state, point, roomType);

            if (roomType == RoomType.Monster && roomSet.normalEncounters.Count > 0)
            {
                int idx = (roomSet.normalEncountersVisited + offset) % roomSet.normalEncounters.Count;
                return roomSet.normalEncounters[idx];
            }
            else if (roomType == RoomType.Elite && roomSet.eliteEncounters.Count > 0)
            {
                int idx = (roomSet.eliteEncountersVisited + offset) % roomSet.eliteEncounters.Count;
                return roomSet.eliteEncounters[idx];
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"MapPreview: Failed to predict encounter: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Count how many unvisited nodes of the same room type appear before this node
    /// in the map traversal order (row by row, left to right).
    /// This gives us the offset into the encounter queue.
    /// </summary>
    private static int CountSameTypeNodesBefore(RunState state, MapPoint targetPoint, RoomType roomType)
    {
        var targetType = roomType == RoomType.Monster ? MapPointType.Monster : MapPointType.Elite;
        int count = 0;

        try
        {
            var map = state.Map;
            if (map == null) return 0;

            var visited = new HashSet<MapCoord>(state.VisitedMapCoords);

            // Get all map points sorted by row then column
            var allPoints = map.GetAllMapPoints()
                .Where(p => p.PointType == targetType)
                .OrderBy(p => p.coord.row)
                .ThenBy(p => p.coord.col)
                .ToList();

            // Count unvisited same-type nodes that come before target in row order
            foreach (var p in allPoints)
            {
                if (p.coord.row == targetPoint.coord.row && p.coord.col == targetPoint.coord.col)
                    break;
                if (!visited.Contains(p.coord))
                    count++;
            }
        }
        catch { /* ignore */ }

        return count;
    }

    private static void ShowTooltip(NMapPoint mapPoint, EncounterModel encounter, int floor, RoomType roomType, bool isOverride)
    {
        // Remove existing tooltip
        RemoveTooltip(mapPoint);

        var encounterName = encounter.Title?.GetFormattedText()
            ?? ((AbstractModel)encounter).Id.Entry;
        var encId = ((AbstractModel)encounter).Id.Entry;

        var typeTag = roomType switch
        {
            RoomType.Monster => I18N.T("map.roomNormal", "Normal"),
            RoomType.Elite   => I18N.T("map.roomElite", "Elite"),
            RoomType.Boss    => I18N.T("map.roomBoss", "Boss"),
            _                => ""
        };

        var tagColor = roomType switch
        {
            RoomType.Elite => new Color(1f, 0.8f, 0.27f),
            RoomType.Boss  => new Color(1f, 0.27f, 0.27f),
            _              => new Color(0.53f, 0.8f, 0.53f)
        };

        var panel = new PanelContainer
        {
            Name = TooltipName,
            ZIndex = 1500,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.1f, 0.95f),
            ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 8, ContentMarginBottom = 8,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor = new Color(0.4f, 0.4f, 0.55f, 0.7f)
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Header: [type] floor N
        var headerLabel = new Label
        {
            Text = I18N.T("map.tooltipHeader", "[{0}] Floor {1}", typeTag, floor),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        headerLabel.AddThemeColorOverride("font_color", tagColor);
        headerLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(headerLabel);

        // Encounter name
        var nameLabel = new Label
        {
            Text = isOverride ? I18N.T("map.override", "{0} (Override)", encounterName) : encounterName,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.85f));
        vbox.AddChild(nameLabel);

        // Encounter ID (smaller)
        if (encId != encounterName)
        {
            var idLabel = new Label
            {
                Text = encId,
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            idLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
            idLabel.AddThemeFontSizeOverride("font_size", 11);
            vbox.AddChild(idLabel);
        }

        // Monster visuals in a row
        var monsters = encounter.AllPossibleMonsters?.ToList();
        if (monsters != null && monsters.Count > 0)
        {
            var visualsContainer = new SubViewportContainer
            {
                CustomMinimumSize = new Vector2(Math.Min(monsters.Count * 80, 240), 100),
                StretchShrink = 1,
                Stretch = true,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            var subViewport = new SubViewport
            {
                Size = new Vector2I(Math.Min(monsters.Count * 80, 240), 100),
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            visualsContainer.AddChild(subViewport);

            float spacing = Math.Min(80f, 240f / monsters.Count);
            for (int i = 0; i < Math.Min(monsters.Count, 3); i++)
            {
                var visuals = EnemySelectUI.TryCreateVisualsPublic(monsters[i]);
                if (visuals != null)
                {
                    float scale = 0.3f;
                    visuals.Scale = new Vector2(scale, scale);
                    visuals.Position = new Vector2(spacing * i + spacing / 2, 80);
                    subViewport.AddChild(visuals);
                }
            }

            vbox.AddChild(visualsContainer);
        }

        // Monster names list
        if (monsters != null && monsters.Count > 0)
        {
            var monsterNames = monsters
                .Select(m => m.Title?.GetFormattedText() ?? ((AbstractModel)m).Id.Entry)
                .Distinct();
            var monstersLabel = new Label
            {
                Text = string.Join(", ", monsterNames),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(160, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            monstersLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.75f, 0.85f));
            monstersLabel.AddThemeFontSizeOverride("font_size", 11);
            vbox.AddChild(monstersLabel);
        }

        // Hint
        var hintLabel = new Label
        {
            Text = I18N.T("map.rightClickHint", "Right-click to replace"),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        hintLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.55f));
        hintLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(hintLabel);

        panel.AddChild(vbox);

        // Position above the map point
        mapPoint.AddChild(panel);
        panel.Position = new Vector2(-120, -panel.Size.Y - 20);

        // Defer position update after layout
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(panel))
                panel.Position = new Vector2(-120, -panel.Size.Y - 20);
        }).CallDeferred();
    }

    private static void RemoveTooltip(NMapPoint mapPoint)
    {
        mapPoint.GetNodeOrNull<Control>(TooltipName)?.QueueFree();
    }
}

[HarmonyPatch(typeof(NMapPoint), "OnUnfocus")]
public static class MapPointUnhoverPatch
{
    public static void Postfix(NMapPoint __instance)
    {
        if (!DevModeState.InDevRun) return;
        __instance.GetNodeOrNull<Control>("DevModeMapTooltip")?.QueueFree();
    }
}

/// <summary>
/// Intercepts right-click on map nodes to allow encounter replacement.
/// Patches NClickableControl._GuiInput since NMapPoint doesn't override it.
/// </summary>
[HarmonyPatch(typeof(NClickableControl), nameof(NClickableControl._GuiInput))]
public static class MapPointRightClickPatch
{
    public static bool Prefix(NClickableControl __instance, InputEvent inputEvent)
    {
        if (!DevModeState.InDevRun) return true;
        if (__instance is not NMapPoint mapPoint) return true;

        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            var point = mapPoint.Point;
            if (point == null) return true;

            var pointType = point.PointType;
            if (pointType is not (MapPointType.Monster or MapPointType.Elite or MapPointType.Boss))
                return true;

            int floor = point.coord.row + 1;
            var filter = pointType switch
            {
                MapPointType.Monster => RoomType.Monster,
                MapPointType.Elite   => RoomType.Elite,
                MapPointType.Boss    => RoomType.Boss,
                _                    => (RoomType?)null
            };

            // Open encounter selector for this floor
            var globalUi = NRun.Instance?.GlobalUi;
            if (globalUi != null)
            {
                EnemySelectUI.Show(globalUi, filter, enc =>
                {
                    EnemyActions.SetFloorOverride(floor, enc);
                    MainFile.Logger.Info($"MapPreview: Floor {floor} override set to {EnemyActions.GetShortName(enc)}");
                });
            }

            return false; // consume the event
        }

        return true;
    }
}
