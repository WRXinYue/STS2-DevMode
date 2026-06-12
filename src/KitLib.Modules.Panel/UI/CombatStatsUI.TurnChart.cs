using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KitLib.UI;

internal static partial class CombatStatsUI {
    private const string TurnChartNodeName = "turn.damage.chart";

    private static List<(int Turn, int Amount)> BuildTurnSeries(
        IEnumerable<KeyValuePair<string, int>> data,
        int maxTurn,
        int? turnLimit) {
        int endTurn = maxTurn > 0 ? maxTurn : 1;
        if (turnLimit.HasValue)
            endTurn = Math.Min(endTurn, turnLimit.Value);

        var lookup = data.ToDictionary(
            kv => int.TryParse(kv.Key, out int t) ? t : 0,
            kv => kv.Value);

        var series = new List<(int Turn, int Amount)>(endTurn);
        for (int turn = 1; turn <= endTurn; turn++)
            series.Add((turn, lookup.GetValueOrDefault(turn)));
        return series;
    }

    private static int SeriesPeak(IReadOnlyList<(int Turn, int Amount)> series) {
        int peak = 0;
        foreach (var (_, amount) in series)
            peak = Math.Max(peak, amount);
        return Math.Max(peak, 1);
    }

    private static TurnTimeSeriesChart MakeTurnDamageChart(
        IEnumerable<KeyValuePair<string, int>> data,
        int maxTurn,
        bool animate,
        int? turnLimit = null) {
        var series = BuildTurnSeries(data, maxTurn, turnLimit);
        var chart = new TurnTimeSeriesChart(TurnChartNodeName);
        chart.SetData(series, SeriesPeak(series), animate);
        return chart;
    }

    private static void RefreshTurnChart(
        Node root,
        Dictionary<string, int> data,
        int maxTurn,
        bool animate,
        int? turnLimit = null) {
        if (root.FindChild(TurnChartNodeName, recursive: true, owned: false) is not TurnTimeSeriesChart chart)
            return;
        var series = BuildTurnSeries(data, maxTurn, turnLimit);
        chart.SetData(series, SeriesPeak(series), animate);
    }

    /// <summary>Time-series chart: X = turn, Y = damage (line + area).</summary>
    private sealed partial class TurnTimeSeriesChart : VBoxContainer {
        private const float YAxisWidth = 28f;
        private const float PlotHeight = 128f;

        private readonly Label _yMaxLabel;
        private readonly Label _yMidLabel;
        private readonly TurnSeriesCanvas _canvas;
        private readonly HBoxContainer _xLabels;

