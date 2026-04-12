using System;
using System.Collections.Generic;
using System.Linq;
using DevMode.Actions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace DevMode.UI;

internal static partial class CardBrowserUI {
    // ── Grid constants ──

    private const float CardDisplayScale = 0.65f;
    private const int CardGridSeparation = 6;
    private const float CardSlotInnerPad = 12f;
    private const int CardBrowserGridPadH = 14;
    private const int CardBrowserGridPadV = 12;

    private static readonly Color ColCardPickNormal = new(0.90f, 0.90f, 0.93f, 1f);
    private static readonly Color ColCardPickSelected = Colors.White;

    // ── Primitive helpers ──

    private static Control CreateEmptyHost() {
        float cardW = NCard.defaultSize.X * CardDisplayScale;
        float cardH = NCard.defaultSize.Y * CardDisplayScale;
        float slotW = cardW + 2f * CardSlotInnerPad;
        float slotH = cardH + 2f * CardSlotInnerPad;

        return new Control {
            CustomMinimumSize = new Vector2(slotW, slotH),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.None,
            Modulate = ColCardPickNormal
        };
    }

    private static NCard? PopulateHost(Control host, CardModel card) {
        float slotW = host.CustomMinimumSize.X;
        float slotH = host.CustomMinimumSize.Y;
        try {
            var nCard = NCard.Create(card);
            if (nCard != null) {
                nCard.Position = new Vector2(slotW / 2f, slotH / 2f);
                nCard.Scale = new Vector2(CardDisplayScale, CardDisplayScale);
                SetDescendantsMouseFilterIgnore(nCard);
                host.AddChild(nCard);
                return nCard;
            }
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[DevMode] NCard.Create failed for {card.Id}: {ex.Message}");
        }
        AddCardFallback(host, card);
        return null;
    }

    private static void ClearCardGrid(GridContainer grid) {
        foreach (var child in grid.GetChildren()) {
            if (child is Node hostNode) {
                foreach (var sub in hostNode.GetChildren()) {
                    if (sub is NCard)
                        ((Node)sub).QueueFreeSafely();
                }
                hostNode.QueueFree();
            }
        }
    }

    private static void SetDescendantsMouseFilterIgnore(Node root) {
        foreach (var child in root.GetChildren()) {
            if (child is Control c)
                c.MouseFilter = Control.MouseFilterEnum.Ignore;
            SetDescendantsMouseFilterIgnore(child);
        }
    }

