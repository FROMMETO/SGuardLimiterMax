using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SGuardLimiterMax.Services;

/// <summary>
/// Register / unregister a system-wide hotkey via user32.dll RegisterHotKey.
/// Hook WM_HOTKEY through a WPF HwndSource. Call Initialize once with
/// the main window handle, then call Register / Unregister as needed.
/// </summary>
public static class GlobalHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const int ModAlt = 0x0001;
    private const int ModControl = 0x0002;
    private const int ModShift = 0x0004;
    private const int ModWin = 0x0008;

    private static HwndSource? _source;
    private static int _hotkeyId;
    private static Action? _callback;
    private static bool _isRegistered;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Attach the window hook. Must be called once after the main window HWND is available.
    /// </summary>
    public static void Initialize(IntPtr hwnd)
    {
        if (_source != null) return;
        _source = HwndSource.FromHwnd(hwnd);
        _source.AddHook(WndProc);
    }

    /// <summary>
    /// Register a system-wide hotkey. Unregisters any previously registered hotkey first.
    /// Returns true on success.
    /// </summary>
    public static bool RegisterHotKey(ModifierKeys modifiers, Key key, Action callback)
    {
        UnregisterCurrentHotkey();
        if (_source == null) return false;

        uint mod = ModifierKeysToWin32(modifiers);
        uint vk  = (uint)KeyInterop.VirtualKeyFromKey(key);
        _hotkeyId = 1;
        _callback = callback;

        _isRegistered = RegisterHotKey(_source.Handle, _hotkeyId, mod, vk);
        return _isRegistered;
    }

    /// <summary>
    /// Unregister the current hotkey if one is active.
    /// </summary>
    public static void UnregisterCurrentHotkey()
    {
        if (!_isRegistered || _source == null) return;
        UnregisterHotKey(_source.Handle, _hotkeyId);
        _isRegistered = false;
        _callback = null;
    }

    /// <summary>
    /// Clean up the hook and hotkey. Call on app shutdown.
    /// </summary>
    public static void Shutdown()
    {
        UnregisterCurrentHotkey();
        if (_source != null)
        {
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }

    /// <summary>
    /// Parse a human-readable hotkey string like "Ctrl+Shift+U" into modifier + key.
    /// Returns true on success.
    /// </summary>
    public static bool TryParseHotkey(string? input, out ModifierKeys modifiers, out Key key)
    {
        modifiers = ModifierKeys.None;
        key = Key.None;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] parts = input.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false; // need at least modifier + key

        foreach (string part in parts)
        {
            string p = part.Trim();
            if (p.Equals("Ctrl",  StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Control;
            else if (p.Equals("Alt",   StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Alt;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Shift;
            else if (p.Equals("Win",   StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Windows;
            else
            {
                // Last part should be the key
                if (!Enum.TryParse(p, ignoreCase: true, out key))
                    return false;
            }
        }

        return key != Key.None && modifiers != ModifierKeys.None;
    }

    /// <summary>
    /// Format a modifier + key pair into a human-readable string like "Ctrl+Shift+U".
    /// </summary>
    public static string HotkeyToString(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))  parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))      parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))    parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows))  parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == _hotkeyId)
        {
            _callback?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static uint ModifierKeysToWin32(ModifierKeys mods)
    {
        uint flags = 0;
        if (mods.HasFlag(ModifierKeys.Alt))      flags |= ModAlt;
        if (mods.HasFlag(ModifierKeys.Control))  flags |= ModControl;
        if (mods.HasFlag(ModifierKeys.Shift))    flags |= ModShift;
        if (mods.HasFlag(ModifierKeys.Windows))  flags |= ModWin;
        return flags;
    }
}