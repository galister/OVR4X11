using System;
using System.Collections;
using EasyOverlay.Overlay;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EasyOverlay
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
        
        [DoNotSerialize]
        public PointerModifier modifier;
        
        private const float MaxLength = 5f;
        private float length;
        private Color color;

        private static readonly Color LeftClickColor = new(0x00, 0x60, 0x80, 0x80);
        private static readonly Color RightClickColor = new(0xB0, 0x30, 0x00, 0x80);
        private static readonly Color MiddleClickColor = new(0x60, 0x00, 0x80, 0x80);
        
        private static readonly Color LeftClickColorDiminished = new(0x00, 0x30, 0x40, 0x80);
        private static readonly Color RightClickColorDiminished = new(0x60, 0x20, 0x00, 0x80);
        private static readonly Color MiddleClickColorDiminished = new(0x30, 0x00, 0x40, 0x80);

        private static readonly Color NeutralColor = new(0xA0, 0xA0, 0xA0, 0x80);
        
        private bool wasIntersectedThisFrame;

        private static Texture2D sharedTexture;
        
        private Transform midPoint;
        private Transform renderTransform;

        protected override void Awake()
        {
            base.Awake();
            
            var go = new GameObject
            {
                name = "MidPoint",
                transform =
                {
                    parent = transform,
                    localPosition = new Vector3(0, 0, MaxLength / 2),
                    localEulerAngles = new Vector3(90, 0, 0)
                }
            };
            midPoint = go.transform;

            var go2 = new GameObject
            {
                name = "RenderTransform",
                transform =
                {
                    parent = midPoint,
                    localPosition = Vector3.zero
                }
            };
            renderTransform = go2.transform;

            if (sharedTexture == null)
            {
                sharedTexture = new Texture2D(1, (int)(MaxLength / width), GraphicsFormat.R8G8B8A8_SRGB, 0,
                    TextureCreationFlags.None);

                for (var i = 0; i < sharedTexture.height; i++) sharedTexture.SetPixel(0, i, Color.white);
            }

            texture = sharedTexture;
        }

        protected override void Start()
        {
            base.Start();
            manager.RegisterPointer(this, trackedDevice);
            width = 0.002f;
            showHideBinding = false;

            cursor.owner = this;
        }

        // Do not show this overlay until there's an actual hit
        protected override IEnumerator FirstShow()
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
        /// <param name="primary">If the laser should use primary colors (eg. owns the mouse)</param>
        /// <param name="wantCursor">Show cursor or not</param>
        public void OnIntersected(PointerHit p, bool primary, bool wantCursor)
        {
            if (wasIntersectedThisFrame)
                return;
            
            wasIntersectedThisFrame = true;
            
            var len = Mathf.Clamp(p.distance, 0f, MaxLength);

            if (!visible)
                Show();

            // forward is backwards for some reason
            var dirToHmd = (renderTransform.transform.position - manager.hmd.position).normalized;
            renderTransform.rotation = Quaternion.LookRotation(dirToHmd, renderTransform.parent.up);
            // rotate around Y towards hmd
            renderTransform.localEulerAngles = new Vector3(0, renderTransform.localEulerAngles.y, 0);

            length = len;
            midPoint.localPosition = new Vector3(0, 0, length / 2);

            if (wantCursor)
            {
                // set cursor
                var cursorT = cursor.transform;
                cursorT.position = p.point + new Vector3(0.5f, -1f, 0) * cursor.width;
                cursorT.rotation = Quaternion.LookRotation(p.overlay.transform.forward);
                if (!cursor.visible)
                    cursor.Show();
                cursor.UploadPositionAbsolute();
            }
            else if (cursor.visible) 
                cursor.Hide();

            color = modifier switch
            {
                PointerModifier.None when primary => LeftClickColor,
                PointerModifier.RightClick when primary => RightClickColor,
                PointerModifier.RightClick => RightClickColorDiminished,
                PointerModifier.MiddleClick when primary => MiddleClickColor,
                PointerModifier.MiddleClick => MiddleClickColorDiminished,
                PointerModifier.Neutral => NeutralColor,
                _ => LeftClickColorDiminished
            };
        }
        
        protected internal override bool Render()
        {
            if (!wasIntersectedThisFrame)
            {
                if (visible) Hide();
                if (cursor.visible) cursor.Hide();
                return false;
            }

            if (!base.Render())
                return false;

            UploadPositionAbsolute(renderTransform);
            UploadBounds(0, 1, 0, length/MaxLength);
            UploadColor(color);
            
            wasIntersectedThisFrame = false;
            return true;
        }

        // Helper methods below
        
        private PointerModifier RecalculateModifier()
        {
            var t = transform;
            var hmdUp = manager.hmd.up;
            var dot = Vector3.Dot(hmdUp, t.right);
            dot *= trackedDevice == TrackedDevice.LeftHand ? -1f : 1f;

            return dot switch
            {
                > 0.8f => PointerModifier.MiddleClick,
                < -0.5f => PointerModifier.RightClick,
                _ => PointerModifier.None
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
        MiddleClick,
        Neutral
    }
}