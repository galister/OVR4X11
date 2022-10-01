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
        
        private const float MaxLength = 10f;

        private readonly Color leftClickColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color rightClickColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color middleClickColor = new(0x60, 0x00, 0x80, 0x80);
        
        private readonly Color leftClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private readonly Color rightClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private readonly Color middleClickColorDiminished = new(0x30, 0x00, 0x40, 0x80);

        public PointerModifier modifier;
        private bool wasIntersectedThisFrame = false;

        private Transform childTransform;

        protected override void Start()
        {
            base.Start();
            manager.RegisterPointer(this, trackedDevice);
            width = 0.002f;
            showHideBinding = false;

            var go = new GameObject
            {
                transform =
                {
                    parent = transform,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one
                }
            };
            childTransform = go.transform;

            // R16G16B16_SFloat is known to work on OpenGL Linux
            texture = new RenderTexture(16, 16, 0, GraphicsFormat.R16G16B16_SFloat, 0);
            
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
            childTransform.localPosition = new Vector3(0, 0, len / 2f);
            childTransform.localScale = new Vector3(1, 1, len / width);
            childTransform.LookAt(manager.hmd);
            
            // rotate Z towards hmd
            var euler = childTransform.localEulerAngles;
            childTransform.localEulerAngles = new Vector3(0, 0, euler.z);
            
            // set cursor
            var cursorT = cursor.transform;

            cursorT.position = p.position + p.normal * 0.002f;
            cursorT.rotation = Quaternion.LookRotation(p.normal);
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
            
            overlay.SetOverlayWidthInMeters(handle, width);

            var m = Matrix4x4.TRS(childTransform.position, childTransform.rotation, childTransform.lossyScale);
            var pose = new HmdMatrix34_t
            {
                m0 = m[0, 0],
                m1 = m[0, 1],
                m2 = -m[0, 2],
                m3 = m[0, 3],
                m4 = m[1, 0],
                m5 = m[1, 1],
                m6 = -m[1, 2],
                m7 = m[1, 3],
                m8 = -m[2, 0],
                m9 = -m[2, 1],
                m10 = m[2, 2],
                m11 = -m[2, 3]
            };
            
            overlay.SetOverlayTransformAbsolute(handle, SteamVR.settings.trackingSpace, ref pose);

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