    private static void AddCardFallback(Control container, CardModel card) {
        var fallback = new ColorRect {
            Color = new Color(0.2f, 0.2f, 0.25f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        fallback.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        container.AddChild(fallback);

        var label = new Label {
            Text = CardEditActions.GetCardDisplayName(card),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        container.AddChild(label);
    }

    // ── State-aware grid operations ──

    private static List<CardModel> GetCards(State s) {
        if (IsLibrarySource)
            return ModelDb.AllCards.Where(c => c.ShouldShowInCardLibrary).ToList();
        var t = BrowseSourceToTarget(_browseSource);
        return t.HasValue
            ? CardActions.GetCardsForTarget(s.Player, t.Value)
            : new List<CardModel>();
    }

    private static void InvalidateCardCache(State s) {
        foreach (var child in s.CardGrid.GetChildren())
            s.CardGrid.RemoveChild((Node)child);

        foreach (var (_, entry) in s.HostCache) {
            if (entry.nCard != null)
                ((Node)entry.nCard).QueueFreeSafely();
            entry.host.QueueFree();
        }
        s.HostCache.Clear();
        s.CachedAllCards = GetCards(s);
    }

    private static (Control host, bool isNew) GetOrCreateHost(State s, CardModel card) {
        if (s.HostCache.TryGetValue(card, out var cached))
            return (cached.host, false);

        var host = CreateEmptyHost();
        var capturedCard = card;
        host.GuiInput += evt => {
            if (evt is not InputEventMouseButton mb || !mb.Pressed ||
                mb.ButtonIndex != MouseButton.Left)
                return;
            if (s.SelectedPickHost != null)
                s.SelectedPickHost.Modulate = ColCardPickNormal;
            s.SelectedPickHost = host;
            host.Modulate = ColCardPickSelected;
            host.AcceptEvent();
            ShowRightPanel(s, capturedCard);
        };
        s.HostCache[card] = (host, null, false);
        return (host, true);
    }

    private static void UpdateCardGridColumns(State s) {
        if (!s.CardGrid.IsNodeReady())
            return;
        float w = s.GridScroll.GetRect().Size.X - 2f * CardBrowserGridPadH;
        if (w < 2f)
            return;
        float scaledCardW = NCard.defaultSize.X * CardDisplayScale;
        float slotW = scaledCardW + 2f * CardSlotInnerPad + CardGridSeparation;
        int cols = Math.Max(1, (int)Math.Floor((w - 4f) / slotW));
        if (s.CardGrid.Columns != cols)
            s.CardGrid.Columns = cols;
    }

    private static void RebuildGrid(State s, string searchText) {
        s.SelectedPickHost = null;

        s.FilteredCards = s.CachedAllCards.Where(c => {
            if (!MatchesTypeSet(c, s.ActiveTypeFilters)) return false;
            if (!MatchesRaritySet(c, s.ActiveRarityFilters)) return false;
            if (!MatchesCostSet(c, s.ActiveCostFilters)) return false;
            if (IsLibrarySource && !MatchesPoolSet(c, s.ActivePoolFilters, s.PoolFilterPredicates)) return false;
            if (!string.IsNullOrWhiteSpace(searchText)) {
                var name = CardEditActions.GetCardDisplayName(c);
                string desc;
                try { desc = c.GetDescriptionForPile(PileType.None)?.StripBbCode() ?? ""; }
                catch { desc = ""; }
                var combined = name + " " + desc;
                if (!combined.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }).ToList();

        s.FilteredCards.Sort((a, b) => CompareCards(a, b, s.SortPriority));

        foreach (var child in s.CardGrid.GetChildren())
            s.CardGrid.RemoveChild((Node)child);

        foreach (var card in s.FilteredCards) {
            var (host, _) = GetOrCreateHost(s, card);
            s.CardGrid.AddChild(host);
        }

        Callable.From(() => UpdateCardGridColumns(s)).CallDeferred();
        Callable.From(() => PopulateVisibleHosts(s)).CallDeferred();

        s.StatusLabel.Text = string.Format(I18N.T("cardBrowser.count", "{0} / {1} cards"),
            s.FilteredCards.Count, s.CachedAllCards.Count);
    }

    private static void PopulateVisibleHosts(State s) {
        if (s.FilteredCards.Count == 0) return;
        int cols = s.CardGrid.Columns;
        if (cols <= 0) cols = 1;

        float slotH = NCard.defaultSize.Y * CardDisplayScale + 2f * CardSlotInnerPad;
        float rowH = slotH + CardGridSeparation;
        float scrollY = s.GridScroll.ScrollVertical;
        float viewH = s.GridScroll.GetRect().Size.Y;
        if (viewH < 1f) return;

        const int buffer = 2;
        int topRow = Math.Max(0, (int)((scrollY - CardBrowserGridPadV) / rowH) - buffer);
        int bottomRow = (int)Math.Ceiling((scrollY + viewH - CardBrowserGridPadV) / rowH) + buffer;

        int startIdx = topRow * cols;
        int endIdx = Math.Min(s.FilteredCards.Count, (bottomRow + 1) * cols);

        for (int i = startIdx; i < endIdx; i++) {
            var card = s.FilteredCards[i];
            if (!s.HostCache.TryGetValue(card, out var entry)) continue;
            if (entry.nCard != null) continue;

            var nCard = PopulateHost(entry.host, card);
            if (nCard != null) {
                try { nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal); }
                catch { }
            }
            s.HostCache[card] = (entry.host, nCard, true);
        }
    }
}