        public TurnTimeSeriesChart(string name) {
            Name = name;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            CustomMinimumSize = new Vector2(0, PlotHeight + 22);
            AddThemeConstantOverride("separation", 4);

            var plotRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            plotRow.AddThemeConstantOverride("separation", 4);

            var yAxis = new VBoxContainer {
                CustomMinimumSize = new Vector2(YAxisWidth, PlotHeight),
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };

            _yMaxLabel = MakeAxisLabel("0");
            var ySpacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
            _yMidLabel = MakeAxisLabel("0");
            var yZero = MakeAxisLabel("0");

            yAxis.AddChild(_yMaxLabel);
            yAxis.AddChild(ySpacer);
            yAxis.AddChild(_yMidLabel);
            yAxis.AddChild(yZero);

            _canvas = new TurnSeriesCanvas {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                CustomMinimumSize = new Vector2(0, PlotHeight),
            };

            plotRow.AddChild(yAxis);
            plotRow.AddChild(_canvas);
            AddChild(plotRow);

            var xRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            xRow.AddChild(new Control { CustomMinimumSize = new Vector2(YAxisWidth + 4, 0) });
            _xLabels = new HBoxContainer {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _xLabels.AddThemeConstantOverride("separation", 0);
            xRow.AddChild(_xLabels);
            AddChild(xRow);
        }

        public void SetData(IReadOnlyList<(int Turn, int Amount)> series, int scaleMax, bool animate) {
            int max = Math.Max(scaleMax, 1);
            _yMaxLabel.Text = max.ToString();
            _yMidLabel.Text = (max / 2).ToString();
            RebuildXLabels(series);
            _canvas.SetSeries(series, max, animate);
        }

        internal void RefreshAfterOverlayOpen() => _canvas.RefreshAfterOverlayOpen();

        private void RebuildXLabels(IReadOnlyList<(int Turn, int Amount)> series) {
            while (_xLabels.GetChildCount() > series.Count) {
                var extra = _xLabels.GetChild(_xLabels.GetChildCount() - 1);
                _xLabels.RemoveChild(extra);
                extra.Free();
            }

            for (int i = 0; i < series.Count; i++) {
                var (turn, _) = series[i];
                Label label;
                if (i < _xLabels.GetChildCount()) {
                    label = (Label)_xLabels.GetChild(i);
                }
                else {
                    label = new Label {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    };
                    label.AddThemeFontSizeOverride("font_size", 9);
                    label.AddThemeColorOverride("font_color", KitLibTheme.Subtle);
                    _xLabels.AddChild(label);
                }
                label.Text = turn.ToString();
            }
        }

        private static Label MakeAxisLabel(string text) {
            var label = new Label {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            label.AddThemeFontSizeOverride("font_size", 9);
            label.AddThemeColorOverride("font_color", KitLibTheme.Subtle);
            return label;
        }
    }

    /// <summary>Rasterized plot (avoids missed <c>_Draw</c> during panel slide-in).</summary>
    private sealed partial class TurnSeriesCanvas : Control {
        private const int RasterMinWidth = 480;
        private const int RasterHeight = 128;

        private readonly TextureRect _texture;
        private IReadOnlyList<(int Turn, int Amount)> _series = Array.Empty<(int, int)>();
        private int _scaleMax = 1;
        private float _anim = 1f;
        private Tween? _tween;

        public TurnSeriesCanvas() {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            ClipContents = false;

            _texture = new TextureRect {
                Name = "TurnPlotTexture",
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _texture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(_texture);
        }

        public override void _Ready() {
            ThemeManager.OnThemeChanged += OnThemeChanged;
            TreeExiting += OnTreeExiting;
            Resized += OnResized;
            RefreshTexture();
        }

        private void OnTreeExiting() => ThemeManager.OnThemeChanged -= OnThemeChanged;

        private void OnThemeChanged() => RefreshTexture();

        private void OnResized() => RefreshTexture();

        public override void _ExitTree() {
            _tween?.Kill();
            base._ExitTree();
        }

        public void SetSeries(IReadOnlyList<(int Turn, int Amount)> series, int scaleMax, bool animate) {
            _series = series;
            _scaleMax = Math.Max(scaleMax, 1);

            if (!animate || _anim >= 0.999f) {
                _tween?.Kill();
                _anim = 1f;
                RefreshTexture();
                Callable.From(RefreshTexture).CallDeferred();
                return;
            }

            _tween?.Kill();
            _anim = 0f;
            _tween = CreateTween();
            _tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            _tween.TweenMethod(Callable.From((float t) => {
                _anim = t;
                if (IsInsideTree())
                    RefreshTexture();
            }), 0f, 1f, BarAnimDuration);
            _tween.Finished += () => {
                _anim = 1f;
                RefreshTexture();
            };
        }

        internal void RefreshAfterOverlayOpen() {
            Callable.From(RefreshTexture).CallDeferred();
            var timer = GetTree()?.CreateTimer(0.9);
            if (timer != null)
                timer.Timeout += RefreshTexture;
        }

        private void RefreshTexture() {
            _texture.Texture = RasterizePlot();
        }

        private ImageTexture? RasterizePlot() {
            int w = Mathf.Max(RasterMinWidth, Mathf.RoundToInt(Mathf.Max(Size.X, CustomMinimumSize.X)));
            int h = RasterHeight;
            var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
            if (image == null)
                return null;

            if (_series.Count == 0)
                return ImageTexture.CreateFromImage(image);

            var plot = new Rect2(0f, 0f, w, h);
            var gridColor = new Color(KitLibTheme.PanelBorder.R, KitLibTheme.PanelBorder.G,
                KitLibTheme.PanelBorder.B, 0.28f);
            for (int i = 1; i <= 2; i++) {
                float y = plot.Position.Y + plot.Size.Y * i / 3f;
                StrokeLine(image, new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), gridColor, 1f);
            }

            StrokeLine(image, new Vector2(plot.Position.X, plot.End.Y), plot.End, gridColor, 1f);

            var points = new Vector2[_series.Count];
            for (int i = 0; i < _series.Count; i++)
                points[i] = new Vector2(XForIndex(i, plot), YForAmount(_series[i].Amount, plot));

            var areaColor = new Color(KitLibTheme.Accent.R, KitLibTheme.Accent.G, KitLibTheme.Accent.B, 0.14f);
            FillAreaUnderLine(image, points, plot.End.Y, areaColor);

            var lineColor = KitLibTheme.Accent;
            for (int i = 1; i < points.Length; i++)
                StrokeLine(image, points[i - 1], points[i], lineColor, 2f);

            foreach (var p in points) {
                FillDisk(image, p, 3f, lineColor);
                FillDisk(image, p, 1.5f, KitLibTheme.TextPrimary);
            }

            return ImageTexture.CreateFromImage(image);
        }

        private static void FillAreaUnderLine(Image image, Vector2[] points, float baselineY, Color color) {
            if (points.Length == 0)
                return;

            int w = image.GetWidth();
            int baseY = Mathf.Clamp(Mathf.RoundToInt(baselineY), 0, image.GetHeight() - 1);

            if (points.Length == 1) {
                int x = Mathf.Clamp(Mathf.RoundToInt(points[0].X), 0, w - 1);
                int topY = Mathf.Clamp(Mathf.RoundToInt(points[0].Y), 0, baseY);
                for (int y = topY; y <= baseY; y++)
                    image.SetPixel(x, y, color);
                return;
            }

            for (int x = 0; x < w; x++) {
                float topY = InterpolateLineY(x, points);
                int yStart = Mathf.Clamp(Mathf.RoundToInt(topY), 0, baseY);
                for (int y = yStart; y <= baseY; y++)
                    image.SetPixel(x, y, color);
            }
        }

        private static float InterpolateLineY(float x, Vector2[] points) {
            for (int i = 1; i < points.Length; i++) {
                float x0 = points[i - 1].X;
                float x1 = points[i].X;
                if (x < x0 && i > 1)
                    continue;
                if (x > x1 && i < points.Length - 1)
                    continue;
                if (Mathf.IsEqualApprox(x0, x1))
                    return points[i].Y;
                if (x >= x0 && x <= x1) {
                    float t = (x - x0) / (x1 - x0);
                    return Mathf.Lerp(points[i - 1].Y, points[i].Y, t);
                }
            }

            if (x <= points[0].X)
                return points[0].Y;
            return points[^1].Y;
        }

        private static void StrokeLine(Image image, Vector2 from, Vector2 to, Color color, float thickness) {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float len = Mathf.Max(Mathf.Sqrt(dx * dx + dy * dy), 1f);
            int steps = Mathf.CeilToInt(len * 2f);
            float radius = Mathf.Max(thickness * 0.5f, 0.5f);
            for (int i = 0; i <= steps; i++) {
                float t = i / (float)steps;
                FillDisk(image, from.Lerp(to, t), radius, color);
            }
        }

        private static void FillDisk(Image image, Vector2 center, float radius, Color color) {
            int w = image.GetWidth();
            int h = image.GetHeight();
            int r = Mathf.CeilToInt(radius);
            int cx = Mathf.RoundToInt(center.X);
            int cy = Mathf.RoundToInt(center.Y);
            float r2 = radius * radius;
            for (int y = cy - r; y <= cy + r; y++) {
                if (y < 0 || y >= h) continue;
                for (int x = cx - r; x <= cx + r; x++) {
                    if (x < 0 || x >= w) continue;
                    float dx = x - center.X;
                    float dy = y - center.Y;
                    if (dx * dx + dy * dy <= r2)
                        image.SetPixel(x, y, color);
                }
            }
        }

        private float XForIndex(int index, Rect2 plot) {
            if (_series.Count <= 1)
                return plot.Position.X + plot.Size.X * 0.5f;
            return plot.Position.X + index / (float)(_series.Count - 1) * plot.Size.X;
        }

        private float YForAmount(int amount, Rect2 plot) {
            float frac = _scaleMax > 0 ? Math.Clamp((float)amount / _scaleMax, 0f, 1f) : 0f;
            return plot.End.Y - frac * _anim * plot.Size.Y;
        }
    }
}
