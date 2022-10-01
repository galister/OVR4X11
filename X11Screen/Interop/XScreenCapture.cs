using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EasyOverlay.X11Screen.Interop
{
    public class XScreenCapture : IDisposable
    {
        private readonly IntPtr xShmHandle;
        private readonly int maxBytes;
        private Texture2D internalTexture { get; }
        private static Vector2 blitScale = new Vector2(1, -1);
        
        public readonly Vector2Int size;
        public RenderTexture texture { get; }

        public XScreenCapture(int screen)
        {
            size = XScreenCapture.GetScreenSize(screen);
            if (size.magnitude < 1)
                return;

            maxBytes = 4 * size.x * size.y;
            
            xShmHandle = xshm_cap_start(screen);
            
            internalTexture = new Texture2D(size.x, size.y, TextureFormat.BGRA32, false);

            // TODO: make this work with RenderTextures + Graphics.Blit.
            texture = new RenderTexture(size.x, size.y, 0, GraphicsFormat.R16G16B16_SFloat, 0);
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 9;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void Tick()
        {
            if (xShmHandle == IntPtr.Zero) return;
            
            var bytes = (int) xshm_grab_bgra32(xShmHandle);
            if (bytes != maxBytes)
            {
                Debug.Log($"Unexpected buffer size: {bytes}");
                return;
            }

            var pixBuf = xshm_pixel_buffer(xShmHandle);
                
            if (pixBuf == IntPtr.Zero)
            {
                Debug.Log($"Could not get pixel buffer!");
                return;
            }
            
            internalTexture.LoadRawTextureData(pixBuf, bytes);
            internalTexture.Apply();
            Graphics.Blit(internalTexture, texture, blitScale, Vector2.up);
        }

        public void Dispose()
        {
            if (xShmHandle != IntPtr.Zero)
                xshm_cap_end(xShmHandle);

            if (internalTexture != null)
                UnityEngine.Object.Destroy(internalTexture);
        }

        public void MoveMouse(Vector2 uv)
        {
            if (xShmHandle == IntPtr.Zero)
                return;

            var to = MouseCoordinatesFromUv(uv);
            xshm_mouse_move(xShmHandle, to.x, to.y);
        }

        public void SendMouse(Vector2 uv, XcbMouseButton button, bool pressed, XcbModifier modifiers)
        {
            if (xShmHandle == IntPtr.Zero)
                return;
            
            var to = MouseCoordinatesFromUv(uv);
            xshm_mouse_event(xShmHandle, to.x, to.y, (byte) button, pressed ? 1UL : 0UL, (ushort) modifiers);
        }

        public void SendKey(KeyCode keyCode, bool pressed, XcbModifier modifiers)
        {
            if (xShmHandle == IntPtr.Zero)
                return;
            
            xshm_keybd_event(xShmHandle, (byte) keyCode, pressed ? 1UL : 0UL, (ushort) modifiers);
        }
        
        public Vector2Int GetMousePosition()
        {
            var bigNumber = xshm_mouse_position(xShmHandle);
            var w = (int)(bigNumber >> 32);
            var h = (int)(bigNumber & 0xFFFFFFFF);
            return new Vector2Int(w, h);
        }

        public static Int32 NumScreens()
        {
            return xshm_num_screens();
        }

        private (short x, short y) MouseCoordinatesFromUv(Vector2 uv)
        {
            var x = (short)(uv.x * size.x);
            var y = (short)((1 - uv.y) * size.y);
            return (x, y);
        }

        public static Vector2Int GetScreenSize(int screen)
        {
            var bigNumber = xshm_screen_size(screen);
            var w = (int)(bigNumber >> 32);
            var h = (int)(bigNumber & 0xFFFFFFFF);
            Debug.Log($"Screen {screen} is {w}x{h}.");
            return new Vector2Int(w, h);
        }

        // ReSharper disable IdentifierTypo
        // ReSharper disable StringLiteralTypo
        // ReSharper disable InconsistentNaming
        // ReSharper disable BuiltInTypeReferenceStyle
        [DllImport("libxshm_cap.so")]
        private static extern IntPtr xshm_cap_start(Int32 screen_id);
        [DllImport("libxshm_cap.so")]
        private static extern void xshm_cap_end(IntPtr xhsm_instance);

        [DllImport("libxshm_cap.so")]
        private static extern Int32 xshm_num_screens();
        
        [DllImport("libxshm_cap.so")]
        private static extern UInt64 xshm_screen_size(Int32 screen);
        
        [DllImport("libxshm_cap.so")]
        private static extern UInt64 xshm_mouse_position(IntPtr xhsm_instance);

        [DllImport("libxshm_cap.so")]
        private static extern IntPtr xshm_pixel_buffer(IntPtr xhsm_instance);
        
        [DllImport("libxshm_cap.so")]
        private static extern UInt64 xshm_grab_bgra32(IntPtr xhsm_instance);

        [DllImport("libxshm_cap.so")]
        private static extern void xshm_mouse_move(IntPtr xhsm_instance, Int16 x, Int16 y);

        [DllImport("libxshm_cap.so")]
        private static extern void xshm_mouse_event(IntPtr xhsm_instance, Int16 x, Int16 y, Byte button, UInt64 pressed, UInt16 modmask);

        [DllImport("libxshm_cap.so")]
        private static extern void xshm_keybd_event(IntPtr xhsm_instance, Byte keycode, UInt64 pressed, UInt16 modmask);
    }
}
