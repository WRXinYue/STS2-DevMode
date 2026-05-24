using System.Collections.Generic;
using System.Linq;
using DevMode.Settings;
using MegaCrit.Sts2.Core.Models;

namespace DevMode;

internal static class CardLibraryVisibility {
    public static bool ShowHiddenCards => SettingsStore.Current.ShowHiddenCards;

    public static List<CardModel> GetLibraryCards() {
        var all = ModelDb.AllCards;
        if (ShowHiddenCards)
            return all.ToList();
        return all.Where(c => c.ShouldShowInCardLibrary).ToList();
    }
}
