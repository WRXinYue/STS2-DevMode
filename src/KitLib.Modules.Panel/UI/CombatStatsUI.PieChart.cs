using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KitLib.CombatStats;

namespace KitLib.UI;

internal static partial class CombatStatsUI {
    private const int PieChartSize = 168;

    private static List<(string Name, int Amount, Color Color)> BuildPieSlices(
        Dictionary<string, int> data,
        int totalForPct,
        int limit,
        bool showAllEntries = false) {
        var entries = showAllEntries
            ? data.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value)).ToList()
            : TopEntries(data, limit).ToList();
        var slices = new List<(string Name, int Amount, Color Color)>(entries.Count + 1);
        int topSum = 0;
        for (int i = 0; i < entries.Count; i++) {
            var (name, amount) = entries[i];
            topSum += amount;
            slices.Add((CombatStatsDisplayNames.LocalizeKey(name), amount, PieSliceColor(i)));
        }

        if (showAllEntries)
            return slices;

        int other = Math.Max(totalForPct, topSum) - topSum;
        if (other > 0 && entries.Count > 0)
            slices.Add((I18N.T("combatStats.pie.other", "Other"), other, PieSliceColor(entries.Count)));

        return slices;
    }

    private static Dictionary<string, int> LocalizeOverviewData(Dictionary<string, int> data) {
        var localized = new Dictionary<string, int>(data.Count);
        foreach (var (kind, amount) in data) {
            string label = kind switch {
                "Damage" => I18N.T("combatStats.score.damage", "Damage"),
                "Block" => I18N.T("combatStats.score.block", "Block"),
                "Debuff" => I18N.T("combatStats.score.debuff", "Debuff"),
                "Buff" => I18N.T("combatStats.score.buff", "Buff"),
                "Utility" => I18N.T("combatStats.score.utility", "Utility cards"),
                "Potion" => I18N.T("combatStats.score.potion", "Potions"),
                "Synergy" => I18N.T("combatStats.score.synergy", "Debuff synergy"),
                _ => kind,
            };
            localized[label] = amount;
        }
        return localized;
    }

    private static Color PieSliceColor(int index) {
        float hue = (KitLibTheme.Accent.H + index * 0.14f) % 1f;
        return Color.FromHsv(hue, 0.58f, 0.92f);
    }

    /// <summary>Pie breakdown panel shown for card/source/turn/timeline views.</summary>
    private sealed partial class CategoryPieSidebarPanel : IDevPanelSidebarProvider {
        private CombatPieCategory _category = CombatPieCategory.Overview;
        private readonly VBoxContainer _root;
        private readonly CombatStatsPieChart _chart;
        private readonly VBoxContainer _legend;
        private readonly Label _emptyLabel;
        private PlayerCombatStats? _selectedPlayer;
        private readonly bool _railCompact;
        private readonly VerticalScoreStack? _compactStack;
        private bool _hasContent;

        public CategoryPieSidebarPanel(string name, bool railCompact = false) {
            _railCompact = railCompact;
            _root = new VBoxContainer { Name = name };
            _root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _root.AddThemeConstantOverride("separation", railCompact ? 2 : 8);

            _chart = new CombatStatsPieChart("stats.pie.chart");
            _root.AddChild(_chart);
            _chart.Visible = !railCompact;

            _legend = new VBoxContainer {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            _legend.AddThemeConstantOverride("separation", 4);
            _root.AddChild(_legend);
            _legend.Visible = !railCompact;

            if (railCompact) {
                _compactStack = new VerticalScoreStack { BarWidth = 10f };
                _compactStack.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                _compactStack.CustomMinimumSize = new Vector2(0, 120);
                _root.AddChild(_compactStack);
            }

            _emptyLabel = new Label {
                Text = I18N.T("combatStats.noData", "No entries yet."),
                Visible = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            _emptyLabel.AddThemeFontSizeOverride("font_size", railCompact ? 8 : 10);
            _emptyLabel.AddThemeColorOverride("font_color", KitLibTheme.Subtle);
            if (!railCompact)
                _root.AddChild(_emptyLabel);

            SetCategory(CombatPieCategory.Overview);
        }

        public Control Root => _root;
        public bool HasContent => _hasContent;
        public string Title => I18N.T("combatStats.pie.title", "Breakdown");
        public string Hint => CategoryHint(_category);

        public void SetContext(PlayerCombatStats? selectedPlayer) => _selectedPlayer = selectedPlayer;

        public void PrepareForViewMode(ViewMode mode) {
            SetCategory(PieCategoryForView(mode));
        }

        public void Refresh() {
            RefreshPlayer(_selectedPlayer);
        }

        internal void RefreshAfterOverlayOpen() => _chart.RefreshAfterOverlayOpen();

        private PlayerCombatStats? _lastPlayer;
        private string? _lastPieFingerprint;

        private void SetCategory(CombatPieCategory category) {
            if (_category == category)
                return;
            _category = category;
            _lastPieFingerprint = null;
            RefreshPlayer(_lastPlayer);
        }

        private void RefreshPlayer(PlayerCombatStats? player) {
            _lastPlayer = player;
            if (player == null) {
                _hasContent = false;
                SetPieVisible(false);
                if (!_railCompact) {
                    _emptyLabel.Visible = true;
                    _emptyLabel.Text = I18N.T("combatStats.noData", "No entries yet.");
                }
                _lastPieFingerprint = null;
                return;
            }

            var (data, total) = CombatScoreCalculator.GetPieCategoryData(player, _category);
            if (_category == CombatPieCategory.Overview)
                data = LocalizeOverviewData(data);

            bool hasData = data.Count > 0 && total > 0;
            _hasContent = hasData;
            SetPieVisible(hasData);
            if (!_railCompact)
                _emptyLabel.Visible = !hasData;

            if (!hasData) {
                _chart.SetSlices(Array.Empty<(string, int, Color)>(), 1);
                ClearLegend();
                _compactStack?.SetSegments(Array.Empty<(string, int, Color)>(), 1);
                if (_railCompact) {
                    ApplyBarTooltip(_compactStack!, "");
                    ApplyBarTooltip(_root, "");
                }
                else {
                    _emptyLabel.Text = I18N.T("combatStats.noData", "No entries yet.");
                }
                _lastPieFingerprint = null;
                return;
            }

            bool showAll = _category == CombatPieCategory.Overview;
            var slices = BuildPieSlices(data, total, limit: 5, showAllEntries: showAll);
            string fingerprint = BuildPieFingerprint(slices, total);
            if (fingerprint == _lastPieFingerprint)
                return;

            _lastPieFingerprint = fingerprint;
            if (_railCompact && _compactStack != null) {
                var segments = new List<(string, int, Color)>(slices.Count);
                foreach (var (name, amount, color) in slices)
                    segments.Add((name, amount, color));
                _compactStack.SetSegments(segments, total);
                string tooltip = FormatPieTooltip(CategoryHint(_category), slices, total);
                ApplyBarTooltip(_compactStack, tooltip);
                ApplyBarTooltip(_root, tooltip);
                return;
            }

            _chart.SetSlices(slices, total);
            UpdateLegend(slices, total);
        }

        private void SetPieVisible(bool visible) {
            if (_railCompact) {
                if (_compactStack != null)
                    _compactStack.Visible = visible;
                return;
            }
            _chart.Visible = visible;
            _legend.Visible = visible;
        }

        private void ClearLegend() {
            while (_legend.GetChildCount() > 0) {
                var child = _legend.GetChild(0);
                _legend.RemoveChild(child);
                child.Free();
            }
        }

        private static string BuildPieFingerprint(
            IReadOnlyList<(string Name, int Amount, Color Color)> slices,
            int total) {
            var sb = new System.Text.StringBuilder(64);
            sb.Append(total).Append('|');
            foreach (var (name, amount, _) in slices)
                sb.Append(name).Append(':').Append(amount).Append('\u001f');
            return sb.ToString();
        }

        private void UpdateLegend(IReadOnlyList<(string Name, int Amount, Color Color)> slices, int total) {
            while (_legend.GetChildCount() > slices.Count) {
                var extra = _legend.GetChild(_legend.GetChildCount() - 1);
                _legend.RemoveChild(extra);
                extra.Free();
            }

            for (int i = 0; i < slices.Count; i++) {
                var (name, amount, color) = slices[i];
                float pct = total > 0 ? 100f * amount / total : 0f;
                if (i < _legend.GetChildCount()) {
                    UpdateLegendRow((HBoxContainer)_legend.GetChild(i), name, amount, pct, color);
                }
                else {
                    _legend.AddChild(MakeLegendRow(name, amount, pct, color));
                }
            }
        }

        private static void UpdateLegendRow(HBoxContainer row, string name, int amount, float pct, Color color) {
            var swatch = (ColorRect)row.GetChild(0);
            var label = (Label)row.GetChild(1);
            var value = (Label)row.GetChild(2);
            swatch.Color = color;
            label.Text = name;
            value.Text = $"{amount} ({pct:0.#}%)";
        }

        private static Control MakeLegendRow(string name, int amount, float pct, Color color) {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var swatch = new ColorRect {
                Color = color,
                CustomMinimumSize = new Vector2(8, 8),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            };

            var label = new Label {
                Text = name,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            label.AddThemeFontSizeOverride("font_size", 10);
            label.AddThemeColorOverride("font_color", KitLibTheme.TextPrimary);

            var value = new Label {
                Text = $"{amount} ({pct:0.#}%)",
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            value.AddThemeFontSizeOverride("font_size", 10);
            value.AddThemeColorOverride("font_color", KitLibTheme.TextSecondary);

            row.AddChild(swatch);
            row.AddChild(label);
            row.AddChild(value);
            return row;
        }

        private static string FormatPieTooltip(
            string categoryHint,
            IReadOnlyList<(string Name, int Amount, Color Color)> slices,
            int total) {
            var sb = new System.Text.StringBuilder(128);
            sb.Append(categoryHint);
            sb.Append('\n');
            sb.Append(I18N.T("combatStats.sidebar.total", "Total {0}", total));
            foreach (var (name, amount, _) in slices) {
                if (amount <= 0)
                    continue;
                float pct = total > 0 ? 100f * amount / total : 0f;
                sb.Append('\n')
                    .Append(name)
                    .Append(' ')
                    .Append(amount)
                    .Append(" (")
                    .Append(pct.ToString("0.#"))
                    .Append("%)");
            }
            return sb.ToString().TrimEnd();
        }

        private static string CategoryHint(CombatPieCategory category) => category switch {
            CombatPieCategory.Overview => I18N.T("combatStats.pie.hint.overview",
                "Combat score by category: damage, block, debuffs, utility, etc."),
            CombatPieCategory.Cards => I18N.T("combatStats.pie.hint.cards",
                "Top cards by attributed contribution score."),
            CombatPieCategory.Offense => I18N.T("combatStats.pie.hint.offense",
                "Damage by card and power."),
            CombatPieCategory.Support => I18N.T("combatStats.pie.hint.support",
                "Block, utility, debuffs, buffs, potions, and synergy."),
            CombatPieCategory.Tank => I18N.T("combatStats.pie.hint.tank",
                "Damage taken by source."),
            _ => "",
        };
    }

    /// <summary>Donut pie chart rasterized to a texture (avoids missed <c>_Draw</c> during panel slide-in).</summary>
    private sealed partial class CombatStatsPieChart : Control {
        private readonly List<(string Name, int Amount, Color Color)> _slices = new();
        private readonly TextureRect _texture;
        private int _total = 1;

        public CombatStatsPieChart(string name) {
            Name = name;
            CustomMinimumSize = new Vector2(PieChartSize, PieChartSize);
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            ClipContents = false;

            _texture = new TextureRect {
                Name = "PieTexture",
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _texture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(_texture);
        }

        public override void _Ready() {
            ThemeManager.OnThemeChanged += OnThemeChanged;
            TreeExiting += OnTreeExiting;
            RefreshTexture();
        }

        private void OnTreeExiting() => ThemeManager.OnThemeChanged -= OnThemeChanged;

        private void OnThemeChanged() => RefreshTexture();

        public void SetSlices(IReadOnlyList<(string Name, int Amount, Color Color)> slices, int total) {
            _slices.Clear();
            _slices.AddRange(slices);
            _total = Math.Max(total, 1);
            RefreshTexture();
            Callable.From(RefreshTexture).CallDeferred();
        }

        public override void _ExitTree() {
            _slices.Clear();
            base._ExitTree();
        }

        internal void RefreshAfterOverlayOpen() {
            Callable.From(RefreshTexture).CallDeferred();
            var timer = GetTree()?.CreateTimer(0.9);
            if (timer != null)
                timer.Timeout += RefreshTexture;
        }

        private void RefreshTexture() {
            _texture.Texture = RasterizePie();
        }

        private ImageTexture? RasterizePie() {
            var image = Image.CreateEmpty(PieChartSize, PieChartSize, false, Image.Format.Rgba8);
            if (image == null)
                return null;

            var center = new Vector2(PieChartSize / 2f, PieChartSize / 2f);
            float radius = PieChartSize * 0.42f;
            float innerEmpty = radius * 0.38f;

            if (_slices.Count == 0 || _total <= 0) {
                FillDisk(image, center, innerEmpty, new Color(0.22f, 0.22f, 0.26f, 0.85f));
                StrokeRing(image, center, radius, KitLibTheme.Separator);
                return ImageTexture.CreateFromImage(image);
            }

            float start = -Mathf.Pi / 2f;
            foreach (var (_, amount, color) in _slices) {
                float sweep = amount / (float)_total * Mathf.Tau;
                FillWedge(image, center, radius, start, start + sweep, color);
                start += sweep;
            }

            FillDisk(image, center, radius * 0.52f, KitLibTheme.PanelBg);
            StrokeRing(image, center, radius, KitLibTheme.PanelBorder);
            return ImageTexture.CreateFromImage(image);
        }

        private static void FillDisk(Image image, Vector2 center, float radius, Color color) {
            int r = Mathf.CeilToInt(radius);
            int cx = Mathf.RoundToInt(center.X);
            int cy = Mathf.RoundToInt(center.Y);
            float r2 = radius * radius;
            for (int y = cy - r; y <= cy + r; y++) {
                if (y < 0 || y >= PieChartSize) continue;
                for (int x = cx - r; x <= cx + r; x++) {
                    if (x < 0 || x >= PieChartSize) continue;
                    float dx = x - center.X;
                    float dy = y - center.Y;
                    if (dx * dx + dy * dy <= r2)
                        image.SetPixel(x, y, color);
                }
            }
        }

        private static void FillWedge(Image image, Vector2 center, float radius, float fromRad, float toRad, Color color) {
            int r = Mathf.CeilToInt(radius);
            int cx = Mathf.RoundToInt(center.X);
            int cy = Mathf.RoundToInt(center.Y);
            float r2 = radius * radius;
            for (int y = cy - r; y <= cy + r; y++) {
                if (y < 0 || y >= PieChartSize) continue;
                for (int x = cx - r; x <= cx + r; x++) {
                    if (x < 0 || x >= PieChartSize) continue;
                    float dx = x - center.X;
                    float dy = y - center.Y;
                    if (dx * dx + dy * dy > r2)
                        continue;
                    if (AngleInSweep(Mathf.Atan2(dy, dx), fromRad, toRad))
                        image.SetPixel(x, y, color);
                }
            }
        }

        private static bool AngleInSweep(float angle, float fromRad, float toRad) {
            angle = NormalizeAngle(angle);
            fromRad = NormalizeAngle(fromRad);
            toRad = NormalizeAngle(toRad);
            if (fromRad <= toRad)
                return angle >= fromRad && angle <= toRad;
            return angle >= fromRad || angle <= toRad;
        }

        private static float NormalizeAngle(float angle) {
            while (angle < 0f) angle += Mathf.Tau;
            while (angle >= Mathf.Tau) angle -= Mathf.Tau;
            return angle;
        }

        private static void StrokeRing(Image image, Vector2 center, float radius, Color color) {
            int cx = Mathf.RoundToInt(center.X);
            int cy = Mathf.RoundToInt(center.Y);
            int ri = Mathf.RoundToInt(radius);
            float outer2 = (radius + 0.5f) * (radius + 0.5f);
            float inner2 = (radius - 0.5f) * (radius - 0.5f);
            for (int y = cy - ri - 1; y <= cy + ri + 1; y++) {
                if (y < 0 || y >= PieChartSize) continue;
                for (int x = cx - ri - 1; x <= cx + ri + 1; x++) {
                    if (x < 0 || x >= PieChartSize) continue;
                    float dx = x - center.X;
                    float dy = y - center.Y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 <= outer2 && d2 >= inner2)
                        image.SetPixel(x, y, color);
                }
            }
        }
    }
}
