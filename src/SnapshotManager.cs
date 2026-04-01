using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace DevMode;

internal static class SnapshotManager
{
    public const int SlotCount = 3;

    private static readonly string SnapshotDir = Path.Combine(
        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
        "snapshots");

    // ──────── Quick save (slot 0) ────────

    public static bool QuickSave() => SaveToSlot(0, "Quick Save");

    public static bool QuickLoad() => LoadFromSlot(0);

    public static bool HasQuickSnapshot => HasSlot(0);

    // ──────── Slot save / load ────────

    public static bool SaveToSlot(int slot, string name = "")
    {
        var rm = RunManager.Instance;
        var state = rm?.DebugOnlyGetState();
        if (state == null) return false;

        try
        {
            Directory.CreateDirectory(SnapshotDir);

            var save = rm!.ToSave(state.CurrentRoom);
            var json = SaveManager.ToJson(save);
            File.WriteAllText(SlotPath(slot), json);

            var meta = CaptureMetaFromState(state, name);
            File.WriteAllText(MetaPath(slot), JsonSerializer.Serialize(meta));

            MainFile.Logger.Info($"SnapshotManager: Saved to slot {slot}.");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SnapshotManager: Save slot {slot} failed: {ex.Message}");
            return false;
        }
    }

    public static bool LoadFromSlot(int slot)
    {
        try
        {
            var path = SlotPath(slot);
            if (!File.Exists(path))
            {
                MainFile.Logger.Warn($"SnapshotManager: Slot {slot} is empty.");
                return false;
            }

            var json = File.ReadAllText(path);
            var result = SaveManager.FromJson<SerializableRun>(json);
            if (result.SaveData == null)
            {
                MainFile.Logger.Warn($"SnapshotManager: Failed to deserialize slot {slot}.");
                return false;
            }

            return LoadFromSnapshot(result.SaveData);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SnapshotManager: Load slot {slot} failed: {ex.Message}");
            return false;
        }
    }

    public static bool HasSlot(int slot) => File.Exists(SlotPath(slot));

    public static SnapshotMeta? LoadMeta(int slot)
    {
        var path = MetaPath(slot);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<SnapshotMeta>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public static void RenameSlot(int slot, string name)
    {
        var meta = LoadMeta(slot) ?? new SnapshotMeta();
        meta.Name = name;
        try { File.WriteAllText(MetaPath(slot), JsonSerializer.Serialize(meta)); }
        catch (Exception ex) { MainFile.Logger.Warn($"SnapshotManager: Rename slot {slot} failed: {ex.Message}"); }
    }

    // ──────── Helpers ────────

    private static string SlotPath(int slot) => Path.Combine(SnapshotDir, $"slot{slot}.json");
    private static string MetaPath(int slot) => Path.Combine(SnapshotDir, $"slot{slot}_meta.json");

    private static SnapshotMeta CaptureMetaFromState(RunState state, string name)
    {
        var player = state.Players.FirstOrDefault();
        var meta = new SnapshotMeta
        {
            Name = name,
            SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            TotalFloor = state.TotalFloor,
            Gold = player?.Gold ?? 0,
            Hp = player?.Creature.CurrentHp ?? 0,
            MaxHp = player?.Creature.MaxHp ?? 0,
            CharacterId = player?.Character.Id.Entry ?? "",
        };

        if (player != null)
        {
            meta.CardTitles = player.Deck.Cards
                .Select(c => c.Title)
                .ToList();

            meta.RelicTitles = player.Relics
                .Select(r => r.Title.GetFormattedText())
                .ToList();
        }

        return meta;
    }

    // ──────── Internal load ────────

    private static bool LoadFromSnapshot(SerializableRun save)
    {
        try
        {
            if (RunManager.Instance == null)
            {
                MainFile.Logger.Warn("SnapshotManager: No RunManager instance.");
                return false;
            }

            TaskHelper.RunSafely(LoadFromSnapshotAsync(save));
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SnapshotManager: Load snapshot failed: {ex.Message}");
            return false;
        }
    }

    private static async Task LoadFromSnapshotAsync(SerializableRun save)
    {
        var game = NGame.Instance!;
        var rm = RunManager.Instance;

        await game.Transition.FadeOut();

        if (rm.IsInProgress)
            rm.CleanUp();

        DevModeState.InDevRun = true;

        var state = RunState.FromSerializable(save);
        rm.SetUpSavedSinglePlayer(state, save);

        var prop = AccessTools.Property(typeof(RunManager), "ShouldSave");
        prop?.SetValue(rm, false);

        game.ReactionContainer.InitializeNetworking(
            new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());

        await game.LoadRun(state, save.PreFinishedRoom);
        await game.Transition.FadeIn();

        MainFile.Logger.Info("SnapshotManager: Snapshot loaded successfully.");
    }
}
