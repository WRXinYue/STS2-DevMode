using Godot;

namespace DevMode.Icons;

/// <summary>
/// Typed accessor for Material Design Icons.
/// Usage:  <c>button.Icon = MdiIcon.Play.Texture();</c>
///         <c>button.Icon = MdiIcon.Get("account-check", 32, Colors.Red);</c>
///
/// Icon names use PascalCase matching the kebab-case originals:
///   "account-check" → MdiIcon.AccountCheck
///   "skip-previous" → MdiIcon.SkipPrevious
///
/// Only icons referenced via MdiIcon.XxxYyy in source code are bundled
/// at build time (tree-shaking via Shake-Icons.ps1).
/// </summary>
public readonly struct MdiIcon
{
    public string Name { get; }

    private MdiIcon(string kebabName) => Name = kebabName;

    /// <summary>Get the icon as a Godot <see cref="ImageTexture"/>.</summary>
    /// <param name="size">Pixel size (square). Default 24.</param>
    /// <param name="color">Tint colour. Default white.</param>
    public ImageTexture? Texture(int size = 24, Color? color = null)
        => IconifyAdapter.Get(Name, size, color);

    /// <summary>Shorthand: get icon at specific size with default colour.</summary>
    public ImageTexture? this[int size]
        => IconifyAdapter.Get(Name, size);

    /// <summary>Check if this icon is available in the bundled set.</summary>
    public bool IsAvailable => IconifyAdapter.Has(Name);

    // ── Static factory ──────────────────────────────────────────────

    /// <summary>Get any icon by kebab-case name.</summary>
    public static ImageTexture? Get(string kebabName, int size = 24, Color? color = null)
        => IconifyAdapter.Get(kebabName, size, color);

    // ── Common icons (add more as needed — tree-shaker picks them up) ──

    // Navigation / Player controls
    public static readonly MdiIcon Play              = new("play");
    public static readonly MdiIcon Pause             = new("pause");
    public static readonly MdiIcon Stop              = new("stop");
    public static readonly MdiIcon SkipNext          = new("skip-next");
    public static readonly MdiIcon SkipPrevious      = new("skip-previous");
    public static readonly MdiIcon FastForward       = new("fast-forward");
    public static readonly MdiIcon Rewind            = new("rewind");

    // Actions
    public static readonly MdiIcon Plus              = new("plus");
    public static readonly MdiIcon Minus             = new("minus");
    public static readonly MdiIcon Close             = new("close");
    public static readonly MdiIcon Check             = new("check");
    public static readonly MdiIcon Delete            = new("delete");
    public static readonly MdiIcon Refresh           = new("refresh");
    public static readonly MdiIcon ContentSave       = new("content-save");
    public static readonly MdiIcon ContentCopy       = new("content-copy");
    public static readonly MdiIcon Pencil            = new("pencil");
    public static readonly MdiIcon Magnify           = new("magnify");
    public static readonly MdiIcon Undo              = new("undo");
    public static readonly MdiIcon Redo              = new("redo");

    // UI / Layout
    public static readonly MdiIcon Menu              = new("menu");
    public static readonly MdiIcon DotsVertical      = new("dots-vertical");
    public static readonly MdiIcon DotsHorizontal    = new("dots-horizontal");
    public static readonly MdiIcon ChevronLeft       = new("chevron-left");
    public static readonly MdiIcon ChevronRight      = new("chevron-right");
    public static readonly MdiIcon ChevronUp         = new("chevron-up");
    public static readonly MdiIcon ChevronDown       = new("chevron-down");
    public static readonly MdiIcon ArrowLeft         = new("arrow-left");
    public static readonly MdiIcon ArrowRight        = new("arrow-right");
    public static readonly MdiIcon Cog               = new("cog");
    public static readonly MdiIcon CogOutline        = new("cog-outline");
    public static readonly MdiIcon InformationOutline = new("information-outline");
    public static readonly MdiIcon AlertCircleOutline = new("alert-circle-outline");

    // Game-related
    public static readonly MdiIcon Sword             = new("sword");
    public static readonly MdiIcon Shield            = new("shield");
    public static readonly MdiIcon Heart             = new("heart");
    public static readonly MdiIcon HeartOutline      = new("heart-outline");
    public static readonly MdiIcon Star              = new("star");
    public static readonly MdiIcon StarOutline       = new("star-outline");
    public static readonly MdiIcon Flash             = new("flash");
    public static readonly MdiIcon Skull             = new("skull");
    public static readonly MdiIcon Map               = new("map");
    public static readonly MdiIcon Cards             = new("cards");
    public static readonly MdiIcon CardsOutline      = new("cards-outline");
    public static readonly MdiIcon Bottle            = new("bottle-tonic");
    public static readonly MdiIcon Robot             = new("robot");
    public static readonly MdiIcon Bug               = new("bug");
    public static readonly MdiIcon Console           = new("console");
    public static readonly MdiIcon Eye               = new("eye");
    public static readonly MdiIcon EyeOff            = new("eye-off");
    public static readonly MdiIcon Lock              = new("lock");
    public static readonly MdiIcon LockOpen          = new("lock-open");
    public static readonly MdiIcon Download          = new("download");
    public static readonly MdiIcon Upload            = new("upload");
    public static readonly MdiIcon FolderOpen        = new("folder-open");
    public static readonly MdiIcon FileDocument      = new("file-document");

    // DevPanel-specific
    public static readonly MdiIcon Diamond            = new("diamond-stone");
    public static readonly MdiIcon Potion             = new("flask-outline");
    public static readonly MdiIcon CalendarStar       = new("calendar-star");
    public static readonly MdiIcon BookOpen           = new("book-open-variant");
    public static readonly MdiIcon ContentSaveOutline = new("content-save-outline");
    public static readonly MdiIcon FolderOpenOutline  = new("folder-open-outline");
    public static readonly MdiIcon SpeedometerMedium  = new("speedometer-medium");
    public static readonly MdiIcon AnimationPlay      = new("animation-play");
    public static readonly MdiIcon SkullCrossbones    = new("skull-crossbones");
    public static readonly MdiIcon Snowflake          = new("snowflake");
    public static readonly MdiIcon TreasureChest      = new("treasure-chest");
    public static readonly MdiIcon MapMarker          = new("map-marker");
    public static readonly MdiIcon ShieldSword        = new("shield-sword");
    public static readonly MdiIcon AccountGroup       = new("account-group");
    public static readonly MdiIcon PlusCircle         = new("plus-circle");
    public static readonly MdiIcon CloseCircle        = new("close-circle");
}
