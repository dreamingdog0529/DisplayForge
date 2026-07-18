using DisplayForge.Core.Models;

namespace DisplayForge.Core.Tests;

public class HotkeyBindingTests
{
    [Fact]
    public void ToDisplayString_FormatsNicely()
    {
        var hk = new HotkeyBinding
        {
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            Key = "F5"
        };
        Assert.Equal("Ctrl+Alt+Shift+F5", hk.ToDisplayString());
    }

    [Fact]
    public void EqualsBinding_IsCaseInsensitiveOnKey()
    {
        var a = new HotkeyBinding { Modifiers = HotkeyModifiers.Control, Key = "D1" };
        var b = new HotkeyBinding { Modifiers = HotkeyModifiers.Control, Key = "d1" };
        Assert.True(a.EqualsBinding(b));
    }

    [Fact]
    public void IsEmpty_WhenNoKey()
    {
        Assert.True(new HotkeyBinding().IsEmpty);
    }
}
