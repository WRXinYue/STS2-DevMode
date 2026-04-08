using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace DevMode;

internal static class SaveSlotManager
{
    private static readonly string SnapshotDir = Path.Combine(
        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
        "snapshots");

    private static readonly Regex SlotMetaPattern = new(@"^slot(\d+)_meta\.json$", RegexOptions.Compiled);

    // ──────── Slot discovery ────────

    /// <summary>Returns sorted list of all slot IDs that have save data on disk.</summary>
    public static List<int> GetAllSlotIds()
    {
        if (!Directory.Exists(SnapshotDir)) return new List<int>();

        return Directory.GetFiles(SnapshotDir, "slot*_meta.json")
            .Select(f => SlotMetaPattern.Match(Path.GetFileName(f)))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .OrderBy(id => id)
            .ToList();
    }

    /// <summary>Returns the next unused slot ID (always >= 1).</summary>
    public static int NextSlotId()
    {
        var ids = GetAllSlotIds();
        return ids.Count == 0 ? 1 : ids.Max() + 1;
    }

    // ──────── Quick save (slot 0, convenience for hotkey/console) ────────

    public static bool QuickSave() => SaveToSlot(0);

    public static bool QuickLoad() => LoadFromSlot(0);

    public static bool HasQuickSnapshot => HasSlot(0);

    // ──────── Slot save / load / delete ────────

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

            MainFile.Logger.Info($"SaveSlotManager: Saved to slot {slot}.");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SaveSlotManager: Save slot {slot} failed: {ex.Message}");
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
                MainFile.Logger.Warn($"SaveSlotManager: Slot {slot} is empty.");
                return false;
            }

            var json = File.ReadAllText(path);
            var result = SaveManager.FromJson<SerializableRun>(json);
            if (result.SaveData == null)
            {
                MainFile.Logger.Warn($"SaveSlotManager: Failed to deserialize slot {slot}.");
                return false;
            }

            return LoadFromSave(result.SaveData);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SaveSlotManager: Load slot {slot} failed: {ex.Message}");
            return false;
        }
    }

    public static bool DeleteSlot(int slot)
    {
        try
        {
            var deleted = false;
            var savePath = SlotPath(slot);
            var metaPath = MetaPath(slot);

            if (File.Exists(savePath)) { File.Delete(savePath); deleted = true; }
            if (File.Exists(metaPath)) { File.Delete(metaPath); deleted = true; }

            if (deleted)
                MainFile.Logger.Info($"SaveSlotManager: Deleted slot {slot}.");
            return deleted;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SaveSlotManager: Delete slot {slot} failed: {ex.Message}");
            return false;
        }
    }

    public static bool HasSlot(int slot) => File.Exists(SlotPath(slot));

    public static SaveSlotMeta? LoadMeta(int slot)
    {
        var path = MetaPath(slot);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<SaveSlotMeta>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public static void RenameSlot(int slot, string name)
    {
        var meta = LoadMeta(slot) ?? new SaveSlotMeta();
        meta.Name = name;
        try { File.WriteAllText(MetaPath(slot), JsonSerializer.Serialize(meta)); }
        catch (Exception ex) { MainFile.Logger.Warn($"SaveSlotManager: Rename slot {slot} failed: {ex.Message}"); }
    }

    // ──────── Helpers ────────

    private static string SlotPath(int slot) => Path.Combine(SnapshotDir, $"slot{slot}.json");
    private static string MetaPath(int slot) => Path.Combine(SnapshotDir, $"slot{slot}_meta.json");

    private static SaveSlotMeta CaptureMetaFromState(RunState state, string name)
    {
        var player = state.Players.FirstOrDefault();
        var meta = new SaveSlotMeta
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

        meta.Seed = state.Rng?.StringSeed ?? "";

        meta.ModList = ModManager.LoadedMods
            .Where(m => m.manifest != null)
            .Select(m => $"{m.manifest!.name} v{m.manifest!.version}")
            .ToList();

        return meta;
    }

    // ──────── Internal load ────────

    private static bool LoadFromSave(SerializableRun save)
    {
        try
        {
            if (RunManager.Instance == null)
            {
                MainFile.Logger.Warn("SaveSlotManager: No RunManager instance.");
                return false;
            }

            TaskHelper.RunSafely(LoadFromSaveAsync(save));
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SaveSlotManager: Load save failed: {ex.Message}");
            return false;
        }
    }

    private static async Task LoadFromSaveAsync(SerializableRun save)
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

        MainFile.Logger.Info("SaveSlotManager: Save loaded successfully.");
    }
}
