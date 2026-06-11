namespace KitLib.Abstractions.Modding;

public readonly record struct KitLibCompatIssue(
    string ModId,
    string DisplayName,
    KitLibCompatFlags Flags,
    KitLibCompatResult Result);
