using System.Collections.Generic;
using System.Globalization;

namespace DevMode.Icons;

/// <summary>
/// Parses an SVG path "d" attribute string into <see cref="PathSegment"/> list.
/// Supports: M/m, L/l, H/h, V/v, C/c, S/s, Q/q, T/t, A/a, Z/z.
/// </summary>
public static partial class SvgPathExtractor
{
    public static List<PathSegment> ParsePath(string d)
    {
        var segs = new List<PathSegment>();
        var tokens = Tokenize(d);
        int i = 0;
        float cx = 0, cy = 0;   // current point
        float sx = 0, sy = 0;   // subpath start
        float lcx = 0, lcy = 0; // last control point (for S/T)
        char lastCmd = 'M';

        while (i < tokens.Count)
        {
            char cmd;
            if (char.IsLetter(tokens[i][0]) && tokens[i].Length == 1)
            {
                cmd = tokens[i][0];
                i++;
            }
            else
            {
                // Implicit repeat of last command (L after M)
                cmd = lastCmd == 'M' ? 'L' : lastCmd == 'm' ? 'l' : lastCmd;
            }

            bool rel = char.IsLower(cmd);
            char upper = char.ToUpper(cmd);

            switch (upper)
            {
                case 'M':
                {
                    float x = Num(tokens, ref i) + (rel ? cx : 0);
                    float y = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.MoveTo, x, y));
                    cx = sx = x; cy = sy = y;
                    // Subsequent coords after M are implicit LineTo
                    lastCmd = rel ? 'l' : 'L';
                    continue;
                }
                case 'L':
                {
                    float x = Num(tokens, ref i) + (rel ? cx : 0);
                    float y = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.LineTo, x, y));
                    cx = x; cy = y;
                    break;
                }
                case 'H':
                {
                    float x = Num(tokens, ref i) + (rel ? cx : 0);
                    segs.Add(new PathSegment(SegmentType.LineTo, x, cy));
                    cx = x;
                    break;
                }
                case 'V':
                {
                    float y = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.LineTo, cx, y));
                    cy = y;
                    break;
                }
                case 'C':
                {
                    float c1x = Num(tokens, ref i) + (rel ? cx : 0);
                    float c1y = Num(tokens, ref i) + (rel ? cy : 0);
                    float c2x = Num(tokens, ref i) + (rel ? cx : 0);
                    float c2y = Num(tokens, ref i) + (rel ? cy : 0);
                    float x   = Num(tokens, ref i) + (rel ? cx : 0);
                    float y   = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.CubicTo, x, y, c1x, c1y, c2x, c2y));
                    lcx = c2x; lcy = c2y;
                    cx = x; cy = y;
                    break;
                }
                case 'S':
                {
                    // Smooth cubic: reflect last control point
                    float c1x = 2 * cx - lcx;
                    float c1y = 2 * cy - lcy;
                    float c2x = Num(tokens, ref i) + (rel ? cx : 0);
                    float c2y = Num(tokens, ref i) + (rel ? cy : 0);
                    float x   = Num(tokens, ref i) + (rel ? cx : 0);
                    float y   = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.CubicTo, x, y, c1x, c1y, c2x, c2y));
                    lcx = c2x; lcy = c2y;
                    cx = x; cy = y;
                    break;
                }
                case 'Q':
                {
                    float c1x = Num(tokens, ref i) + (rel ? cx : 0);
                    float c1y = Num(tokens, ref i) + (rel ? cy : 0);
                    float x   = Num(tokens, ref i) + (rel ? cx : 0);
                    float y   = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.QuadTo, x, y, c1x, c1y));
                    lcx = c1x; lcy = c1y;
                    cx = x; cy = y;
                    break;
                }
                case 'T':
                {
                    float c1x = 2 * cx - lcx;
                    float c1y = 2 * cy - lcy;
                    float x = Num(tokens, ref i) + (rel ? cx : 0);
                    float y = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.QuadTo, x, y, c1x, c1y));
                    lcx = c1x; lcy = c1y;
                    cx = x; cy = y;
                    break;
                }
                case 'A':
                {
                    float rx   = Num(tokens, ref i);
                    float ry   = Num(tokens, ref i);
                    float rot  = Num(tokens, ref i);
                    bool large = Num(tokens, ref i) != 0;
                    bool sweep = Num(tokens, ref i) != 0;
                    float x    = Num(tokens, ref i) + (rel ? cx : 0);
                    float y    = Num(tokens, ref i) + (rel ? cy : 0);
                    segs.Add(new PathSegment(SegmentType.ArcTo, x, y,
                        ArcRx: rx, ArcRy: ry, ArcRotation: rot,
                        ArcLargeArc: large, ArcSweep: sweep));
                    cx = x; cy = y;
                    break;
                }
                case 'Z':
                {
                    segs.Add(new PathSegment(SegmentType.Close, sx, sy));
                    cx = sx; cy = sy;
                    break;
                }
            }
            lastCmd = cmd;
        }
        return segs;
    }

    private static float Num(List<string> tokens, ref int i)
    {
        if (i >= tokens.Count) return 0;
        var s = tokens[i++];
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    /// <summary>Tokenize SVG path d-string into commands and numbers.</summary>
    private static List<string> Tokenize(string d)
    {
        var tokens = new List<string>();
        int n = d.Length;
        int i = 0;

        while (i < n)
        {
            char c = d[i];

            // Skip whitespace and commas
            if (c == ' ' || c == ',' || c == '\t' || c == '\n' || c == '\r')
            {
                i++;
                continue;
            }

            // Command letter
            if (char.IsLetter(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            // Number (including negative, decimal, scientific)
            int start = i;
            if (c == '-' || c == '+') i++;
            bool hasDot = false;
            while (i < n)
            {
                c = d[i];
                if (c == '.' && !hasDot) { hasDot = true; i++; }
                else if (char.IsDigit(c)) i++;
                else if ((c == 'e' || c == 'E') && i > start)
                {
                    i++;
                    if (i < n && (d[i] == '+' || d[i] == '-')) i++;
                }
                else break;
            }

            if (i > start)
                tokens.Add(d[start..i]);
            else
                i++; // skip unknown char
        }
        return tokens;
    }
}
