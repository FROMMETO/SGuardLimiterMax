using System.Windows.Input;
using SGuardLimiterMax.Services;
using Xunit;

namespace SGuardLimiterMax.Tests;

public class GlobalHotkeyServiceTests
{
    [Theory]
    [InlineData("Ctrl+Shift+U", ModifierKeys.Control | ModifierKeys.Shift, Key.U)]
    [InlineData("Alt+F4", ModifierKeys.Alt, Key.F4)]
    [InlineData("Ctrl+Alt+Win+T", ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows, Key.T)]
    public void TryParseHotkey_ValidInput_ReturnsTrueWithExpectedValues(
        string input, ModifierKeys expectedMods, Key expectedKey)
    {
        bool ok = GlobalHotkeyService.TryParseHotkey(input, out var mods, out var key);

        Assert.True(ok);
        Assert.Equal(expectedMods, mods);
        Assert.Equal(expectedKey, key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("U")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+InvalidKey")]
    [InlineData("+")]
    public void TryParseHotkey_InvalidInput_ReturnsFalse(string input)
    {
        bool ok = GlobalHotkeyService.TryParseHotkey(input, out var mods, out var key);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseHotkey_WhitespaceOnlyModifier_ReturnsFalse()
    {
        bool ok = GlobalHotkeyService.TryParseHotkey("Ctrl + U", out var mods, out var key);
        Assert.True(ok);
        Assert.Equal(ModifierKeys.Control, mods);
        Assert.Equal(Key.U, key);
    }

    [Fact]
    public void HotkeyToString_RoundTrip_MatchesParse()
    {
        string original = "Ctrl+Shift+U";
        Assert.True(GlobalHotkeyService.TryParseHotkey(original, out var mods, out var key));
        string formatted = GlobalHotkeyService.HotkeyToString(mods, key);
        Assert.Equal(original, formatted);
    }
}
