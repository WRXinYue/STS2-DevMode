using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Actions;

/// <summary>
/// Deep card editing: modify cost, replay, damage, block, keywords, enchantments.
/// Uses reflection for cross-version compatibility.
/// </summary>
internal static class CardEditActions
{
    private const BindingFlags ReflFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static bool TrySetBaseCost(CardModel card, int cost)
    {
        return TrySetProperty(card, "BaseCost", cost)
            || TrySetProperty(card, "Cost", cost)
            || TrySetField(card, "_baseCost", cost);
    }

    public static bool TrySetReplayCount(CardModel card, int count)
    {
        return TrySetProperty(card, "ReplayCount", count)
            || TrySetProperty(card, "Replay", count)
            || TrySetField(card, "_replayCount", count);
    }

    public static bool TrySetDamage(CardModel card, int damage)
    {
        return TrySetProperty(card, "BaseDamage", damage)
            || TrySetProperty(card, "Damage", damage)
            || TrySetField(card, "_baseDamage", damage);
    }

    public static bool TrySetBlock(CardModel card, int block)
    {
        return TrySetProperty(card, "BaseBlock", block)
            || TrySetProperty(card, "Block", block)
            || TrySetField(card, "_baseBlock", block);
    }

    public static bool TrySetExhaust(CardModel card, bool exhaust)
    {
        return TrySetProperty(card, "Exhaust", exhaust)
            || TrySetField(card, "_exhaust", exhaust);
    }

    public static bool TrySetEthereal(CardModel card, bool ethereal)
    {
        return TrySetProperty(card, "Ethereal", ethereal)
            || TrySetField(card, "_ethereal", ethereal);
    }

    public static bool TrySetUnplayable(CardModel card, bool unplayable)
    {
        return TrySetProperty(card, "Unplayable", unplayable)
            || TrySetField(card, "_unplayable", unplayable);
    }

    public static int? GetBaseCost(CardModel card)
    {
        return TryGetInt(card, "BaseCost") ?? TryGetInt(card, "Cost");
    }

    public static int? GetReplayCount(CardModel card)
    {
        return TryGetInt(card, "ReplayCount") ?? TryGetInt(card, "Replay");
    }

    public static int? GetDamage(CardModel card)
    {
        return TryGetInt(card, "BaseDamage") ?? TryGetInt(card, "Damage");
    }

    public static int? GetBlock(CardModel card)
    {
        return TryGetInt(card, "BaseBlock") ?? TryGetInt(card, "Block");
    }

    public static bool? GetExhaust(CardModel card) => TryGetBool(card, "Exhaust");
    public static bool? GetEthereal(CardModel card) => TryGetBool(card, "Ethereal");
    public static bool? GetUnplayable(CardModel card) => TryGetBool(card, "Unplayable");

    /// <summary>Get all enchantment types available in the game.</summary>
    public static IReadOnlyList<Type> GetEnchantmentTypes()
    {
        try
        {
            var baseType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .FirstOrDefault(t => t.Name == "AbstractEnchantment" && !t.IsInterface);

            if (baseType == null) return Array.Empty<Type>();

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToArray();
        }
        catch { return Array.Empty<Type>(); }
    }

    /// <summary>Try to apply an enchantment to a card.</summary>
    public static bool TryApplyEnchantment(CardModel card, Type enchantmentType, bool force = false)
    {
        try
        {
            var enchantment = Activator.CreateInstance(enchantmentType);
            if (enchantment == null) return false;

            // Try CardCmd.Enchant or similar
            var enchantMethod = typeof(CardCmd).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.Contains("Enchant", StringComparison.OrdinalIgnoreCase));

            if (enchantMethod != null)
            {
                var parameters = enchantMethod.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var pt = parameters[i].ParameterType;
                    if (pt == typeof(CardModel) || typeof(CardModel).IsAssignableFrom(pt))
                        args[i] = card;
                    else if (enchantmentType.IsAssignableFrom(pt) || pt.IsAssignableFrom(enchantmentType))
                        args[i] = enchantment;
                    else if (pt == typeof(bool))
                        args[i] = force;
                    else if (parameters[i].HasDefaultValue)
                        args[i] = parameters[i].DefaultValue;
                    else
                        args[i] = null;
                }
                enchantMethod.Invoke(null, args);
                return true;
            }

            // Fallback: set Enchantment property directly
            return TrySetProperty(card, "Enchantment", enchantment)
                || TrySetProperty(card, "CurrentEnchantment", enchantment);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Apply enchantment failed: {ex.Message}");
            return false;
        }
    }

    public static bool TryClearEnchantment(CardModel card)
    {
        return TrySetProperty(card, "Enchantment", null)
            || TrySetProperty(card, "CurrentEnchantment", null);
    }

    /// <summary>Get all cards in the player's deck for editing.</summary>
    public static IReadOnlyList<CardModel> GetDeckCards(Player player)
    {
        return player.Deck?.Cards?.ToArray() ?? Array.Empty<CardModel>();
    }

    public static string GetCardDisplayName(CardModel card)
    {
        try { return card.Title ?? ((AbstractModel)card).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)card).Id.Entry ?? "?"; }
    }

    // ── Reflection helpers ──

    private static bool TrySetProperty(object target, string name, object? value)
    {
        var prop = target.GetType().GetProperty(name, ReflFlags);
        if (prop is not { CanWrite: true }) return false;
        try { prop.SetValue(target, value); return true; }
        catch { return false; }
    }

    private static bool TrySetField(object target, string name, object? value)
    {
        var field = target.GetType().GetField(name, ReflFlags);
        if (field == null || field.IsInitOnly) return false;
        try { field.SetValue(target, value); return true; }
        catch { return false; }
    }

    private static int? TryGetInt(object target, string name)
    {
        try
        {
            var prop = target.GetType().GetProperty(name, ReflFlags);
            if (prop != null) return Convert.ToInt32(prop.GetValue(target));
            var field = target.GetType().GetField(name, ReflFlags);
            if (field != null) return Convert.ToInt32(field.GetValue(target));
        }
        catch { }
        return null;
    }

    private static bool? TryGetBool(object target, string name)
    {
        try
        {
            var prop = target.GetType().GetProperty(name, ReflFlags);
            if (prop != null) return (bool?)prop.GetValue(target);
            var field = target.GetType().GetField(name, ReflFlags);
            if (field != null) return (bool?)field.GetValue(target);
        }
        catch { }
        return null;
    }
}
