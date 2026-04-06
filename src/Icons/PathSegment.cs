namespace DevMode.Icons;

public enum SegmentType { MoveTo, LineTo, CubicTo, QuadTo, ArcTo, Close }

public readonly record struct PathSegment(
    SegmentType Type,
    float X, float Y,                     // endpoint
    float C1X = 0, float C1Y = 0,        // control point 1
    float C2X = 0, float C2Y = 0,        // control point 2 / arc params
    float ArcRx = 0, float ArcRy = 0,
    float ArcRotation = 0,
    bool ArcLargeArc = false,
    bool ArcSweep = false
);
