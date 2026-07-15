using System;
using System.Collections.Generic;
using System.Linq;

namespace StayOnTop
{
    /// <summary>
    /// Pins/unpins windows (toggles the topmost style) and keeps track of which
    /// ones are currently pinned for the tray menu's "Pinned windows" list.
    ///
    /// Pin state is tracked in our own list rather than re-queried live from
    /// Windows every time, since that turned out to be unreliable in practice.
    /// The one risk with a cached list is that Windows can reuse a closed
    /// window's HWND value for a brand-new, unrelated window - we guard
    /// against that cheaply by remembering each pinned window's title and
    /// treating a changed title as a sign it's not the same window anymore.
    /// </summary>
    internal class PinManager
    {
        private readonly HashSet<IntPtr> _pinned = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, string> _titleCache = new Dictionary<IntPtr, string>();

        public event Action<IntPtr, bool> PinStateChanged;

        public bool IsPinned(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd)) return false;
            if (!_pinned.Contains(hwnd)) return false;

            // Guard against HWND reuse: if the title has changed since we
            // pinned it, this is almost certainly a different window that
            // happens to have inherited the same handle value.
            string cachedTitle;
            if (_titleCache.TryGetValue(hwnd, out cachedTitle) && !string.IsNullOrEmpty(cachedTitle))
            {
                string currentTitle = NativeMethods.GetWindowTitle(hwnd);
                if (!string.IsNullOrEmpty(currentTitle) && currentTitle != cachedTitle)
                {
                    _pinned.Remove(hwnd);
                    _titleCache.Remove(hwnd);
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<KeyValuePair<IntPtr, string>> PinnedWindows
        {
            get { return _titleCache.Where(kv => _pinned.Contains(kv.Key)).ToList(); }
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

            _pinned.Add(hwnd);
            _titleCache[hwnd] = NativeMethods.GetWindowTitle(hwnd);

            var handler = PinStateChanged;
            if (handler != null) handler(hwnd, true);
        }

        public void Unpin(IntPtr hwnd)
        {
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

            _pinned.Remove(hwnd);
            _titleCache.Remove(hwnd);

            var handler = PinStateChanged;
            if (handler != null) handler(hwnd, false);
        }

        /// <summary>Call periodically to drop entries for windows that have since been closed.</summary>
        public void PruneClosedWindows()
        {
            var dead = _pinned.Where(h => !NativeMethods.IsWindow(h)).ToList();
            foreach (var h in dead)
            {
                _pinned.Remove(h);
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
