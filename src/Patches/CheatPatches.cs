using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace DevMode.Patches;

/// <summary>Infinite HP — prevent player HP loss.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
public static class InfiniteHpPatch
{
    public static bool Prefix(Creature __instance, ref DamageResult __result, decimal amount, ValueProp props)
    {
        if (!DevModeState.InDevRun || !DevModeState.InfiniteHp) return true;
        if (__instance.Player == null) return true;
        __result = new DamageResult(__instance, props)
        {
            UnblockedDamage = 0,
            WasTargetKilled = false,
            OverkillDamage = 0
        };
        return false;
    }
}

/// <summary>Infinite Block — refill player block after loss.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.LoseBlockInternal))]
public static class InfiniteBlockPatch
{
    public static void Postfix(Creature __instance)
    {
        if (!DevModeState.InDevRun || !DevModeState.InfiniteBlock) return;
        if (__instance.Player == null) return;
        __instance.GainBlockInternal(999 - __instance.Block);
    }
}

/// <summary>Infinite Energy — refill energy after spending.</summary>
[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseEnergy))]
public static class InfiniteEnergyPatch
{
    public static void Postfix(PlayerCombatState __instance)
    {
        if (!DevModeState.InDevRun || !DevModeState.InfiniteEnergy) return;
        __instance.Energy = 999;
    }
}

/// <summary>Infinite Stars — refill stars after spending.</summary>
[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseStars))]
public static class InfiniteStarsPatch
{
    public static void Postfix(PlayerCombatState __instance)
    {
        if (!DevModeState.InDevRun || !DevModeState.InfiniteStars) return;
        __instance.Stars = 999;
    }
}

/// <summary>Always reward potion — force potion drop from combat.</summary>
[HarmonyPatch(typeof(PotionRewardOdds), nameof(PotionRewardOdds.Roll))]
public static class AlwaysPotionRewardPatch
{
    public static void Postfix(ref bool __result)
    {
        if (!DevModeState.InDevRun || !DevModeState.AlwaysRewardPotion) return;
        __result = true;
    }
}

/// <summary>Always upgrade card rewards.</summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Factories.CardFactory), "RollForUpgrade",
    [typeof(Player), typeof(MegaCrit.Sts2.Core.Models.CardModel), typeof(decimal), typeof(MegaCrit.Sts2.Core.Random.Rng)])]
public static class AlwaysUpgradeRewardPatch
{
    public static void Prefix(ref decimal baseChance)
    {
        if (!DevModeState.InDevRun || !DevModeState.AlwaysUpgradeCardReward) return;
        baseChance = 999m;
    }
}

/// <summary>Max card reward rarity — all card rewards are Rare.</summary>
[HarmonyPatch(typeof(CardRarityOdds), nameof(CardRarityOdds.Roll))]
public static class MaxCardRarityPatch
{
    public static void Postfix(ref CardRarity __result)
    {
        if (!DevModeState.InDevRun || !DevModeState.MaxCardRewardRarity) return;
        __result = CardRarity.Rare;
    }
}

/// <summary>Defense multiplier — multiply block gained by player.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.GainBlockInternal))]
public static class DefenseMultiplierPatch
{
    public static void Prefix(Creature __instance, ref decimal amount)
    {
        if (!DevModeState.InDevRun || DevModeState.DefenseMultiplier == 1.0f) return;
        if (__instance.Player == null) return;
        amount = Math.Round(amount * (decimal)DevModeState.DefenseMultiplier);
    }
}

/// <summary>Gold multiplier — multiply gold gained.</summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.PlayerCmd), nameof(MegaCrit.Sts2.Core.Commands.PlayerCmd.GainGold))]
public static class GoldMultiplierPatch
{
    public static void Prefix(ref decimal amount)
    {
        if (!DevModeState.InDevRun || DevModeState.GoldMultiplier == 1.0f) return;
        amount = Math.Round(amount * (decimal)DevModeState.GoldMultiplier);
    }
}

/// <summary>Free shop — bypass gold cost for merchant purchases.</summary>
[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper))]
public static class FreeShopPatch
{
    public static void Prefix(ref bool ignoreCost)
    {
        if (!DevModeState.InDevRun || !DevModeState.FreeShop) return;
        ignoreCost = true;
    }
}

/// <summary>Freeze enemies — skip enemy turns entirely.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.TakeTurn))]
public static class FreezeEnemiesPatch
{
    public static bool Prefix(Creature __instance, ref Task __result)
    {
        if (!DevModeState.InDevRun || !DevModeState.FreezeEnemies) return true;
        if (__instance.Player != null) return true;
        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>One-hit kill — enemies take massive damage.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
[HarmonyPriority(Priority.High)]
public static class OneHitKillPatch
{
    public static void Prefix(Creature __instance, ref decimal amount)
    {
        if (!DevModeState.InDevRun || !DevModeState.OneHitKill) return;
        if (__instance.Player != null) return;
        amount = 999999m;
    }
}

/// <summary>Damage multiplier — multiply damage dealt to enemies.</summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.DamageBlockInternal))]
public static class DamageMultiplierPatch
{
    public static void Prefix(Creature __instance, ref decimal amount)
    {
        if (!DevModeState.InDevRun || DevModeState.DamageMultiplier == 1.0f) return;
        if (__instance.Player != null) return;
        amount = Math.Round(amount * (decimal)DevModeState.DamageMultiplier);
    }
}

/// <summary>Unknown map points always resolve to Treasure.</summary>
[HarmonyPatch(typeof(UnknownMapPointOdds), nameof(UnknownMapPointOdds.Roll))]
public static class UnknownMapTreasurePatch
{
    public static void Postfix(ref RoomType __result)
    {
        if (!DevModeState.InDevRun || !DevModeState.UnknownMapAlwaysTreasure) return;
        __result = RoomType.Treasure;
    }
}
