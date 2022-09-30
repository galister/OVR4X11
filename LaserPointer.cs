using System.Collections;
using EasyOverlay;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Modules
{
    /// <summary>
    /// Place on a direct child of the tracked controller objects
    /// </summary>
    public class LaserPointer : BillboardOverlay
    {
        [SerializeField]
        public TrackedDevice trackedDevice;
        
        private const float MaxLength = 10f;

        private readonly Color leftClickColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color rightClickColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color middleClickColor = new(0x60, 0x00, 0x80, 0x80);
        
        private readonly Color leftClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private readonly Color rightClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private readonly Color middleClickColorDiminished = new(0x30, 0x00, 0x40, 0x80);

        public PointerModifier modifier;
        private bool wasIntersectedThisFrame = false;

        protected override void Start()
        {
            base.Start();
            manager.RegisterPointer(this, trackedDevice);
            width = 0.002f;
            showHideBinding = false;
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
        /// <param name="len">Length of laser</param>
        /// <param name="primary">If the pointer should use primary colors (eg. owns the mouse)</param>
        public void OnIntersected(float len, bool primary)
        {
            if (wasIntersectedThisFrame)
                return;
            
            wasIntersectedThisFrame = true;
            
            len = Mathf.Clamp(len, 0f, MaxLength);

            if (!visible)
                Show();
            else
                RenderTexture.ReleaseTemporary((RenderTexture)texture);
            // R16G16B16_SFloat is known to work on OpenGL Linux
            texture = RenderTexture.GetTemporary(1, (int)(len/width), 0, GraphicsFormat.R16G16B16_SFloat, 0);

            // forward is backwards for some reason
            transform.localPosition = transform.forward * len / -2f;
            
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
                return false;
            }
            
            if (!base.Render()) 
                return false;

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
    }

    public enum PointerModifier
    {
        None,
        RightClick,
        MiddleClick
    }
}