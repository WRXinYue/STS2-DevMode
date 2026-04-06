using System;
using System.Collections.Generic;
using Godot;

namespace DevMode.Icons;

/// <summary>
/// Rasterises parsed SVG path segments into a Godot <see cref="Image"/>
/// using edge-based scanline fill with 4× supersampling for anti-aliasing.
/// </summary>
public static class SvgRasterizer
{
    private const int AA = 4; // supersampling factor

    public static Image Rasterize(List<List<PathSegment>> paths, int viewBox, int size, Color color)
    {
        int ssSize = size * AA;
        float scale = ssSize / (float)viewBox;

        // Flatten all paths to polylines
        var edges = new List<(float y0, float y1, float x0, float dx)>();
        foreach (var path in paths)
            CollectEdges(path, scale, edges);

        // Scanline fill into supersampled buffer
        var buf = new float[ssSize * ssSize];
        FillEdges(edges, ssSize, buf);

        // Downsample to final size
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        float invAA2 = 1f / (AA * AA);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float sum = 0;
            for (int sy = 0; sy < AA; sy++)
            for (int sx = 0; sx < AA; sx++)
                sum += buf[(y * AA + sy) * ssSize + (x * AA + sx)];

            float alpha = sum * invAA2;
            if (alpha > 0.001f)
                img.SetPixel(x, y, new Color(color.R, color.G, color.B, color.A * Math.Min(alpha, 1f)));
        }

