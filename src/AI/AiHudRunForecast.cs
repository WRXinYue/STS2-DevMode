using System;
using System.Text.Json.Nodes;
using DevMode.AI.Combat;
using DevMode.AI.Core.Schema;
using DevMode.AI.Planning;

namespace DevMode.AI;

/// <summary>Lightweight deck/route hints for the in-game HUD (no beam or MC sim).</summary>
public static class AiHudRunForecast {
    /// <summary>Matches <c>BigDeck</c> bronze badge threshold.</summary>
    public const int OfficialBigDeckMin = 40;
    /// <summary>Matches <c>TinyDeck</c> bronze badge threshold.</summary>
    public const int OfficialSmallDeckMax = 20;

    const int CacheTtlMs = 3000;

    static readonly object CacheGate = new();
    static int _cacheKey;
    static long _cacheUtcMs;
    static HudContext? _cached;

    public enum DeckStyle {
        Big,
        Small,
    }

    public sealed record DeckProfile(
        DeckStyle Style,
        int DeckSize,
        bool IsExhaustFocused);

    public sealed record RunPrognosis(
        float WinRate,
        int RouteNodes,
        int PathRisk,
        float CombatsToRest,
        int NextFightIncoming);

    public sealed record HudContext(DeckProfile Profile, RunPrognosis Prognosis);

    public static bool TryGetCachedContext(out HudContext context) {
        lock (CacheGate) {
            if (_cached != null && Environment.TickCount64 - _cacheUtcMs < CacheTtlMs) {
                context = _cached;
                return true;
            }
        }

        context = null!;
        return false;
    }

    public static HudContext BuildHudContext(JsonObject snapshot, GamePhase phase) {
        int key = ComputeCacheKey(snapshot, (int)phase);
        var now = Environment.TickCount64;
        lock (CacheGate) {
            if (_cached != null && key == _cacheKey && now - _cacheUtcMs < CacheTtlMs)
                return _cached;
        }

        var profile = AnalyzeDeckLight(snapshot);
        var prognosis = AnalyzeRunLight(snapshot, profile);
        var ctx = new HudContext(profile, prognosis);

        lock (CacheGate) {
            _cacheKey = key;
            _cacheUtcMs = now;
            _cached = ctx;
        }

        return ctx;
    }

    public static void ClearCache() {
        lock (CacheGate) {
            _cacheKey = 0;
            _cacheUtcMs = 0;
            _cached = null;
        }
    }

    public static DeckProfile AnalyzeDeckLight(JsonObject snapshot) {
        int deckSize = snapshot["deck"]?.AsArray()?.Count ?? 0;
        bool exhaustFocused = false;
        var deck = snapshot["deck"]?.AsArray();
        if (deck != null) {
            int exhaust = 0;
            foreach (var node in deck) {
                if (node is not JsonObject card) continue;
                var keywords = card["keywords"]?.AsArray();
                if (keywords == null) continue;
                foreach (var kw in keywords) {
                    if ((kw?.GetValue<string>() ?? "").Contains("Exhaust", StringComparison.OrdinalIgnoreCase)) {
                        exhaust++;
                        break;
                    }
                }
            }

            exhaustFocused = exhaust >= 3;
        }

        return new DeckProfile(InferStyle(deckSize), deckSize, exhaustFocused);
    }

    static RunPrognosis AnalyzeRunLight(JsonObject snapshot, DeckProfile profile) {
        var mapPlan = MapPathPlanner.CachedPlan;

        int routeNodes = mapPlan?.PathCoords.Count ?? 0;
        int pathRisk = mapPlan?.PathRiskAtNext ?? 0;
        float combatsToRest = mapPlan?.CombatsToRestAtNext ?? 0f;
        int incoming = ReadPreviewIncoming(snapshot);

        float winRate = EstimateWinRateLight(snapshot, profile, pathRisk, incoming);

        return new RunPrognosis(winRate, routeNodes, pathRisk, combatsToRest, incoming);
    }

    static int ReadPreviewIncoming(JsonObject snapshot) {
        if (snapshot["combat"] != null)
            return IntentCalculator.TotalIncomingDamage(snapshot);

        var preview = snapshot["nextFightPreview"]?.AsArray();
        if (preview != null && preview.Count > 0 && preview[0] is JsonObject fight)
            return fight["incomingTurn1"]?.GetValue<int>() ?? 0;

        return 0;
    }

    static DeckStyle InferStyle(int deckSize) {
        if (deckSize >= OfficialBigDeckMin)
            return DeckStyle.Big;
        if (deckSize <= OfficialSmallDeckMax)
            return DeckStyle.Small;

        int toBig = OfficialBigDeckMin - deckSize;
        int toSmall = deckSize - OfficialSmallDeckMax;
        return toBig <= toSmall ? DeckStyle.Big : DeckStyle.Small;
    }

    static float EstimateWinRateLight(
        JsonObject snapshot,
        DeckProfile profile,
        int pathRisk,
        int incoming) {
        var act = snapshot["actIndex"]?.GetValue<int>() ?? 0;
        var hp = IntentCalculator.HpRatio(snapshot);

        float rate = act switch {
            0 => 0.58f,
            1 => 0.48f,
            _ => 0.38f,
        };

        rate += (hp - 0.55f) * 0.35f;
        rate -= pathRisk / 250f;

        if (profile.Style == DeckStyle.Big && profile.DeckSize >= OfficialBigDeckMin)
            rate -= 0.06f;
        else if (profile.Style == DeckStyle.Small && profile.DeckSize <= OfficialSmallDeckMax)
            rate += 0.03f;

        if (incoming > 0) {
            var hpNow = snapshot["currentHp"]?.GetValue<int>() ?? 0;
            if (incoming >= hpNow)
                rate -= 0.08f;
        }

        return Math.Clamp(rate, 0.08f, 0.92f);
    }

    static int ComputeCacheKey(JsonObject snapshot, int phase) {
        var floor = snapshot["totalFloor"]?.GetValue<int>() ?? 0;
        var deckSize = snapshot["deck"]?.AsArray()?.Count ?? 0;
        var hp = snapshot["currentHp"]?.GetValue<int>() ?? 0;
        var gold = snapshot["gold"]?.GetValue<int>() ?? 0;
        return HashCode.Combine(floor, deckSize, hp, gold, phase);
    }

    public static string StyleLabel(DeckStyle style) => style switch {
        DeckStyle.Big => I18N.T("ai.hud.deck.big", "Big deck"),
        _ => I18N.T("ai.hud.deck.small", "Small deck"),
    };

    public static bool MeetsOfficialBig(DeckProfile profile) =>
        profile.DeckSize >= OfficialBigDeckMin;

    public static bool MeetsOfficialSmall(DeckProfile profile) =>
        profile.DeckSize <= OfficialSmallDeckMax;
}
