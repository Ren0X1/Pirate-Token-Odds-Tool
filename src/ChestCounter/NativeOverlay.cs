using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ChestCounter;

/// <summary>
/// Small always-on-top counter window drawn with plain Win32/GDI (no WinForms needed).
/// Left-drag to move it, right-click to hide it (Ctrl+Shift+O or the counter's menu shows it again).
/// Visible over Fortnite when the game runs in "windowed fullscreen" (not exclusive fullscreen).
/// </summary>
/// <summary>What the overlay shows. <see cref="Current"/> null = no match in progress.</summary>
/// <param name="Detail">Optional secondary text drawn small and grey after the value.</param>
public sealed record OverlayRow(string Label, string Value, bool Treasure = false, string? Detail = null);

/// <param name="Current">Right counter (match in progress, or last match); null = "—".</param>
/// <param name="CurrentLive">Right counter is the match in progress (green).</param>
/// <param name="Badge">Header badge ("● EN DIRECT" / "● LIVE", "● PARTIE EN COURS" / "● MATCH IN PROGRESS") or null.</param>
public sealed record OverlayView(string Total, string? Current, string CurrentCaption, bool CurrentLive, string? Badge, IReadOnlyList<OverlayRow> Rows, string Treasures = "0", string Session = "0", string? Last = null, string? Squad = null, string Pirate = "", double PirateChance = 0);

[SupportedOSPlatform("windows")]
public static class NativeOverlay
{
    private static Thread? _thread;
    private static IntPtr _hwnd;
    private static volatile OverlayView _view = new("0", null, OverlayModel.LastMatchCaption, false, null, Array.Empty<OverlayRow>());

    // Layout (computed by Relayout, used by Paint; both on the overlay thread).
    private static int _headerH, _bigH, _captionH, _rowH, _col1W, _col2W, _labelW;
    private static IntPtr _fontHeader, _fontCaption, _fontLabel;
    private static IntPtr _brushBorder, _brushLine;
    private static Action<int, int>? _onMoved;
    private static Action<bool>? _onLockChanged;
    private static volatile bool _locked;
    private static bool _offscreen;
    private static int _stableW;
    private static RECT _lockRect; // lock button, client coordinates
    private static IntPtr _fontIcon;
    private static WndProcDelegate? _wndProc; // keep the delegate alive
    private static IntPtr _fontBig, _fontSmall, _brushBg;
    private static float _scale = 1f;    // display scaling × size chosen by the player
    private static float _dpiScale = 1f; // display scaling only
    private static volatile int _sizePercent = 100;
    private static volatile int _opacityPercent = 100;

    /// <summary>"hud" (figures only, under the minimap, like the game's HUD) or "classic" (dark card).</summary>
    private static volatile string _style = "jet";
    private static IntPtr _fontHudBig, _fontHudSmall;
    private static int _hudBigH, _hudSmallH;

    /// <summary>Pixels of this color are fully transparent (styles without a background). Almost black, so that the
    /// anti-aliased edge of the black outline around the text blends into it without any colored fringe.</summary>
    private static readonly uint KeyColor = 0x00010001;

    public static string[] Styles => new[] { "jet", "hud", "classic" };

    /// <summary>Changes the look of the overlay at once.</summary>
    public static void SetStyle(string style)
    {
        _style = Array.IndexOf(Styles, style) >= 0 ? style : "jet";
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_APPEARANCE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private static bool Keyed => _style != "classic";

    private const int MinWidth = 210, Pad = 14;

    /// <summary>Size and opacity of the overlay (percent). Applied at once when the overlay is shown.</summary>
    public static void SetAppearance(int sizePercent, int opacityPercent)
    {
        _sizePercent = OverlayAppearance.ClampSize(sizePercent);
        _opacityPercent = OverlayAppearance.ClampOpacity(opacityPercent);
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_APPEARANCE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private static byte Alpha => (byte)Math.Round(_opacityPercent * 255 / 100.0);

    public static bool IsRunning => _hwnd != IntPtr.Zero;

    /// <summary>True when the overlay window is shown (not hidden with the shortcut or the menu).</summary>
    public static bool IsVisible => _hwnd != IntPtr.Zero && IsWindowVisible(_hwnd);

    /// <summary>Closes the overlay window and its thread (overlay turned off in the settings). Start() can open it again.</summary>
    public static void Stop()
    {
        var thread = _thread;
        if (thread == null)
        {
            return;
        }
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_STOP, IntPtr.Zero, IntPtr.Zero);
        }
        thread.Join(TimeSpan.FromSeconds(3));
    }

    /// <summary>True when the global Ctrl+Shift+O shortcut could be registered.</summary>
    public static bool HotkeyRegistered { get; private set; }

    /// <summary>True when the global Ctrl+Shift+L shortcut (lock/unlock) could be registered.</summary>
    public static bool LockHotkeyRegistered { get; private set; }

    /// <summary>Locked = click-through: the mouse goes through the overlay and it cannot be moved.</summary>
    public static bool IsLocked => _locked;

