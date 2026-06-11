using KitLib.Abstractions.Modding;
using KitLib.Logging;

namespace KitLib.Abstractions.Tests;

public sealed class KitLibModSettingsUiOpsTests {
    [Fact]
    public void BuildLogLevelRow_is_null_when_modpanel_not_loaded() {
        Assert.Null(KitLibModSettingsUiOps.BuildLogLevelRow);
    }
}
