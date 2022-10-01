using System;

namespace EasyOverlay.X11Screen.Interop
{
    [Flags]
    public enum XcbModifier : ushort
    {
        Shift = 1,
        Caps = 2, 
        Ctrl = 4,
        Alt = 8,
        NumLock = 16,
        Super = 64
    }
}