using System;
using System.Collections.Generic;
using System.Linq;

namespace PinToTop
{
    /// <summary>
    /// Pins/unpins windows (toggles the topmost style) and keeps a small
    /// convenience list of titles for the tray menu.
    ///
    /// Important: "is this window pinned" is always answered by asking Windows
    /// directly (checking the real WS_EX_TOPMOST style), never by trusting our
    /// own cached bookkeeping. HWNDs get reused by Windows once a window closes,
    /// so a cache-only approach can end up telling a brand-new, unrelated window
    /// "you're already pinned" just because some earlier, now-closed window
    /// happened to have the same handle value. Querying the OS live avoids that
    /// entirely.
    /// </summary>
    internal class PinManager
    {
        private readonly HashSet<IntPtr> _pinnedByUs = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, string> _titleCache = new Dictionary<IntPtr, string>();

        public event Action<IntPtr, bool> PinStateChanged;

        public bool IsPinned(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd)) return false;
            return (NativeMethods.GetWindowExStyle(hwnd) & NativeMethods.WS_EX_TOPMOST) != 0;
        }

        public IEnumerable<KeyValuePair<IntPtr, string>> PinnedWindows
        {
            get { return _titleCache.Where(kv => IsPinned(kv.Key)); }
        }

        /// <summary>Toggles the pin state of the given window. Returns the new state.</summary>
        public bool Toggle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
                return false;

            bool nowPinned = !IsPinned(hwnd);
            if (nowPinned) Pin(hwnd); else Unpin(hwnd);
            return nowPinned;
        }

        public void Pin(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd)) return;

            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

            _pinnedByUs.Add(hwnd);
            _titleCache[hwnd] = NativeMethods.GetWindowTitle(hwnd);

            var handler = PinStateChanged;
            if (handler != null) handler(hwnd, true);
        }

        public void Unpin(IntPtr hwnd)
        {
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

            _pinnedByUs.Remove(hwnd);

            var handler = PinStateChanged;
            if (handler != null) handler(hwnd, false);
        }

        /// <summary>Call periodically to drop menu entries for windows that closed
        /// or are no longer actually pinned (e.g. unpinned by some other means).</summary>
        public void PruneClosedWindows()
        {
            var stale = _pinnedByUs.Where(h => !IsPinned(h)).ToList();
            foreach (var h in stale)
            {
                _pinnedByUs.Remove(h);
                _titleCache.Remove(h);
            }
        }

        public string GetCachedTitle(IntPtr hwnd)
        {
            string title;
            if (_titleCache.TryGetValue(hwnd, out title) && !string.IsNullOrEmpty(title))
                return title;
            return NativeMethods.GetWindowTitle(hwnd);
        }
    }
}
