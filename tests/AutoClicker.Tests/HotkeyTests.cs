using AutoClicker.Core;

namespace AutoClicker.Tests;

public class HotkeyTests
{
    [Theory]
    [InlineData("F6")]
    [InlineData("XButton1")]
    [InlineData("XButton2")]
    [InlineData("A")]
    public void Hotkey_roundtrips(string s)
    {
        Assert.True(Hotkey.TryParse(s, out var h));
        Assert.Equal(s, h.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("XButton3")]
    [InlineData("banana")]
    public void Hotkey_rejects_invalid(string s) => Assert.False(Hotkey.TryParse(s, out _));

    [Fact]
    public void Default_is_F6() => Assert.Equal(new Hotkey(HotkeyKind.Keyboard, 0x75), Hotkey.Default);
}
