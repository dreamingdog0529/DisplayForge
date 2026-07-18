namespace DisplayForge.Core.Models;

/// <summary>
/// Global hotkey binding. Extensible for future Next/Previous profile actions.
/// </summary>
public sealed class HotkeyBinding
{
    /// <summary>Kind of action this hotkey triggers.</summary>
    public HotkeyActionKind Kind { get; set; } = HotkeyActionKind.ApplyProfile;

    /// <summary>Modifier flags: Control, Alt, Shift, Win (comma-separated or flags).</summary>
    public HotkeyModifiers Modifiers { get; set; }

    /// <summary>Virtual-key name compatible with System.Windows.Input.Key (e.g. "D1", "F5").</summary>
    public string Key { get; set; } = string.Empty;

    public bool IsEmpty => string.IsNullOrWhiteSpace(Key) || Modifiers == HotkeyModifiers.None;

    public string ToDisplayString()
    {
        if (IsEmpty)
            return string.Empty;

        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(FormatKey(Key));
        return string.Join("+", parts);
    }

    public HotkeyBinding Clone() => new()
    {
        Kind = Kind,
        Modifiers = Modifiers,
        Key = Key
    };

    public bool EqualsBinding(HotkeyBinding? other)
    {
        if (other is null || IsEmpty || other.IsEmpty)
            return false;
        return Modifiers == other.Modifiers
               && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatKey(string key)
    {
        if (key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]))
            return key[1].ToString();
        if (key.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase))
            return "Num" + key["NumPad".Length..];
        return key;
    }
}

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public enum HotkeyActionKind
{
    ApplyProfile = 0,
    NextProfile = 1,
    PreviousProfile = 2
}
