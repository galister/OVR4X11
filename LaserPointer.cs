using System;
using System.Collections;
using EasyOverlay;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Valve.VR;

namespace Modules
{
    /// <summary>
    /// Place on tracked controller objects
    /// </summary>
    public class LaserPointer : BaseOverlay, IDisposable
    {
        [SerializeField]
        public TrackedDevice trackedDevice;

        [SerializeField] 
        public BaseOverlay cursor;
        
        private const float MaxLength = 5f;
        private float length;

        private readonly Color leftClickColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color rightClickColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color middleClickColor = new(0x60, 0x00, 0x80, 0x80);
        
        private readonly Color leftClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private readonly Color rightClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private readonly Color middleClickColorDiminished = new(0x30, 0x00, 0x40, 0x80);

        public PointerModifier modifier;
        private bool wasIntersectedThisFrame = false;

        private Transform refPoint1;
        private Transform refPoint2;

        protected override void Start()
        {
            base.Start();
            manager.RegisterPointer(this, trackedDevice);
            width = 0.002f;
            showHideBinding = false;

            var go = new GameObject
            {
                name = "RefPoint1",
                transform =
                {
                    parent = transform,
                    localPosition = new Vector3(0, 0, MaxLength / 2),
                    localEulerAngles = new Vector3(90, 0, 0)
                }
            };
            refPoint1 = go.transform;

            var go2 = new GameObject
            {
                name = "RefPoint2",
                transform =
                {
                    parent = refPoint1,
                    localPosition = Vector3.zero
                }
            };
            refPoint2 = go2.transform;

            // R16G16B16_SFloat is known to work on OpenGL Linux
            texture = new RenderTexture(1, (int)(MaxLength/width), 0, GraphicsFormat.R16G16B16_SFloat, 0);
            
            if (Application.isEditor)
            {   
                // visualize
                var c = go.AddComponent<BoxCollider>();
                c.size = Vector3.one * width;
            }
        }

        // Do not show this overlay until there's an actual hit
        protected override IEnumerator AfterEnable()
        {
            yield break;
        }

        // We need to calculate modifiers before LateUpdate
        // because ClickableOverlay will read it then.
        protected override void Update()
        {
            base.Update();
            
            modifier = RecalculateModifier();
        }
        
        /// <summary>
        /// Called by the intersecting ClickableOverlay from LateUpdate
        /// </summary>
        /// <param name="p">Hit data</param>
        /// <param name="primary">If the pointer should use primary colors (eg. owns the mouse)</param>
        public void OnIntersected(PointerHit p, bool primary)
        {
            if (wasIntersectedThisFrame)
                return;
            
            wasIntersectedThisFrame = true;
            
            var len = Mathf.Clamp(p.distance, 0f, MaxLength);

            if (!visible)
                Show();

            // forward is backwards for some reason
            var dirToHmd = (refPoint2.transform.position - manager.hmd.position).normalized;
            refPoint2.rotation = Quaternion.LookRotation(dirToHmd, refPoint2.parent.up);
            // rotate around Y towards hmd
            refPoint2.localEulerAngles = new Vector3(0, refPoint2.localEulerAngles.y, 0);

            length = len;
            refPoint1.localPosition = new Vector3(0, 0, length / 2);
            
            // set cursor
            var cursorT = cursor.transform;
            var adjustedUv = (p.nativeUv + new Vector2(-0.5f, -0.5f)) * p.overlay.width;
            var screenTransform = p.overlay.transform;
            cursorT.position = screenTransform.TransformPoint(adjustedUv.x, adjustedUv.y, 0.001f);
            cursorT.rotation = Quaternion.LookRotation(screenTransform.forward);
            if (!cursor.visible)
                cursor.Show();

            var pointerColor = modifier switch
            {
                PointerModifier.None when primary => leftClickColor,
                PointerModifier.RightClick when primary => rightClickColor,
                PointerModifier.RightClick when !primary => rightClickColorDiminished,
                PointerModifier.MiddleClick when primary => middleClickColor,
                PointerModifier.MiddleClick when !primary => middleClickColorDiminished,
                _ => leftClickColorDiminished
            };
            
            SetColor(pointerColor);
        }

        // Runs after waitForEndOfFrame
        public override bool Render()
        {
            if (!wasIntersectedThisFrame)
            {
                if (visible) Hide();
                if (cursor.visible) cursor.Hide();
                return false;
            }

            wasIntersectedThisFrame = false;

            if (handle == OpenVR.k_ulOverlayHandleInvalid || !visible)
                return false;

            overlay.SetOverlaySortOrder(handle, 0xFFFFU);
            overlay.SetOverlayWidthInMeters(handle, width);
            
            var t = new SteamVR_Utils.RigidTransform(refPoint2);
            var matrix = t.ToHmdMatrix34();
            overlay.SetOverlayTransformAbsolute(handle, SteamVR.settings.trackingSpace, ref matrix);

            var bounds = new VRTextureBounds_t
            {
                uMin = 0,
                uMax = 1,
                vMin = 0,
                vMax = length / MaxLength
            };

            overlay.SetOverlayTextureBounds(handle, ref bounds);

            UploadTexture();
            return true;
        }

        // Helper methods below
        
        private void SetColor(Color c)
        {
            RenderTexture.active = (RenderTexture) texture;
            GL.Clear(false, true, c);
            RenderTexture.active = null;
        }
        
        private PointerModifier RecalculateModifier()
        {
            var t = transform;
            var toEye = manager.hmd.position - t.position;
            var dot = Vector3.Dot(toEye, t.up);
            return dot switch
            {
                > 0.7f => PointerModifier.MiddleClick, // backhand towards face
                < -0.7f => PointerModifier.RightClick, // palm towards face
                _ => PointerModifier.None // anything in between
            };
        }

        public void Dispose()
        {
            if (texture is RenderTexture rt)
                rt.DiscardContents();
        }
    }

    public enum PointerModifier
    {
        None,
        RightClick,
        MiddleClick
    }
}