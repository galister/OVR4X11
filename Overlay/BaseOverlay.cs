using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay.Overlay
{
    /// <summary>
    /// Represents a basic overlay that displays a texture.
    /// </summary>
    public class BaseOverlay : MonoBehaviour
    {
        protected OverlayManager manager;
        
        [Tooltip("Unique identifier for use with OpenVR API.\nIf left empty, one will be generated from ProductName/GameObjectName")]
        [SerializeField]
        public string key;

        [Tooltip("Texture to be sent to OpenVR")]
        [SerializeField]
        public Texture texture;

        [Tooltip("Width of overlay. Height will be calculated from texture aspect ratio.")]
        [SerializeField]
        public float width;

        [SerializeField] 
        public TransformUpdateMode transformUpdateMode;
        
        [Tooltip("Respond to the Show/Hide binding.")]
        [SerializeField]
        public bool showHideBinding = true;

        [SerializeField]
        public uint zOrder;

        [DoNotSerialize]
        public BaseOverlay owner;

        private ulong handle = OpenVR.k_ulOverlayHandleInvalid;
        private CVROverlay overlay;

        [Tooltip("(ReadOnly) Returns true if being rendered.")]
        public bool visible { get; private set; }

        protected virtual void Start()
        {
            manager = FindObjectOfType<OverlayManager>();
            manager.RegisterWindow(this);
        }

        protected virtual void Update() { }

        protected virtual void LateUpdate() { }
        
        protected internal virtual void BeforeRender() { }
        
        /// <summary>
        /// Start rendering this overlay.
        /// </summary>
        public virtual void Show()
        {
            if (handle == OpenVR.k_ulOverlayHandleInvalid)
                return;

            var error = overlay.ShowOverlay(handle);
            if (error is EVROverlayError.InvalidHandle or EVROverlayError.UnknownOverlay 
                && overlay.FindOverlay(key, ref handle) != EVROverlayError.None)
            {
                Debug.Log($"Could not FindOverlay for {key}");
                visible = false;
                return;
            }
            
            overlay.SetOverlaySortOrder(handle, zOrder);
            UploadWidth();
            UploadTexture();
            
            visible = true;
        }

        /// <summary>
        /// Stop rendering this overlay, but keep it allocated in OpenVR.
        /// </summary>
        public virtual void Hide()
        {
            if (handle != OpenVR.k_ulOverlayHandleInvalid)
                overlay.HideOverlay(handle);
            visible = false;
        }

        protected void LootAtHmd()
        {
            var direction = transform.position - manager.hmd.position;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        protected virtual void OnEnable()
        {
            if (String.IsNullOrWhiteSpace(key))
                key = $"{Application.productName.Replace(" ", "_")}/{gameObject.name.Replace(" ", "_")}";
            
            if (handle != OpenVR.k_ulOverlayHandleInvalid)
            {
                enabled = false;
                return;
            }

            overlay = OpenVR.Overlay;
            if (overlay == null)
            {
                Debug.Log("Overlay API not available.");
                enabled = false;
                return;
            }
            
            var error = overlay.CreateOverlay(key, key, ref handle);
            if (error != EVROverlayError.None)
            {
                Debug.Log("<b>[SteamVR]</b> " + overlay.GetOverlayErrorNameFromEnum(error));
                enabled = false;
                return;
            }

            if (texture == null)
                Debug.Log($"Not showing {key} - Texture not set");
            else 
                StartCoroutine(FirstShow());
        }

        protected virtual IEnumerator FirstShow()
        {
            yield return null;
            Show();
        }

        #region OpenVR Rendering
        
        private readonly Queue<Action> renderActions = new();
        private TrackedDevice relativeDevice;
        
        /// <summary>
        /// Uploads changes to OpenVR. This is called by the OverlayManager instance.
        /// </summary>
        internal void Render()
        {
            if (handle == OpenVR.k_ulOverlayHandleInvalid || !visible)
                return;

            foreach (var action in renderActions) 
                action();
            
            if (transformUpdateMode == TransformUpdateMode.Automatic)
                DoUploadPositionAbsolute();

            renderActions.Clear();
        }

        /// <summary>
        /// Will remove this overlay from OVR, freeing up its resources.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (handle == OpenVR.k_ulOverlayHandleInvalid) return;
            
            if (!SteamVR.active) return;

            Debug.Log($"Destroying {key}");
            overlay?.DestroyOverlay(handle);

            handle = OpenVR.k_ulOverlayHandleInvalid;
        }

        /// <summary>
        /// Will upload the width at the end of this frame.
        /// </summary>
        protected virtual void UploadWidth()
        {
            renderActions.Enqueue(DoUploadWidth);
        }

        /// <summary>
        /// Will upload the absolute position at the end of this frame.
        /// </summary>
        protected internal void UploadPositionAbsolute()
        {
            relativeDevice = TrackedDevice.None;
            renderActions.Enqueue(DoUploadPositionAbsolute);
        }
        
        protected void UploadPositionAbsolute(Transform t)
        {
            relativeDevice = TrackedDevice.None;
            renderActions.Enqueue(() => DoUploadPositionAbsolute(t));
        }
        
        protected void UploadPositionRelative(TrackedDevice d)
        {
            relativeDevice = d;
            transformUpdateMode = TransformUpdateMode.Manual;
            renderActions.Enqueue(DoUploadPositionRelative);
        }
        
        protected void UploadColor(Color c)
        {
            renderActions.Enqueue(() => DoUploadColor(c));
        }
        
        protected void UploadBounds(float uMin, float uMax, float vMin, float vMax)
        {
            renderActions.Enqueue(() => DoUploadBounds(uMin, uMax, vMin, vMax));
        }
        
        /// <summary>
        /// Will upload the texture at the end of this frame.
        /// </summary>
        protected void UploadTexture()
        {
            renderActions.Enqueue(DoUploadTexture);
        }
        
        // Actual implementations below
        
        private void DoUploadWidth()
        {
            overlay.SetOverlayWidthInMeters(handle, width);
        }

        private void DoUploadPositionAbsolute()
        {
            var matrix = new SteamVR_Utils.RigidTransform(transform).ToHmdMatrix34();
            overlay.SetOverlayTransformAbsolute(handle, SteamVR.settings.trackingSpace, ref matrix);
        }
        
        private void DoUploadPositionAbsolute(Transform t)
        {
            var matrix = new SteamVR_Utils.RigidTransform(t).ToHmdMatrix34();
            overlay.SetOverlayTransformAbsolute(handle, SteamVR.settings.trackingSpace, ref matrix);
        }
        private void DoUploadPositionRelative()
        {
            var matrix = new SteamVR_Utils.RigidTransform(transform.localPosition, transform.localRotation).ToHmdMatrix34();
            overlay.SetOverlayTransformTrackedDeviceRelative(handle, (uint) relativeDevice, ref matrix);
        }
        
        private void DoUploadTexture()
        {
            var tex = new Texture_t
            {
                handle = texture.GetNativeTexturePtr(),
                eType = SteamVR.instance.textureType,
                eColorSpace = EColorSpace.Auto
            };
            
            overlay.SetOverlayTexture(handle, ref tex);
        }

        private void DoUploadColor(Color c)
        {
            overlay.SetOverlayColor(handle, c.r, c.g, c.b);
        }

        private void DoUploadBounds(float uMin, float uMax, float vMin, float vMax)
        {
            var bounds = new VRTextureBounds_t
            {
                uMin = uMin,
                uMax = uMax,
                vMin = vMin,
                vMax = vMax
            };
            overlay.SetOverlayTextureBounds(handle, ref bounds);
        }
        #endregion
    }

    public enum TransformUpdateMode
    {
        Automatic,
        Manual
    }
}