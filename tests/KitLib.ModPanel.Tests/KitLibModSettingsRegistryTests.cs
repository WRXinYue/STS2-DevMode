using KitLib.Abstractions.Modding;

namespace KitLib.ModPanel.Tests;

public sealed class KitLibModSettingsRegistryTests {
    [Fact]
    public void GetPages_returns_sorted_by_order_then_id() {
        KitLibModSettingsRegistry.ClearForTests();
        try {
            KitLibModSettingsRegistry.Register(new KitLibModSettingsPageRegistration {
                ModId = "KitLib",
                PageId = "z",
                Title = "Z",
                SortOrder = 10,
                BuildBody = () => new object(),
            });
            KitLibModSettingsRegistry.Register(new KitLibModSettingsPageRegistration {
                ModId = "KitLib",
                PageId = "a",
                Title = "A",
                SortOrder = 0,
                BuildBody = () => new object(),
            });
            var pages = KitLibModSettingsRegistry.GetPages("KitLib");
            Assert.Equal(2, pages.Count);
            Assert.Equal("a", pages[0].PageId);
            Assert.Equal("z", pages[1].PageId);
        }
        finally {
            KitLibModSettingsRegistry.ClearForTests();
        }
    }

    [Fact]
    public void Register_replaces_same_mod_and_page_id() {
        KitLibModSettingsRegistry.ClearForTests();
        try {
            KitLibModSettingsRegistry.Register(new KitLibModSettingsPageRegistration {
                ModId = "KitLib",
                PageId = "general",
                Title = "Old",
                BuildBody = () => "old",
            });
            KitLibModSettingsRegistry.Register(new KitLibModSettingsPageRegistration {
                ModId = "KitLib",
                PageId = "general",
                Title = "New",
                BuildBody = () => "new",
            });
            var pages = KitLibModSettingsRegistry.GetPages("KitLib");
            Assert.Single(pages);
            Assert.Equal("New", pages[0].Title);
        }
        finally {
            KitLibModSettingsRegistry.ClearForTests();
        }
    }
}
