using SGuardLimiterMax.Services;
using Xunit;

namespace SGuardLimiterMax.Tests;

public class TimerResolutionServiceTests
{
    [Fact]
    public void Options_IsNotEmpty_And_ContainsSystemDefault()
    {
        var options = TimerResolutionService.Options;

        Assert.NotEmpty(options);
        Assert.Contains(options, o => o.Period100Ns == TimerResolutionService.PeriodSysDefault);
        Assert.Contains(options, o => o.Period100Ns == 5000);
        Assert.Contains(options, o => o.Period100Ns == 10000);
    }

    [Fact]
    public void IsActive_InitiallyFalse()
    {
        Assert.False(TimerResolutionService.IsActive);
    }

    [Fact]
    public void Enable_ThenDisable_TogglesIsActive()
    {
        // NtSetTimerResolution may fail without admin rights; the test asserts state
        // transitions when the API succeeds. If it fails, Enable sets _active anyway
        // to reflect the requested state.
        TimerResolutionService.Enable(10000);
        Assert.True(TimerResolutionService.IsActive);

        TimerResolutionService.Disable();
        Assert.False(TimerResolutionService.IsActive);
    }

    [Fact]
    public void Enable_SamePeriodTwice_DoesNotThrow()
    {
        TimerResolutionService.Enable(10000);
        TimerResolutionService.Enable(10000);
        Assert.True(TimerResolutionService.IsActive);

        // Clean up.
        TimerResolutionService.Disable();
    }

    [Fact]
    public void QueryCurrentResolutionText_ReturnsNonEmptyString()
    {
        string text = TimerResolutionService.QueryCurrentResolutionText();
        Assert.False(string.IsNullOrWhiteSpace(text));
    }
}
