using System;
using EasyOverlay.Overlay;
using EasyOverlay.X11Screen.Interop;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay.X11Screen
{
    public class ScreenOverlay : GrabbableOverlay
    {
        private XScreenCapture cap;
        
        [SerializeField]
        public int screen;
        
        private DateTime freezeCursor = DateTime.MinValue;

        protected internal override void BeforeRender()
        {
            base.LateUpdate();
            cap?.Tick();
            
            var mouse = cap.GetMousePosition();
            var cursor = manager.desktopCursor;
            
            if (mouse.x >= 0 && mouse.x < texture.width
                             && mouse.y >= 0 && mouse.y < texture.height)
            {
                var point = new Vector2(mouse.x / (float)texture.width, (texture.height-mouse.y) / (float)texture.height);
                point += new Vector2(-0.5f, -0.5f);
                point *= new Vector2(width, width * activeRatio.y);
                point += new Vector2(cursor.width / 2f, -cursor.width / 2f);
                point /= new Vector2(transform.localScale.x, transform.localScale.y);
                
                var cursorT = cursor.transform;
                var screenTransform = transform;
                cursorT.position = screenTransform.TransformPoint(point.x, point.y, 0.001f);
                cursorT.rotation = Quaternion.LookRotation(screenTransform.forward);

                if (cursor.owner != this)
                {
                    cursor.owner = this;
                    if (!cursor.visible)
                        cursor.Show();
                }
                cursor.UploadPositionAbsolute();
            }
            else if (cursor.owner == this)
            {
                cursor.Hide();
                cursor.owner = null;
            }
            
            UploadTexture();
        }
        
        protected override void OnEnable()
        {
            cap = new XScreenCapture(screen);
            texture = cap.texture;
            
            UpdateTextureBounds(); 

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (manager.desktopCursor.owner == this)
            {
                manager.desktopCursor.Hide();
                manager.desktopCursor.owner = null;
            }

            cap?.Dispose();
            texture = null;
            cap = null;
        }

        protected override bool OnMove(PointerHit pointer, bool primary)
        {
            if (primary && freezeCursor < DateTime.UtcNow) 
                cap?.MoveMouse(pointer.uv);
            return true;
        }

        protected override bool OnLeft(TrackedDevice device, bool primary)
        {
            return true;
        }

        protected override bool OnPressed(PointerHit pointer)
        {
            freezeCursor = DateTime.UtcNow + TimeSpan.FromMilliseconds(200);
            SendMouse(pointer, true);
            return true;
        }

        protected override bool OnReleased(PointerHit pointer)
        {
            SendMouse(pointer, false);
            return true;
        }

        private void SendMouse(PointerHit pointer, bool pressed)
        {
            var click = pointer.modifier switch
            {
                PointerModifier.RightClick => XcbMouseButton.Right,
                PointerModifier.MiddleClick => XcbMouseButton.Middle,
                _ => XcbMouseButton.Left
            };

            cap?.SendMouse(pointer.uv, click, pressed);
        }

        private DateTime nextScroll = DateTime.MinValue;
        protected override bool OnScroll(PointerHit pointer, float value)
        {
            if (base.OnScroll(pointer, value))
                return true;

            if (nextScroll > DateTime.UtcNow)
                return true;


            if (pointer.modifier == PointerModifier.MiddleClick)
            {
                // super fast scroll, 1 click per frame
            }
            else
            {
                var millis = pointer.modifier == PointerModifier.None ? 100 : 50;
                nextScroll = DateTime.UtcNow.AddMilliseconds((1 - Mathf.Abs(value)) * millis);
            }

            if (value < 0)
            {
                cap?.SendMouse(pointer.uv, XcbMouseButton.WheelDown, true);
                cap?.SendMouse(pointer.uv, XcbMouseButton.WheelDown, false);
            }
            else
            {
                cap?.SendMouse(pointer.uv, XcbMouseButton.WheelUp, true);
                cap?.SendMouse(pointer.uv, XcbMouseButton.WheelUp, false);
            }

            return true;
        }

        protected override void DrawPointer(PointerHit p, bool primary)
        {
            if (!primary)
                p.modifier = PointerModifier.Neutral;
            
            p.pointer.OnIntersected(p, primary, false);
        }
    }
}