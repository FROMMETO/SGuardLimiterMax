using SGuardLimiterMax.Services;
using Xunit;

namespace SGuardLimiterMax.Tests;

public class PowerManagerTests
{
    private const string SampleListOutput = """
        Existing Power Schemes (* Active)
        -----------------------------------
        Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (平衡)
        Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (高性能)
        Power Scheme GUID: e9a42b02-d5df-448d-aa00-03f14749eb61  (卓越性能) *
        """;

    [Fact]
    public void ParsePlansFromOutput_Sample_ReturnsAllPlansAndActiveFlag()
    {
        var plans = PowerManager.ParsePlansFromOutput(SampleListOutput);

        Assert.Equal(3, plans.Count);

        var balanced = plans.FirstOrDefault(p => p.Guid == "381b4222-f694-41f0-9685-ff5bb260df2e");
        Assert.NotNull(balanced);
        Assert.Equal("平衡", balanced.Name);
        Assert.False(balanced.IsActive);

        var ultimate = plans.FirstOrDefault(p => p.Guid == "e9a42b02-d5df-448d-aa00-03f14749eb61");
        Assert.NotNull(ultimate);
        Assert.Equal("卓越性能", ultimate.Name);
        Assert.True(ultimate.IsActive);
    }

    [Fact]
    public void ParsePlansFromOutput_Empty_ReturnsEmptyList()
    {
        var plans = PowerManager.ParsePlansFromOutput(string.Empty);
        Assert.Empty(plans);
    }

    [Fact]
    public void ParsePlansFromOutput_Guid_IsLowercase()
    {
        var output = "Power Scheme GUID: 8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C  (High) *";
        var plans = PowerManager.ParsePlansFromOutput(output);

        Assert.Single(plans);
        Assert.Equal("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", plans[0].Guid);
    }

    [Fact]
    public void ParsePlansFromOutput_Malformed_ReturnsEmptyList()
    {
        var plans = PowerManager.ParsePlansFromOutput("Power Scheme GUID: not-a-guid  (Name) *");
        Assert.Empty(plans);
    }
}