    public static void Start(int? x, int? y, bool locked, Action<int, int> onMoved, Action<bool> onLockChanged)
    {
        if (_thread != null)
        {
            return;
        }

        _onMoved = onMoved;
        _offscreen = x <= -10000; // --overlay-preview: never brought back on screen
        _onLockChanged = onLockChanged;
        _locked = locked;
        var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            try
            {
                Run(x, y, ready);
            }
            catch (Exception ex)
            {
                Console.WriteLine(L.T($"Overlay indisponible : {ex.Message}", $"Overlay unavailable: {ex.Message}", $"Overlay no disponible: {ex.Message}"));
                ready.Set();
            }
            finally
            {
                _hwnd = IntPtr.Zero;
                HotkeyRegistered = LockHotkeyRegistered = false;
                _thread = null;
            }
        })
        {
            IsBackground = true,
            Name = "Overlay",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
    }

    /// <summary>Replaces what is shown. The window resizes itself to fit.</summary>
    public static void Update(OverlayView view)
    {
        _view = view;
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_RELAYOUT, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>Saves the overlay as it is drawn to a PNG file (works off-screen). Used by --overlay-preview.</summary>
    public static bool Snapshot(string path)
    {
        if (_style == "jet" && _jetFrame is { } frame)
        {
            lock (frame)
            {
                frame.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            return true;
        }
        if (_hwnd == IntPtr.Zero || !GetWindowRect(_hwnd, out var r))
        {
            return false;
        }
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var bmp = CreateCompatibleBitmap(screen, w, h);
        var old = SelectObject(mem, bmp);
        var ok = PrintWindow(_hwnd, mem, 1 /* PW_CLIENTONLY: WM_PRINT -> WM_PRINTCLIENT */);
        SelectObject(mem, old);
        var bi = new BITMAPINFOHEADER { biSize = 40, biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32 };
        var bgra = new byte[w * h * 4];
        GetDIBits(mem, bmp, 0, (uint)h, bgra, ref bi, 0);
        DeleteObject(bmp);
        DeleteDC(mem);
        ReleaseDC(IntPtr.Zero, screen);
        Png.Write(path, w, h, bgra);
        return ok;
    }

    public static void ToggleLock()
    {
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_LOCK, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public static void Toggle()
    {
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_TOGGLE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private static void Run(int? x, int? y, ManualResetEventSlim ready)
    {
        try { SetProcessDPIAware(); } catch { /* old Windows */ }

        var screenDc = GetDC(IntPtr.Zero);
        _dpiScale = GetDeviceCaps(screenDc, LOGPIXELSY) / 96f;
        ReleaseDC(IntPtr.Zero, screenDc);

        int S(int v) => (int)Math.Round(v * _scale);

        CreateFonts();
        _brushBg = CreateSolidBrush(Rgb(12, 17, 28));
        _brushBorder = CreateSolidBrush(Rgb(36, 50, 76));
        _brushLine = CreateSolidBrush(Rgb(44, 44, 52));

        var hInstance = GetModuleHandleW(null);
        _wndProc = WndProc;
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            hCursor = LoadCursorW(IntPtr.Zero, IDC_SIZEALL),
            hbrBackground = _brushBg,
            lpszClassName = "ChestCounterOverlay",
        };
        // Already registered when the overlay is turned off and on again in the same run (ERROR_CLASS_ALREADY_EXISTS).
        if (RegisterClassExW(ref wc) == 0 && Marshal.GetLastWin32Error() != 1410)
        {
            throw new InvalidOperationException("RegisterClassEx failed");
        }

        int w = S(MinWidth), h = S(90);
        var posX = x ?? S(24);
        var posY = y ?? S(24);

        _hwnd = CreateWindowExW(
            // Opaque (no WS_EX_LAYERED alpha): nothing for the compositor to blend every frame.
            WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
            wc.lpszClassName, L.T("Coffres", "Chests", "Cofres"), WS_POPUP,

            posX, posY, w, h, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("CreateWindowEx failed");
        }

        ApplyLock(_hwnd, _locked, notify: false);
        Relayout(_hwnd);
        ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
        // No timer at all: the thread sleeps in GetMessage and only wakes up when the counter changes,
        // when you drag/hide the window or press the shortcut.
        // Global shortcut, works even when the game has the focus.
        HotkeyRegistered = RegisterHotKey(_hwnd, 1, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_O);
        LockHotkeyRegistered = RegisterHotKey(_hwnd, 2, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_L);
        ready.Set();

        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    /// <summary>(Re)creates the fonts for the current display scaling and chosen size.</summary>
    private static void CreateFonts()
    {
        foreach (var old in new[] { _fontBig, _fontSmall, _fontHeader, _fontCaption, _fontLabel, _fontIcon, _fontHudBig, _fontHudSmall })
        {
            if (old != IntPtr.Zero) DeleteObject(old);
        }
        _scale = _dpiScale * _sizePercent / 100f;
        int S(int v) => (int)Math.Round(v * _scale);
        _fontBig = CreateFontW(-S(34), 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 5, 0, "Bahnschrift SemiBold");
        _fontSmall = CreateFontW(-S(14), 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
        _fontHeader = CreateFontW(-S(11), 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
        _fontCaption = CreateFontW(-S(11), 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
        _fontLabel = CreateFontW(-S(13), 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
        _fontIcon = CreateFontW(-S(13), 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe MDL2 Assets");
        // Game-like style: condensed italic, grayscale anti-aliasing (ClearType would leave colored edges once the
        // background is made transparent).
        _fontHudBig = CreateFontW(-S(36), 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 4, 0, "Bahnschrift SemiBold");
        _fontHudSmall = CreateFontW(-S(19), 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 4, 0, "Bahnschrift");
        if (_fontEmoji != IntPtr.Zero) DeleteObject(_fontEmoji);
        _fontEmoji = CreateFontW(-S(15), 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 4, 0, "Segoe UI Emoji");
        CreateJetFonts();
    }

    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_NCHITTEST:
            {
                // Lock button = normal client area (clickable); everywhere else = caption (drag to move).
                // When locked the window is WS_EX_TRANSPARENT, so it never even receives this message.
                GetWindowRect(hwnd, out var wr);
                var px = (short)((long)lParam & 0xFFFF) - wr.Left;
                var py = (short)(((long)lParam >> 16) & 0xFFFF) - wr.Top;
                var onButton = px >= _lockRect.Left && px < _lockRect.Right && py >= _lockRect.Top && py < _lockRect.Bottom;
                return (IntPtr)(onButton ? HTCLIENT : HTCAPTION);
            }

            case WM_SETCURSOR:
                if (((long)lParam & 0xFFFF) == HTCLIENT)
                {
                    SetCursor(LoadCursorW(IntPtr.Zero, IDC_HAND));
                    return (IntPtr)1;
                }
                break;

            case WM_LBUTTONUP:
                ApplyLock(hwnd, true, notify: true);
                return IntPtr.Zero;

            case WM_APP_LOCK:
                ApplyLock(hwnd, !_locked, notify: true);
                return IntPtr.Zero;

            case WM_NCRBUTTONUP:
                SetVisible(hwnd, false);
                return IntPtr.Zero;

            case WM_HOTKEY:
                if ((long)wParam == 2)
                {
                    ApplyLock(hwnd, !_locked, notify: true);
                }
                else
                {
                    SetVisible(hwnd, !IsWindowVisible(hwnd));
                }
                return IntPtr.Zero;

            case WM_EXITSIZEMOVE:
                if (_anchor != "free")
                {
                    Relayout(hwnd); // anchored: back to its place
                }
                else if (GetWindowRect(hwnd, out var r))
                {
                    _onMoved?.Invoke(r.Left, r.Top);
                }
                return IntPtr.Zero;

            case WM_TIMER:
                if ((ulong)wParam == JetTimerId && _style == "jet")
                {
                    PresentJet(hwnd);
                }
                return IntPtr.Zero;

            case WM_APP_STOP:
                DestroyWindow(hwnd);
                return IntPtr.Zero;

            case WM_APP_TOGGLE:
                SetVisible(hwnd, !IsWindowVisible(hwnd));
                return IntPtr.Zero;

            case WM_APP_RELAYOUT:
                Relayout(hwnd);
                return IntPtr.Zero;

            case WM_APP_APPEARANCE:
                CreateFonts();
                _stableW = 0; // the width may shrink when the overlay gets smaller
                ApplyLock(hwnd, _locked, notify: false); // also applies the opacity
                Relayout(hwnd);
                return IntPtr.Zero;

            case WM_PAINT:
                Paint(hwnd);
                return IntPtr.Zero;

            case WM_PRINTCLIENT: // snapshot (--overlay-preview): draw into the given DC
                PaintTo(hwnd, wParam);
                return IntPtr.Zero;

            case WM_DESTROY:
                UnregisterHotKey(hwnd, 1);
                UnregisterHotKey(hwnd, 2);
                PostQuitMessage(0);
                return IntPtr.Zero;
        }
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Locked: click-through (WS_EX_LAYERED + WS_EX_TRANSPARENT). Unlocked: normal window. Layered also when the
    /// player made it see-through; a fully opaque unlocked overlay stays a plain window (nothing to blend).
    /// </summary>
    private static void ApplyLock(IntPtr hwnd, bool locked, bool notify)
    {
        _locked = locked;
        var alpha = Alpha;
        var jet = _style == "jet";
        var layered = jet || locked || alpha < 255 || Keyed;
        var ex = (long)GetWindowLongPtrW(hwnd, GWL_EXSTYLE);
        ex = locked ? ex | WS_EX_TRANSPARENT : ex & ~(long)WS_EX_TRANSPARENT;
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, (IntPtr)(ex & ~(long)WS_EX_LAYERED));
        ex = layered ? ex | WS_EX_LAYERED : ex & ~(long)WS_EX_LAYERED;
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, (IntPtr)ex);
        if (layered && !jet)
        {
            // Required for a layered window to be drawn; the key color makes the background of the game-like styles invisible.
            SetLayeredWindowAttributes(hwnd, KeyColor, alpha, Keyed ? LWA_ALPHA | LWA_COLORKEY : LWA_ALPHA);
        }
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        if (jet)
        {
            RelayoutJet(hwnd); // the plate and the padlock of the unlocked overlay are part of the picture
        }
        else
        {
            KillTimer(hwnd, (UIntPtr)JetTimerId);
            InvalidateRect(hwnd, IntPtr.Zero, true);
        }

        if (notify)
        {
            _onLockChanged?.Invoke(locked);
            Console.WriteLine(locked
                ? L.T($"Overlay verrouillé : la souris passe au travers. {(LockHotkeyRegistered ? "Ctrl+Maj+L" : "« Déverrouiller l'overlay » dans le compteur")} pour le déverrouiller.",
                      $"Overlay locked: the mouse goes through it. {(LockHotkeyRegistered ? "Ctrl+Shift+L" : "\"Unlock the overlay\" in the counter")} to unlock it.",
                      $"Overlay bloqueado: el ratón lo atraviesa. {(LockHotkeyRegistered ? "Ctrl+Mayús+L" : "«Desbloquear el overlay» en el contador")} para desbloquearlo.")
                : L.T("Overlay déverrouillé : clic gauche maintenu pour le déplacer, cadenas pour le reverrouiller.",
                      "Overlay unlocked: hold the left mouse button to move it, click the padlock to lock it again.",
                      "Overlay desbloqueado: arrástralo con el clic izquierdo; el candado lo vuelve a bloquear."));
        }
    }

    private static void SetVisible(IntPtr hwnd, bool visible)
    {
        if (visible)
        {
            ShowWindow(hwnd, SW_SHOWNOACTIVATE);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            UpdateJetTimer(hwnd);
        }
        else
        {
            ShowWindow(hwnd, SW_HIDE);
            KillTimer(hwnd, (UIntPtr)JetTimerId);
            Console.WriteLine(HotkeyRegistered
                ? L.T("Overlay masqué : Ctrl+Maj+O (n'importe où) pour le réafficher.", "Overlay hidden: Ctrl+Shift+O (anywhere) to show it again.", "Overlay oculto: Ctrl+Mayús+O (desde cualquier sitio) para volver a mostrarlo.")
                : L.T("Overlay masqué : « Afficher l'overlay » dans le compteur pour le réafficher.", "Overlay hidden: \"Show the overlay\" in the counter to show it again.", "Overlay oculto: «Mostrar el overlay» en el contador para volver a mostrarlo."));
        }
    }

    private static int TextWidth(IntPtr hdc, IntPtr font, string text, out int height)
    {
        SelectObject(hdc, font);
        var r = new RECT();
        DrawTextW(hdc, text.Length == 0 ? " " : text, -1, ref r, DT_CALCRECT | DT_SINGLELINE | DT_NOPREFIX);
        height = r.Bottom - r.Top;
        return r.Right - r.Left;
    }

    // ---- Shared palette -------------------------------------------------------------------------------------------
    private static readonly uint Ink = Rgb(7, 10, 18);
    private static readonly uint Sea = Rgb(52, 230, 196);     // pirate chance
    private static readonly uint Gold = Rgb(255, 201, 64);    // chests
    private static readonly uint Live = Rgb(255, 77, 94);     // match in progress
    private static readonly uint Soft = Rgb(148, 160, 184);   // captions
    private static readonly uint White = Rgb(240, 244, 250);

    private static int Sc(double v) => (int)Math.Round(v * _scale);

    private static string PirateCaption => L.T("BOUTIQUE PIRATE", "PIRATE SHOP", "TIENDA PIRATA");

    /// <summary>Bottom line of both styles: the match in progress, or the last match.</summary>
    private static (string Text, bool Live) StateLine(OverlayView v)
    {
        if (v.CurrentLive && v.Current is { } cur && cur != "…")
        {
            return ($"{L.T("En direct", "Live", "En directo")}  +{cur}", true);
        }
        if (v.CurrentLive)
        {
            return (L.T("En partie", "In a match", "En partida"), true);
        }
        return ($"{L.T("Dernière", "Last", "Última")}  {v.Last ?? "—"}", false);
    }

    /// <summary>Keeps the window on its screen; on the right half of the screen it grows to the left.</summary>
    private static void Place(IntPtr hwnd, int w, int h)
    {
        if (Anchored(w, h) is { } at)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, at.X, at.Y, w, h, SWP_NOACTIVATE);
            return;
        }
        GetWindowRect(hwnd, out var cur);
        int nx = cur.Left, ny = cur.Top;
        var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!_offscreen && GetMonitorInfoW(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref mi))
        {
            var work = mi.rcWork;
            if (cur.Right - cur.Left > 0 && (cur.Left + cur.Right) / 2 > (work.Left + work.Right) / 2)
            {
                nx = cur.Right - w;
            }
            nx = Math.Max(work.Left, Math.Min(nx, work.Right - w));
            ny = Math.Max(work.Top, Math.Min(ny, work.Bottom - h));
        }
        SetWindowPos(hwnd, HWND_TOPMOST, nx, ny, w, h, SWP_NOACTIVATE);
        if (nx != cur.Left || ny != cur.Top)
        {
            _onMoved?.Invoke(nx, ny);
        }
    }

    // ---- "classic" style: a small dark card ------------------------------------------------------------------------
    //
    //   PIRATE SHOP                 [lock]
    //   0.727 %
    //   [=====-----------------------]
    //   [chest] 1 000          Last  14

    private static int _footH;

    private static void Relayout(IntPtr hwnd)
    {
        if (_style == "jet")
        {
            RelayoutJet(hwnd);
            return;
        }
        if (Keyed)
        {
            RelayoutStyled(hwnd);
            return;
        }
        var v = _view;
        var hdc = GetDC(hwnd);
        try
        {
            var pad = Sc(Pad);
            var headerW = TextWidth(hdc, _fontHeader, PirateCaption, out _headerH) + Sc(30);
            var bigW = TextWidth(hdc, _fontBig, v.Pirate, out _bigH);
            var state = StateLine(v);
            var footW = Sc(20) + TextWidth(hdc, _fontSmall, v.Total, out _footH) + Sc(18) + TextWidth(hdc, _fontSmall, state.Text, out _) + Sc(12);
            var w = Math.Max(_stableW, Math.Max(Sc(MinWidth), Math.Max(headerW, Math.Max(bigW + Sc(52), footW)) + 2 * pad));
            _stableW = w;
            var h = pad + _headerH + Sc(2) + _bigH + Sc(4) + Sc(5) + Sc(10) + _footH + pad - Sc(2);
            _lockRect = new RECT { Left = w - pad - Sc(18), Top = Sc(6), Right = w - Sc(6), Bottom = pad + _headerH + Sc(4) };
            Place(hwnd, w, h);
            SetWindowRgn(hwnd, CreateRoundRectRgn(0, 0, w + 1, h + 1, Sc(16), Sc(16)), true);
        }
        finally
        {
            ReleaseDC(hwnd, hdc);
        }
        InvalidateRect(hwnd, IntPtr.Zero, true);
        UpdateWindow(hwnd);
    }

    private static void Text(IntPtr hdc, IntPtr font, uint color, string text, int left, int top, int right, int bottom, uint align = DT_LEFT)
    {
        SelectObject(hdc, font);
        SetTextColor(hdc, color);
        var r = new RECT { Left = left, Top = top, Right = right, Bottom = bottom };
        DrawTextW(hdc, text, -1, ref r, DT_SINGLELINE | DT_NOPREFIX | align);
    }

    private static void Paint(IntPtr hwnd)
    {
        var hdc = BeginPaint(hwnd, out var ps);
        try
        {
            PaintTo(hwnd, hdc);
        }
        finally
        {
            EndPaint(hwnd, ref ps);
        }
    }

    private static void Fill(IntPtr hdc, int l, int t, int r, int b, uint color)
    {
        var brush = CreateSolidBrush(color);
        var rc = new RECT { Left = l, Top = t, Right = r, Bottom = b };
        FillRect(hdc, ref rc, brush);
        DeleteObject(brush);
    }

    /// <summary>Progress bar of the cumulative chance (a minimum sliver so that it is always visible).</summary>
    private static void Meter(IntPtr hdc, int l, int t, int r, int height, double p, uint track)
    {
        var pen = CreatePen(0, 1, track);
        var oldPen = SelectObject(hdc, pen);
        var tb = CreateSolidBrush(track);
        var old = SelectObject(hdc, tb);
        RoundRect(hdc, l, t, r, t + height, height, height);
        var fw = Math.Max(height, (int)Math.Round((r - l) * Math.Clamp(p, 0, 1)));
        var fb = CreateSolidBrush(Sea);
        var fp = CreatePen(0, 1, Sea);
        SelectObject(hdc, fb);
        SelectObject(hdc, fp);
        RoundRect(hdc, l, t, l + fw, t + height, height, height);
        SelectObject(hdc, old);
        SelectObject(hdc, oldPen);
        DeleteObject(tb);
        DeleteObject(fb);
        DeleteObject(fp);
        DeleteObject(pen);
    }

    private static void PaintTo(IntPtr hwnd, IntPtr hdc)
    {
        if (_style == "jet")
        {
            PaintJet(hwnd, hdc);
            return;
        }
        if (Keyed)
        {
            PaintStyled(hwnd, hdc);
            return;
        }
        var v = _view;
        GetClientRect(hwnd, out var rc);
        FillRect(hdc, ref rc, _brushBg);
        SetBkMode(hdc, TRANSPARENT);
        var pad = Sc(Pad);
        var x = pad;
        var right = rc.Right - pad;
        var y = pad - Sc(2);

        var border = CreateRoundRectRgn(0, 0, rc.Right + 1, rc.Bottom + 1, Sc(16), Sc(16));
        FrameRgn(hdc, border, _brushBorder, 1, 1);
        DeleteObject(border);

        // Header + lock button (Segoe MDL2 Assets: E785 = open padlock, E72E = closed padlock).
        Text(hdc, _fontHeader, Soft, PirateCaption, x, y, right, y + _headerH);
        SelectObject(hdc, _fontIcon);
        SetTextColor(hdc, _locked ? Rgb(70, 80, 100) : White);
        var lr = _lockRect;
        DrawTextW(hdc, _locked ? "" : "", -1, ref lr, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
        y += _headerH + Sc(2);

        Text(hdc, _fontBig, Sea, v.Pirate, x - Sc(1), y, right + Sc(4), y + _bigH);
        Coin(hdc, right - Sc(40), y + (_bigH - Sc(40)) / 2, Sc(40));
        y += _bigH + Sc(4);
        Meter(hdc, x, y, right, Sc(5), v.PirateChance, Rgb(32, 42, 62));
        y += Sc(5) + Sc(10);

        EmojiChest(hdc, x - Sc(1), y, y + _footH, Gold);
        Text(hdc, _fontSmall, White, v.Total, x + Sc(20), y, right, y + _footH);
        var state = StateLine(v);
        Text(hdc, _fontSmall, state.Live ? Live : Soft, state.Text, x, y, right, y + _footH, DT_RIGHT);
        if (state.Live)
        {
            var sw = TextWidth(hdc, _fontSmall, state.Text, out _);
            Disc(hdc, right - sw - Sc(12), y + _footH / 2 - Sc(3), Sc(7), Live, Live, 1);
        }
    }

    // ---- "hud" style: figures only, outlined like the game's own HUD, right-aligned under the minimap -------------
    //
    //        [coin] 0.727 %       chance of the Pirate Shop Access (sea green)
    //       [chest] 1 000         total chests
    //       ● Live  +12           match in progress / last match

    private static void RelayoutStyled(IntPtr hwnd)
    {
        var v = _view;
        var hdc = GetDC(hwnd);
        try
        {
            var pad = Sc(8);
            var lockW = Sc(22);
            var pirW = TextWidth(hdc, _fontHudBig, v.Pirate, out _hudBigH);
            var totW = TextWidth(hdc, _fontHudSmall, v.Total, out _hudSmallH) + Sc(4);
            var stateW = TextWidth(hdc, _fontHudSmall, StateLine(v).Text, out _);
            var content = Math.Max(Sc(44) + pirW, Math.Max(Sc(30) + totW, Sc(16) + stateW));
            var w = Math.Max(_stableW, lockW + pad + content + pad + Sc(14));
            _stableW = w;
            var h = pad + _hudBigH + 2 * (_hudSmallH + Sc(2)) + pad;
            _lockRect = new RECT { Left = Sc(2), Top = 0, Right = lockW, Bottom = h };
            Place(hwnd, w, h);
            SetWindowRgn(hwnd, IntPtr.Zero, true); // plain rectangle: the corners are made invisible by the key color
        }
        finally
        {
            ReleaseDC(hwnd, hdc);
        }
        InvalidateRect(hwnd, IntPtr.Zero, true);
        UpdateWindow(hwnd);
    }

    /// <summary>Text outlined in near black, like the game's own HUD figures (readable on any background).</summary>
    private static void Outlined(IntPtr hdc, IntPtr font, uint color, string text, int left, int top, int right, int bottom, uint align, bool thin = false)
    {
        SelectObject(hdc, font);
        var o = thin ? 1 : Math.Max(1, Sc(1.6));
        SetTextColor(hdc, Ink);
        for (var dx = -o; dx <= o; dx++)
        {
            for (var dy = -o; dy <= o + 1; dy++)
            {
                if (Math.Abs(dx) != o && Math.Abs(dy) < o) continue; // only the ring (and one pixel of drop shadow)
                var r = new RECT { Left = left + dx, Top = top + dy, Right = right + dx, Bottom = bottom + dy };
                DrawTextW(hdc, text, -1, ref r, DT_SINGLELINE | DT_NOPREFIX | DT_NOCLIP | align);
            }
        }
        SetTextColor(hdc, color);
        var rc = new RECT { Left = left, Top = top, Right = right, Bottom = bottom };
        DrawTextW(hdc, text, -1, ref rc, DT_SINGLELINE | DT_NOPREFIX | DT_NOCLIP | align);
    }

    /// <summary>Chest symbol: the emoji font's glyph (one color: GDI draws emoji outlines, not their colors).</summary>
    private const string ChestGlyph = "🧰";
    private static IntPtr _fontEmoji;

    private static int EmojiChest(IntPtr hdc, int x, int top, int bottom, uint color)
    {
        var w = TextWidth(hdc, _fontEmoji, ChestGlyph, out var gh);
        var y = top + (bottom - top - gh) / 2;
        Outlined(hdc, _fontEmoji, color, ChestGlyph, x, y, x + w + Sc(4), y + gh, DT_LEFT, thin: true);
        return w;
    }

    /// <summary>A treasure chest made of three shapes: body, rounded lid, latch.</summary>
    private static void Chest(IntPtr hdc, int x, int y, int w, int h, uint body, uint lid, uint latch, uint outline)
    {
        var pen = CreatePen(0, Math.Max(1, Sc(1.2)), outline);
        var oldPen = SelectObject(hdc, pen);
        var lidH = (int)(h * 0.42);
        var bLid = CreateSolidBrush(lid);
        var old = SelectObject(hdc, bLid);
        Chord(hdc, x, y, x + w, y + 2 * lidH, x + w, y + lidH, x, y + lidH);
        var bBody = CreateSolidBrush(body);
        SelectObject(hdc, bBody);
        RoundRect(hdc, x, y + lidH, x + w, y + h, Sc(3), Sc(3));
        var bLatch = CreateSolidBrush(latch);
        SelectObject(hdc, bLatch);
        var lw = Math.Max(2, w / 5);
        Rectangle(hdc, x + (w - lw) / 2, y + lidH - Sc(1), x + (w + lw) / 2, y + lidH + Math.Max(3, h / 4));
        SelectObject(hdc, old);
        SelectObject(hdc, oldPen);
        DeleteObject(bLid);
        DeleteObject(bBody);
        DeleteObject(bLatch);
        DeleteObject(pen);
    }

    /// <summary>The Pirate Shop Access token picture (embedded PNG), drawn with its transparency.</summary>
    private static readonly System.Drawing.Bitmap? TokenBitmap = Brand.TokenPng is { } png ? new System.Drawing.Bitmap(new MemoryStream(png)) : null;

    /// <summary>The token; without the picture, a sea-green doubloon with a gold center.</summary>
    private static void Coin(IntPtr hdc, int x, int y, int d)
    {
        if (TokenBitmap != null)
        {
            using var g = System.Drawing.Graphics.FromHdc(hdc);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(TokenBitmap, x, y, d, d);
            return;
        }
        Disc(hdc, x, y, d, Sea, Ink, Math.Max(1, Sc(1.6)));
        var inner = (int)(d * 0.52);
        var off = (d - inner) / 2;
        Disc(hdc, x + off, y + off, inner, Gold, Ink, Math.Max(1, Sc(1.2)));
    }

    private static void Disc(IntPtr hdc, int x, int y, int d, uint fill, uint outline, int penW)
    {
        var pen = CreatePen(0, penW, outline);
        var brush = CreateSolidBrush(fill);
        var oldPen = SelectObject(hdc, pen);
        var old = SelectObject(hdc, brush);
        Ellipse(hdc, x, y, x + d, y + d);
        SelectObject(hdc, old);
        SelectObject(hdc, oldPen);
        DeleteObject(brush);
        DeleteObject(pen);
    }

    private static void PaintStyled(IntPtr hwnd, IntPtr hdc)
    {
        GetClientRect(hwnd, out var rc);
        var key = CreateSolidBrush(KeyColor);
        FillRect(hdc, ref rc, key);
        DeleteObject(key);
        SetBkMode(hdc, TRANSPARENT);
        var v = _view;
        var pad = Sc(8);
        var right = rc.Right - pad - Sc(12); // italic glyphs and their outline lean past the measured width

        // Unlocked: a dark backdrop to grab it, and the padlock (locked, only the figures remain, mouse goes through).
        if (!_locked)
        {
            var pen = CreatePen(0, 1, Rgb(40, 54, 80));
            var brush = CreateSolidBrush(Rgb(14, 20, 32));
            var oldPen = SelectObject(hdc, pen);
            var old = SelectObject(hdc, brush);
            RoundRect(hdc, 0, 0, rc.Right, rc.Bottom, Sc(12), Sc(12));
            SelectObject(hdc, old);
            SelectObject(hdc, oldPen);
            DeleteObject(brush);
            DeleteObject(pen);
            SelectObject(hdc, _fontIcon);
            SetTextColor(hdc, White);
            var lr = _lockRect;
            DrawTextW(hdc, "", -1, ref lr, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
        }

        var left = _lockRect.Right;
        var y = pad;

        // Pirate Shop chance, big, with the coin.
        var pirW = TextWidth(hdc, _fontHudBig, v.Pirate, out _);
        Outlined(hdc, _fontHudBig, Sea, v.Pirate, left, y, right, y + _hudBigH, DT_RIGHT);
        var cd = Sc(34);
        Coin(hdc, right - pirW - Sc(8) - cd, y + (_hudBigH - cd) / 2, cd);
        y += _hudBigH + Sc(2);

        // Total chests.
        var rowH = _hudSmallH;
        var totW = TextWidth(hdc, _fontHudSmall, v.Total, out _);
        Outlined(hdc, _fontHudSmall, White, v.Total, left, y, right, y + rowH, DT_RIGHT, thin: true);
        EmojiChest(hdc, right - totW - Sc(8) - Sc(22), y, y + rowH, Gold);
        y += rowH + Sc(2);

        // Match in progress / last match.
        var state = StateLine(v);
        var sw = TextWidth(hdc, _fontHudSmall, state.Text, out _);
        Outlined(hdc, _fontHudSmall, state.Live ? Live : Rgb(255, 233, 166), state.Text, left, y, right, y + rowH, DT_RIGHT, thin: true);
        if (state.Live)
        {
            Disc(hdc, right - sw - Sc(13), y + rowH / 2 - Sc(4), Sc(9), Live, Ink, 1);
        }
    }

    // ---- "jet" style: a fighter head-up display (green stroke symbology, corner brackets, a scale with a caret) -----
    //
    //   ┌                          ┐
    //    PIRATE SHOP        1:137500
    //       >   3,175 %   <
    //    |----|----|-^--|----|----|
    //    CHT 4.436            LST 23
    //   └                          ┘

    private static uint Phosphor = Rgb(90, 255, 150);
    private static System.Drawing.Bitmap? _jetToken = Brand.TintedToken(Brand.DefaultHudColor);
    private static volatile string _hudColor = Brand.DefaultHudColor;
    private static string _appliedColor = Brand.DefaultHudColor;

    /// <summary>Color of the fighter-jet HUD (lines, text and the token), "#RRGGBB".</summary>
    public static void SetHudColor(string hex)
    {
        _hudColor = Brand.NormalizeColor(hex);
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_APPEARANCE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private static void ApplyHudColor()
    {
        var hex = _hudColor;
        var (r, g, b) = Brand.ParseColor(hex);
        Phosphor = Rgb(r, g, b);
        if (hex != _appliedColor)
        {
            _appliedColor = hex;
            var old = _jetToken;
            _jetToken = Brand.TintedToken(hex);
            old?.Dispose();
        }
    }

    private static void DrawBitmap(IntPtr hdc, System.Drawing.Bitmap bmp, int x, int y, int d)
    {
        using var g = System.Drawing.Graphics.FromHdc(hdc);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.DrawImage(bmp, x, y, d, d);
    }
    private static readonly uint Caution = Rgb(255, 196, 64);
    private static IntPtr _fontJetBig, _fontJetSmall;
    private static int _jetBigH, _jetSmallH;

    private static void CreateJetFonts()
    {
        ApplyHudColor();
        foreach (var old in new[] { _fontJetBig, _fontJetSmall })
        {
            if (old != IntPtr.Zero) DeleteObject(old);
        }
        _fontJetBig = CreateFontW(-Sc(34), 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 4, 0, "Consolas");
        _fontJetSmall = CreateFontW(-Sc(14), 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 4, 0, "Consolas");
    }

    private static string JetTitle => L.T("BOUTIQUE PIRATE", "PIRATE SHOP", "TIENDA PIRATA");
    private static string JetOdds => "1:" + PirateOdds.OneIn.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private static string JetValue(OverlayView v) => v.Pirate.Replace(" ", "").Replace(" ", "");

    private static (string Left, string Right, bool Live) JetBottom(OverlayView v)
    {
        var left = v.Total;
        if (v.CurrentLive && v.Current is { } cur && cur != "…")
        {
            return (left, $"{L.T("DIRECT", "LIVE", "DIRECTO")} +{cur}", true);
        }
        if (v.CurrentLive)
        {
            return (left, L.T("EN PARTIE", "IN MATCH", "EN PARTIDA"), true);
        }
        return (left, $"{L.T("DER", "LST", "ULT")} {v.Last ?? "—"}", false);
    }

    // The jet style is drawn with GDI+ into a 32-bit bitmap and shown with UpdateLayeredWindow (per-pixel alpha):
    // that is what lets the strokes glow softly like a real head-up display over any scenery. The static picture is
    // drawn once per change (_jetBase); the animations (phosphor flicker, a scan sweep, a short glitch now and then)
    // only compose that picture again, 20 times a second, and only while they are on and the overlay is shown.

    private static System.Drawing.Bitmap? _jetBase, _jetFrame;
    private static volatile bool _animations = true;
    private static readonly Random Rng = new();
    private static readonly System.Diagnostics.Stopwatch AnimClock = System.Diagnostics.Stopwatch.StartNew();
    private static long _glitchUntil, _nextGlitch = 4000;
    private const int JetTimerId = 7;

    /// <summary>Animations of the jet HUD (flicker, scan sweep, glitch). Off: a still picture, no timer at all.</summary>
    public static void SetAnimations(bool on)
    {
        _animations = on;
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_APPEARANCE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private static float F(double v) => (float)(v * _scale);

    private static System.Drawing.Color C(uint rgb, int a = 255) =>
        System.Drawing.Color.FromArgb(a, (int)(rgb & 0xFF), (int)((rgb >> 8) & 0xFF), (int)((rgb >> 16) & 0xFF));

    private static System.Drawing.Drawing2D.GraphicsPath TextPath(string text, string family, bool bold, float px)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        try
        {
            using var ff = new System.Drawing.FontFamily(family);
            path.AddString(text, ff, (int)(bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular), px, System.Drawing.PointF.Empty, System.Drawing.StringFormat.GenericTypographic);
        }
        catch
        {
            // font missing: nothing drawn for that text
        }
        return path;
    }

    private static float PathWidth(System.Drawing.Drawing2D.GraphicsPath p) => p.PointCount == 0 ? 0 : p.GetBounds().Right;

    /// <summary>A dark halo for contrast, three soft glow strokes, then the crisp fill.</summary>
    private static void GlowFill(System.Drawing.Graphics g, System.Drawing.Drawing2D.GraphicsPath p, uint color)
    {
        if (p.PointCount == 0) return;
        using (var halo = new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 4, 8, 6), F(2.6)) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
        {
            g.DrawPath(halo, p);
        }
        foreach (var (w, a) in new[] { (7.0, 22), (4.6, 38), (2.6, 70) })
        {
            using var glow = new System.Drawing.Pen(C(color, a), F(w)) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
            g.DrawPath(glow, p);
        }
        using var fill = new System.Drawing.SolidBrush(C(color));
        g.FillPath(fill, p);
    }

    private static void GlowLine(System.Drawing.Graphics g, float x1, float y1, float x2, float y2, uint color)
    {
        using var p = new System.Drawing.Drawing2D.GraphicsPath();
        p.AddLine(x1, y1, x2, y2);
        foreach (var (w, a) in new[] { (3.8, 160), (8.0, 20), (5.2, 36), (3.2, 70) })
        {
            using var pen = new System.Drawing.Pen(a == 160 ? System.Drawing.Color.FromArgb(150, 4, 8, 6) : C(color, a), F(w))
            { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            g.DrawPath(pen, p);
        }
        using var core = new System.Drawing.Pen(C(color), F(1.6)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        g.DrawPath(core, p);
    }

    private static void At(System.Drawing.Drawing2D.GraphicsPath p, float x, float y)
    {
        using var m = new System.Drawing.Drawing2D.Matrix();
        m.Translate(x, y);
        p.Transform(m);
    }

    /// <summary>Draws the still picture of the jet HUD and sizes the window to it.</summary>
    private static void RelayoutJet(IntPtr hwnd)
    {
        var v = _view;
        var color = Phosphor;
        float small = F(14), big = F(34);
        using var title = TextPath(JetTitle, "Consolas", true, small);
        using var odds = TextPath(JetOdds, "Consolas", true, small);
        using var value = TextPath(JetValue(v), "Consolas", true, big);
        var b = JetBottom(v);
        using var chest = TextPath(ChestGlyph, "Segoe UI Emoji", false, F(15));
        using var bl = TextPath(b.Left, "Consolas", true, small);
        using var br = TextPath(b.Right, "Consolas", true, small);

        float pad = F(18), smallH = F(17), bigH = F(40), gap = F(4);
        var contentW = Math.Max(PathWidth(title) + F(24) + PathWidth(odds),
            Math.Max(PathWidth(value) + F(70) + 2 * F(46), F(24) + PathWidth(bl) + F(24) + PathWidth(br)));
        var w = Math.Max(_stableW, (int)Math.Ceiling(Math.Max(F(260), contentW + 2 * pad)));
        _stableW = w;
        var h = (int)Math.Ceiling(pad + smallH + gap + bigH + F(6) + F(16) + F(8) + smallH + pad);
        _lockRect = new RECT { Left = w - Sc(24), Top = 0, Right = w, Bottom = Sc(24) };

        var bmp = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.Clear(System.Drawing.Color.Transparent);

            if (!_locked)
            {
                // Unlocked: a dark plate to grab it, and the padlock.
                using var plate = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(215, 8, 14, 10));
                g.FillRectangle(plate, 0, 0, w, h);
                using var lockPath = TextPath("", "Segoe MDL2 Assets", false, F(13));
                At(lockPath, w - F(19), F(6));
                using var white = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230, 240, 244, 250));
                g.FillPath(white, lockPath);
            }

            // Corner brackets.
            float m = F(5), len = F(16), W = w, H = h;
            GlowLine(g, m, m, m + len, m, color); GlowLine(g, m, m, m, m + len, color);
            GlowLine(g, W - m, m, W - m - len, m, color); GlowLine(g, W - m, m, W - m, m + len, color);
            GlowLine(g, m, H - m, m + len, H - m, color); GlowLine(g, m, H - m, m, H - m - len, color);
            GlowLine(g, W - m, H - m, W - m - len, H - m, color); GlowLine(g, W - m, H - m, W - m, H - m - len, color);

            float left = pad, right = W - pad, y = pad;
            At(title, left, y);
            GlowFill(g, title, color);
            At(odds, right - PathWidth(odds), y);
            GlowFill(g, odds, color);
            y += smallH + gap;

            // The chance between two chevrons, the token on the left.
            var vw = PathWidth(value);
            var cx = W / 2;
            At(value, cx - vw / 2, y + (bigH - big) / 2 - F(2));
            GlowFill(g, value, color);
            var my = y + bigH / 2;
            float cv = F(7), lx = cx - vw / 2 - F(16), rx = cx + vw / 2 + F(16);
            GlowLine(g, lx - cv, my - cv, lx, my, color); GlowLine(g, lx - cv, my + cv, lx, my, color);
            GlowLine(g, rx + cv, my - cv, rx, my, color); GlowLine(g, rx + cv, my + cv, rx, my, color);
            if (_jetToken != null)
            {
                var td = F(40);
                using var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.28f });
                var gd = td + F(8);
                g.DrawImage(_jetToken, new System.Drawing.Rectangle((int)(left - F(4)), (int)(my - gd / 2), (int)gd, (int)gd), 0, 0, _jetToken.Width, _jetToken.Height, System.Drawing.GraphicsUnit.Pixel, ia);
                g.DrawImage(_jetToken, left, my - td / 2, td, td);
            }
            y += bigH + F(6);

            // Scale 0 → 50 % (one chance in two), ticks every 10 %, caret on the current chance.
            var baseY = y + F(10);
            GlowLine(g, left, baseY, right, baseY, color);
            for (var i = 0; i <= 5; i++)
            {
                var tx = left + (right - left) * i / 5;
                GlowLine(g, tx, baseY, tx, baseY + (i is 0 or 5 ? F(8) : F(5)), color);
            }
            var px = left + (float)((right - left) * Math.Clamp(v.PirateChance / 0.5, 0, 1));
            using (var caret = new System.Drawing.Drawing2D.GraphicsPath())
            {
                caret.AddPolygon(new[] { new System.Drawing.PointF(px, baseY - F(2.5)), new System.Drawing.PointF(px - F(6), baseY - F(11)), new System.Drawing.PointF(px + F(6), baseY - F(11)) });
                GlowFill(g, caret, color);
            }
            y += F(16) + F(8);

            // Chests, and the match in progress or the last match.
            At(chest, left, y - F(1));
            GlowFill(g, chest, color);
            At(bl, left + F(24), y);
            GlowFill(g, bl, color);
            At(br, right - PathWidth(br), y);
            GlowFill(g, br, color);
        }
        var old = _jetBase;
        _jetBase = bmp;
        old?.Dispose();

        Place(hwnd, w, h);
        SetWindowRgn(hwnd, IntPtr.Zero, true);
        UpdateJetTimer(hwnd);
        PresentJet(hwnd);
    }

    /// <summary>The timer of the animations runs only for the jet style, animations on, overlay shown.</summary>
    private static void UpdateJetTimer(IntPtr hwnd)
    {
        if (_style == "jet" && _animations && IsWindowVisible(hwnd) && !_offscreen)
        {
            SetTimer(hwnd, (UIntPtr)JetTimerId, 50, IntPtr.Zero);
        }
        else
        {
            KillTimer(hwnd, (UIntPtr)JetTimerId);
        }
    }

    /// <summary>Composes a frame (with the animations, if on) and puts it on screen.</summary>
    private static void PresentJet(IntPtr hwnd)
    {
        var b = _jetBase;
        if (b == null)
        {
            return;
        }
        int w = b.Width, h = b.Height;
        if (_jetFrame == null || _jetFrame.Width != w || _jetFrame.Height != h)
        {
            _jetFrame?.Dispose();
            _jetFrame = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        }
        using (var g = System.Drawing.Graphics.FromImage(_jetFrame))
        {
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.DrawImageUnscaled(b, 0, 0);
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            if (_animations && !_offscreen)
            {
                var t = AnimClock.Elapsed.TotalSeconds;
                var now = AnimClock.ElapsedMilliseconds;

                // Phosphor flicker: the whole picture breathes a little, with a rare deeper dip.
                var flicker = 0.9 + 0.1 * (0.5 + 0.5 * Math.Sin(t * 11.3) * Math.Sin(t * 3.7));
                if (Rng.NextDouble() < 0.02) flicker -= 0.18;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                using (var ia = new System.Drawing.Imaging.ImageAttributes())
                {
                    ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = (float)flicker });
                    g.DrawImage(b, new System.Drawing.Rectangle(0, 0, w, h), 0, 0, w, h, System.Drawing.GraphicsUnit.Pixel, ia);
                }
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                // Scan sweep: a band brighter than the rest goes down over the symbology every 2.6 s.
                var band = Math.Max(4, Sc(12));
                var sy = (int)((t % 2.6) / 2.6 * (h + band)) - band;
                g.SetClip(new System.Drawing.Rectangle(0, sy, w, band));
                using (var ia = new System.Drawing.Imaging.ImageAttributes())
                {
                    ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.55f });
                    g.DrawImage(b, new System.Drawing.Rectangle(0, 0, w, h), 0, 0, w, h, System.Drawing.GraphicsUnit.Pixel, ia);
                }
                g.ResetClip();

                // Glitch: every 4 to 9 s, for a split second, a few horizontal slices jump sideways.
                if (now >= _nextGlitch)
                {
                    _glitchUntil = now + 110 + Rng.Next(90);
                    _nextGlitch = now + 4000 + Rng.Next(5000);
                }
                if (now < _glitchUntil)
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    for (var i = 0; i < 3; i++)
                    {
                        var sh = Math.Max(3, Rng.Next(Sc(4), Sc(14) + 1));
                        var y0 = Rng.Next(0, Math.Max(1, h - sh));
                        var dx = (Rng.Next(2) == 0 ? -1 : 1) * Rng.Next(Sc(3), Sc(10) + 1);
                        var slice = new System.Drawing.Rectangle(0, y0, w, sh);
                        g.SetClip(slice);
                        g.Clear(System.Drawing.Color.Transparent);
                        g.DrawImage(b, new System.Drawing.Rectangle(dx, y0, w, sh), new System.Drawing.Rectangle(0, y0, w, sh), System.Drawing.GraphicsUnit.Pixel);
                        g.ResetClip();
                    }
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                }
            }
        }

        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var hbmp = _jetFrame.GetHbitmap(System.Drawing.Color.FromArgb(0));
        var oldBmp = SelectObject(mem, hbmp);
        var size = new SIZE { cx = w, cy = h };
        var src = new POINT();
        var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = Alpha, AlphaFormat = 1 };
        UpdateLayeredWindow(hwnd, screen, IntPtr.Zero, ref size, mem, ref src, 0, ref blend, 2 /* ULW_ALPHA */);
        SelectObject(mem, oldBmp);
        DeleteObject(hbmp);
        DeleteDC(mem);
        ReleaseDC(IntPtr.Zero, screen);
    }

    /// <summary>Nothing to paint: the window shows the bitmap given to UpdateLayeredWindow.</summary>
    private static void PaintJet(IntPtr hwnd, IntPtr hdc)
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
    [DllImport("user32.dll")] private static extern bool KillTimer(IntPtr hwnd, UIntPtr id);

    // ---- Placement: monitor and anchor ------------------------------------------------------------------------------

    /// <summary>"free" (where the player dragged it), a corner, the top center or the center of the screen.</summary>
    public static readonly string[] Anchors = { "free", "tl", "tc", "tr", "center", "bl", "br" };

    private static volatile string _anchor = "free";
    private static volatile string? _monitor;

    /// <summary>Anchors the overlay on a monitor (device name, null = the primary one).</summary>
    public static void SetPlacement(string anchor, string? monitor)
    {
        _anchor = Array.IndexOf(Anchors, anchor) >= 0 ? anchor : "free";
        _monitor = monitor;
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_APPEARANCE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public sealed record MonitorEntry(string Id, int Width, int Height, bool Primary);

    /// <summary>The monitors of this PC, in Windows' order.</summary>
    public static List<MonitorEntry> Monitors()
    {
        var list = new List<MonitorEntry>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, _, _, _) =>
        {
            var mi = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfoExW(h, ref mi))
            {
                list.Add(new MonitorEntry(mi.szDevice, mi.rcMonitor.Right - mi.rcMonitor.Left, mi.rcMonitor.Bottom - mi.rcMonitor.Top, (mi.dwFlags & 1) != 0));
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }

    /// <summary>The whole area of the chosen monitor (the game is full screen), or null when it is not connected.</summary>
    private static RECT? MonitorRect()
    {
        RECT? found = null, primary = null;
        var wanted = _monitor;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, _, _, _) =>
        {
            var mi = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfoExW(h, ref mi))
            {
                if ((mi.dwFlags & 1) != 0) primary = mi.rcMonitor;
                if (wanted != null && mi.szDevice == wanted) found = mi.rcMonitor;
            }
            return true;
        }, IntPtr.Zero);
        return found ?? primary;
    }

    /// <summary>Position of an anchored overlay, or null for "free".</summary>
    private static (int X, int Y)? Anchored(int w, int h)
    {
        if (_anchor == "free" || _offscreen || MonitorRect() is not { } r)
        {
            return null;
        }
        var mx = Sc(28);
        var my = Sc(28);
        var cx = (r.Left + r.Right - w) / 2;
        return _anchor switch
        {
            "tl" => (r.Left + mx, r.Top + my),
            "tc" => (cx, r.Top + my),
            "tr" => (r.Right - w - mx, r.Top + my),
            "bl" => (r.Left + mx, r.Bottom - h - my),
            "br" => (r.Right - w - mx, r.Bottom - h - my),
            // Head-up: centered, above the crosshair (never on it).
            _ => (cx, r.Top + (r.Bottom - r.Top) * 30 / 100 - h / 2),
        };
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor, rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }

    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")] private static extern bool GetMonitorInfoExW(IntPtr monitor, ref MONITORINFOEX info);
    [DllImport("gdi32.dll")] private static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr old);
    [DllImport("gdi32.dll")] private static extern bool LineTo(IntPtr hdc, int x, int y);

    private static uint Rgb(byte r, byte g, byte b) => (uint)(r | (g << 8) | (b << 16));

    // ---- Win32 ------------------------------------------------------------------------------
    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_TOPMOST = 0x00000008, WS_EX_TRANSPARENT = 0x00000020, WS_EX_TOOLWINDOW = 0x00000080, WS_EX_LAYERED = 0x00080000, WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20, HTCLIENT = 1;
    private const uint WM_SETCURSOR = 0x0020, WM_LBUTTONUP = 0x0202, WM_APP_LOCK = 0x8003, VK_L = 0x4C, SWP_FRAMECHANGED = 0x20;
    private static readonly IntPtr IDC_HAND = new(32649);
    private const uint LWA_ALPHA = 0x2, LWA_COLORKEY = 0x1;
    private const int SW_HIDE = 0, SW_SHOWNOACTIVATE = 4;
    private const uint WM_DESTROY = 0x0002, WM_PAINT = 0x000F, WM_NCHITTEST = 0x0084, WM_NCRBUTTONUP = 0x00A5,
        WM_TIMER = 0x0113, WM_EXITSIZEMOVE = 0x0232, WM_APP_TOGGLE = 0x8001, WM_PRINTCLIENT = 0x0318, WM_APP_RELAYOUT = 0x8002, WM_HOTKEY = 0x0312, WM_APP_APPEARANCE = 0x8004, WM_APP_STOP = 0x8005;
    private const uint MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000, VK_O = 0x4F;
    private const int HTCAPTION = 2;
    private const int TRANSPARENT = 1, LOGPIXELSY = 90;
    private const uint DT_LEFT = 0x0, DT_CENTER = 0x1, DT_RIGHT = 0x2, DT_VCENTER = 0x4, DT_BOTTOM = 0x8, DT_SINGLELINE = 0x20, DT_CALCRECT = 0x400, DT_NOPREFIX = 0x800, DT_NOCLIP = 0x100;
    private const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr IDC_SIZEALL = new(32646);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam, lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore, fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetMessageW(out MSG msg, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint key, byte alpha, uint flags);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr hwnd, IntPtr rgn, bool redraw);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT ps);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hwnd, ref PAINTSTRUCT ps);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hdc, ref RECT rect, IntPtr brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int DrawTextW(IntPtr hdc, string text, int count, ref RECT rect, uint format);
    [DllImport("user32.dll")] private static extern UIntPtr SetTimer(IntPtr hwnd, UIntPtr id, uint ms, IntPtr proc);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursorW(IntPtr instance, IntPtr name);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtrW(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandleW(string? name);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr hdc, int index);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public uint cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfoW(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] private static extern bool UpdateWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr bmp, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER bi, uint usage);
    [DllImport("gdi32.dll")] private static extern bool FrameRgn(IntPtr hdc, IntPtr rgn, IntPtr brush, int w, int h);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern int SetBkMode(IntPtr hdc, int mode);
    [DllImport("gdi32.dll")] private static extern uint SetTextColor(IntPtr hdc, uint color);
    [DllImport("gdi32.dll")] private static extern IntPtr CreatePen(int style, int width, uint color);
    [DllImport("gdi32.dll")] private static extern bool Ellipse(IntPtr hdc, int l, int t, int r, int b);
    [DllImport("gdi32.dll")] private static extern bool Polygon(IntPtr hdc, POINT[] points, int count);
    [DllImport("gdi32.dll")] private static extern bool Rectangle(IntPtr hdc, int l, int t, int r, int b);
    [DllImport("gdi32.dll")] private static extern bool RoundRect(IntPtr hdc, int l, int t, int r, int b, int w, int h);
    [DllImport("gdi32.dll")] private static extern bool Chord(IntPtr hdc, int l, int t, int r, int b, int x1, int y1, int x2, int y2);
    [DllImport("user32.dll")] private static extern int FrameRect(IntPtr hdc, ref RECT rect, IntPtr brush);
}