        return img;
    }

    private static void CollectEdges(List<PathSegment> segs, float scale,
        List<(float y0, float y1, float x0, float dx)> edges)
    {
        float cx = 0, cy = 0;

        foreach (var seg in segs)
        {
            switch (seg.Type)
            {
                case SegmentType.MoveTo:
                    cx = seg.X * scale;
                    cy = seg.Y * scale;
                    break;

                case SegmentType.LineTo:
                case SegmentType.Close:
                {
                    float nx = seg.X * scale, ny = seg.Y * scale;
                    AddEdge(edges, cx, cy, nx, ny);
                    cx = nx; cy = ny;
                    break;
                }

                case SegmentType.CubicTo:
                {
                    float nx = seg.X * scale, ny = seg.Y * scale;
                    float c1x = seg.C1X * scale, c1y = seg.C1Y * scale;
                    float c2x = seg.C2X * scale, c2y = seg.C2Y * scale;
                    FlattenCubic(edges, cx, cy, c1x, c1y, c2x, c2y, nx, ny);
                    cx = nx; cy = ny;
                    break;
                }

                case SegmentType.QuadTo:
                {
                    float nx = seg.X * scale, ny = seg.Y * scale;
                    float qx = seg.C1X * scale, qy = seg.C1Y * scale;
                    // Convert quad to cubic
                    float c1x = cx + 2f / 3f * (qx - cx);
                    float c1y = cy + 2f / 3f * (qy - cy);
                    float c2x = nx + 2f / 3f * (qx - nx);
                    float c2y = ny + 2f / 3f * (qy - ny);
                    FlattenCubic(edges, cx, cy, c1x, c1y, c2x, c2y, nx, ny);
                    cx = nx; cy = ny;
                    break;
                }

                case SegmentType.ArcTo:
                {
                    float nx = seg.X * scale, ny = seg.Y * scale;
                    FlattenArc(edges, cx, cy,
                        seg.ArcRx * scale, seg.ArcRy * scale,
                        seg.ArcRotation, seg.ArcLargeArc, seg.ArcSweep,
                        nx, ny);
                    cx = nx; cy = ny;
                    break;
                }
            }
        }
    }

    private static void AddEdge(List<(float y0, float y1, float x0, float dx)> edges,
        float x0, float y0, float x1, float y1)
    {
        if (Math.Abs(y1 - y0) < 0.001f) return; // horizontal — skip

        if (y0 > y1)
        {
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
        }

        float dx = (x1 - x0) / (y1 - y0);
        edges.Add((y0, y1, x0, dx));
    }

    private static void FillEdges(List<(float y0, float y1, float x0, float dx)> edges,
        int size, float[] buf)
    {
        // Sort edges by y0
        edges.Sort((a, b) => a.y0.CompareTo(b.y0));

        for (int y = 0; y < size; y++)
        {
            float scanY = y + 0.5f;
            var xIntersections = new List<float>();

            foreach (var (ey0, ey1, ex0, edx) in edges)
            {
                if (scanY < ey0 || scanY >= ey1) continue;
                float x = ex0 + (scanY - ey0) * edx;
                xIntersections.Add(x);
            }

            xIntersections.Sort();

            // Fill between pairs (even-odd rule)
            for (int j = 0; j + 1 < xIntersections.Count; j += 2)
            {
                int xStart = Math.Max(0, (int)Math.Ceiling(xIntersections[j]));
                int xEnd   = Math.Min(size - 1, (int)Math.Floor(xIntersections[j + 1]));
                for (int x = xStart; x <= xEnd; x++)
                    buf[y * size + x] = 1f;
            }
        }
    }

    private static void FlattenCubic(List<(float y0, float y1, float x0, float dx)> edges,
        float x0, float y0, float c1x, float c1y, float c2x, float c2y, float x3, float y3,
        int depth = 0)
    {
        // Adaptive subdivision
        if (depth < 8)
        {
            float mx = (x0 + 3 * c1x + 3 * c2x + x3) / 8f;
            float my = (y0 + 3 * c1y + 3 * c2y + y3) / 8f;
            float midX = (x0 + x3) / 2f;
            float midY = (y0 + y3) / 2f;
            float dist = Math.Abs(mx - midX) + Math.Abs(my - midY);

            if (dist > 0.25f)
            {
                // de Casteljau split at t=0.5
                float m01x = (x0 + c1x) / 2f, m01y = (y0 + c1y) / 2f;
                float m12x = (c1x + c2x) / 2f, m12y = (c1y + c2y) / 2f;
                float m23x = (c2x + x3) / 2f, m23y = (c2y + y3) / 2f;
                float m012x = (m01x + m12x) / 2f, m012y = (m01y + m12y) / 2f;
                float m123x = (m12x + m23x) / 2f, m123y = (m12y + m23y) / 2f;
                float mx2 = (m012x + m123x) / 2f, my2 = (m012y + m123y) / 2f;

                FlattenCubic(edges, x0, y0, m01x, m01y, m012x, m012y, mx2, my2, depth + 1);
                FlattenCubic(edges, mx2, my2, m123x, m123y, m23x, m23y, x3, y3, depth + 1);
                return;
            }
        }

        AddEdge(edges, x0, y0, x3, y3);
    }

    private static void FlattenArc(List<(float y0, float y1, float x0, float dx)> edges,
        float cx, float cy, float rx, float ry, float rotation,
        bool largeArc, bool sweep, float nx, float ny)
    {
        // Approximate arc with line segments
        if (rx < 0.001f || ry < 0.001f)
        {
            AddEdge(edges, cx, cy, nx, ny);
            return;
        }

        // Endpoint to center parameterization
        float cosR = MathF.Cos(rotation * MathF.PI / 180f);
        float sinR = MathF.Sin(rotation * MathF.PI / 180f);

        float dx = (cx - nx) / 2f;
        float dy = (cy - ny) / 2f;
        float x1p = cosR * dx + sinR * dy;
        float y1p = -sinR * dx + cosR * dy;

        float rx2 = rx * rx, ry2 = ry * ry;
        float x1p2 = x1p * x1p, y1p2 = y1p * y1p;

        // Correct radii
        float lambda = x1p2 / rx2 + y1p2 / ry2;
        if (lambda > 1f)
        {
            float sq = MathF.Sqrt(lambda);
            rx *= sq; ry *= sq;
            rx2 = rx * rx; ry2 = ry * ry;
        }

        float num = rx2 * ry2 - rx2 * y1p2 - ry2 * x1p2;
        float den = rx2 * y1p2 + ry2 * x1p2;
        float sq2 = Math.Max(0, num / den);
        float coef = MathF.Sqrt(sq2) * (largeArc == sweep ? -1 : 1);

        float cxp = coef * rx * y1p / ry;
        float cyp = coef * -ry * x1p / rx;

        float midX = (cx + nx) / 2f;
        float midY = (cy + ny) / 2f;
        float ccx = cosR * cxp - sinR * cyp + midX;
        float ccy = sinR * cxp + cosR * cyp + midY;

        float theta1 = AngleOf((x1p - cxp) / rx, (y1p - cyp) / ry);
        float dTheta = AngleOf((-x1p - cxp) / rx, (-y1p - cyp) / ry) - theta1;

        if (sweep && dTheta < 0) dTheta += 2 * MathF.PI;
        if (!sweep && dTheta > 0) dTheta -= 2 * MathF.PI;

        int steps = Math.Max(4, (int)(Math.Abs(dTheta) / (MathF.PI / 16)));
        float prevX = cx, prevY = cy;

        for (int i = 1; i <= steps; i++)
        {
            float t = theta1 + dTheta * i / steps;
            float px = cosR * rx * MathF.Cos(t) - sinR * ry * MathF.Sin(t) + ccx;
            float py = sinR * rx * MathF.Cos(t) + cosR * ry * MathF.Sin(t) + ccy;
            AddEdge(edges, prevX, prevY, px, py);
            prevX = px; prevY = py;
        }
    }

    private static float AngleOf(float x, float y) => MathF.Atan2(y, x);
}
