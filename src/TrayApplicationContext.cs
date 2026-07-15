using System;
using System.Linq;
using System.Windows.Forms;

namespace StayOnTop
{
    /// <summary>
    /// Hosts the tray icon and the global hotkey. There is no visible main form,
    /// and (deliberately) no floating overlay window - just the hotkey and the
    /// tray menu, kept as small and predictable as possible.
    /// </summary>
    internal class TrayApplicationContext : ApplicationContext
    {
        private const int HotkeyId = 9000;
        private const uint HotkeyVk = 0x54; // 'T'

        private readonly NotifyIcon _trayIcon;
        private readonly HotkeyWindow _hotkeyWindow;
        private readonly PinManager _pinManager = new PinManager();
        private readonly Timer _pruneTimer;

        private ToolStripMenuItem _startupItem;
        private ToolStripMenuItem _pinnedListItem;

        public TrayApplicationContext()
        {
            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

            bool registered = NativeMethods.RegisterHotKey(
                _hotkeyWindow.Handle, HotkeyId,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, HotkeyVk);

            _trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "Stay on Top  (Ctrl+Alt+T)",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };

            if (!registered)
            {
                _trayIcon.ShowBalloonTip(3000, "Stay on Top",
                    "Ctrl+Alt+T is already used by another program. You can still pin/unpin " +
                    "from this tray icon's right-click menu.", ToolTipIcon.Warning);
            }

            // Keep the "Pinned windows" submenu current the moment anything
            // actually changes, instead of relying only on the menu's Opening
            // event (which is one more moving part that could be flaky).
            _pinManager.PinStateChanged += (hwnd, pinned) => RefreshPinnedList();

            // Periodically clean up the "Pinned windows" list (closed windows)
            // and, as a third safety net, refresh it too.
            _pruneTimer = new Timer { Interval = 5000 };
            _pruneTimer.Tick += (s, e) =>
            {
                _pinManager.PruneClosedWindows();
                RefreshPinnedList();
            };
            _pruneTimer.Start();

            RefreshPinnedList();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var hintItem = new ToolStripMenuItem("Press Ctrl+Alt+T to pin/unpin the active window") { Enabled = false };
            menu.Items.Add(hintItem);

            menu.Items.Add(new ToolStripSeparator());

            _pinnedListItem = new ToolStripMenuItem("Pinned windows");
            menu.Items.Add(_pinnedListItem);
            menu.Opening += (s, e) => RefreshPinnedList();

            menu.Items.Add(new ToolStripSeparator());

            _startupItem = new ToolStripMenuItem("Start with Windows")
            {
                CheckOnClick = true,
                Checked = StartupHelper.IsEnabled()
            };
            _startupItem.Click += (s, e) => StartupHelper.SetEnabled(_startupItem.Checked);
            menu.Items.Add(_startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("About Stay on Top");
            aboutItem.Click += (s, e) => MessageBox.Show(
                "Stay on Top\n\n" +
                "Ctrl+Alt+T pins or unpins the currently focused window so it stays on top " +
                "of every other window.",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            menu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApp();
            menu.Items.Add(exitItem);

            return menu;
        }

        private void RefreshPinnedList()
        {
            if (_pinnedListItem == null) return;

            _pinnedListItem.DropDownItems.Clear();
            var pinned = _pinManager.PinnedWindows.ToList();

            if (pinned.Count == 0)
            {
                _pinnedListItem.DropDownItems.Add(new ToolStripMenuItem("(none)") { Enabled = false });
                return;
            }

            foreach (var kv in pinned)
            {
                IntPtr hwnd = kv.Key;
                string title = _pinManager.GetCachedTitle(hwnd);
                if (string.IsNullOrEmpty(title)) title = "(untitled window)";
                if (title.Length > 50) title = title.Substring(0, 47) + "...";

                var item = new ToolStripMenuItem(title) { Checked = true };
                item.Click += (s, e) => _pinManager.Unpin(hwnd);
                _pinnedListItem.DropDownItems.Add(item);
            }
        }

        private void ToggleActiveWindow()
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd)) return;
            if (hwnd == _hotkeyWindow.Handle) return; // never target our own hidden window

            bool nowPinned = _pinManager.Toggle(hwnd);
            string title = _pinManager.GetCachedTitle(hwnd);
            if (string.IsNullOrEmpty(title)) title = "this window";

            int totalPinned = _pinManager.PinnedWindows.Count();

            _trayIcon.ShowBalloonTip(1500, "Stay on Top",
                (nowPinned ? "Pinned on top: " : "Unpinned: ") + title +
                "  [" + totalPinned + " total pinned]", ToolTipIcon.Info);
        }

        private void OnHotkeyPressed()
        {
            ToggleActiveWindow();
        }

        private void ExitApp()
        {
            NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyId);
            _trayIcon.Visible = false;
            _pruneTimer.Stop();
            Application.Exit();
        }

        /// <summary>Minimal, invisible native window whose only job is to receive WM_HOTKEY.</summary>
        private class HotkeyWindow : NativeWindow, IDisposable
        {
            public event Action HotkeyPressed;

            public HotkeyWindow()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_HOTKEY)
                {
                    var handler = HotkeyPressed;
                    if (handler != null) handler();
                }
                base.WndProc(ref m);
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}
