using SGuardLimiterMax.Models;
using SGuardLimiterMax.Services;
using Xunit;

namespace SGuardLimiterMax.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly string _tempPath;

    public ConfigManagerTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"SGuardLimiterMax_Test_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        try { File.Delete(_tempPath); } catch { }
    }

    [Fact]
    public void Load_MissingFile_CreatesDefaultsAndPersists()
    {
        Assert.False(File.Exists(_tempPath));

        var config = ConfigManager.Load(_tempPath);

        Assert.NotNull(config);
        Assert.True(config.ThrottleSGuard);
        Assert.True(config.BoostGamePriority);
        Assert.False(config.UnbindCPU);
        Assert.True(File.Exists(_tempPath));
    }

    [Fact]
    public void Load_CorruptJson_FallsBackToDefaultsAndOverwritesFile()
    {
        File.WriteAllText(_tempPath, "this is not json {\n");

        var config = ConfigManager.Load(_tempPath);

        Assert.NotNull(config);
        Assert.True(config.ThrottleSGuard);
        // File should have been overwritten with valid JSON defaults.
        var reloaded = ConfigManager.Load(_tempPath);
        Assert.True(reloaded.ThrottleSGuard);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesValues()
    {
        var original = new AppConfig
        {
            ThrottleSGuard = false,
            BoostGamePriority = false,
            UnbindCPU = true,
            TargetPowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2e",
            TimerResolutionPeriod100Ns = 5000,
            CustomGames =
            [
                new CustomGameEntry { ProcessName = "TestGame", DisplayName = "测试游戏", BoostPriority = true, UnbindCpu0 = true }
            ]
        };

        ConfigManager.Save(original, _tempPath);
        var loaded = ConfigManager.Load(_tempPath);

        Assert.Equal(original.ThrottleSGuard, loaded.ThrottleSGuard);
        Assert.Equal(original.BoostGamePriority, loaded.BoostGamePriority);
        Assert.Equal(original.UnbindCPU, loaded.UnbindCPU);
        Assert.Equal(original.TargetPowerPlanGuid, loaded.TargetPowerPlanGuid);
        Assert.Equal(original.TimerResolutionPeriod100Ns, loaded.TimerResolutionPeriod100Ns);
        Assert.Single(loaded.CustomGames);
        Assert.Equal("TestGame", loaded.CustomGames[0].ProcessName);
        Assert.Equal("测试游戏", loaded.CustomGames[0].DisplayName);
    }

    [Fact]
    public void Save_SwallowsExceptions_ForInvalidPath()
    {
        var config = new AppConfig();
        // Should not throw even though the path is invalid.
        ConfigManager.Save(config, "?:\\\\\\\\invalid\\\npath\\\n.json");
    }
}
