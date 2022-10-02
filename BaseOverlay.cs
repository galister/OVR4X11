using System;
using System.Collections;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay
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

        [Tooltip("Respond to the Show/Hide binding.")]
        [SerializeField]
        public bool showHideBinding = true;

        protected ulong handle = OpenVR.k_ulOverlayHandleInvalid;
        protected CVROverlay overlay;

        [Tooltip("(ReadOnly) Returns true if being rendered.")]
        public bool visible { get; private set; }

        protected virtual void Start()
        {
            manager = FindObjectOfType<OverlayManager>();
            manager.RegisterWindow(this);
        }

        protected virtual void Update() { }

        protected virtual void LateUpdate() { }

        public virtual void SetTexture(Texture t)
        {
            texture = t;
            UploadTexture();
        }
        
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

            visible = true;
            UploadTexture();
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

        public void LootAtHmd()
        {
            var direction = transform.position - manager.hmd.position;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private readonly WaitForEndOfFrame waitForEndOfFrame = new();

        /// <summary>
        /// Upload changes to OpenVR
        /// </summary>
        /// <returns><i>true</i> if the rendering can continue</returns>
        public virtual bool Render()
        {
            if (handle == OpenVR.k_ulOverlayHandleInvalid || !visible)
                return false;
            
            overlay.SetOverlayWidthInMeters(handle, width);
            
            var t = new SteamVR_Utils.RigidTransform(transform);
            // var dot = Vector3.Dot(transform.forward, (manager.hmd.position - transform.position).normalized);
            // if (dot > 0f)
            // {
            //     t.rot *= Quaternion.AngleAxis(180, Vector3.up);
            // }
            var matrix = t.ToHmdMatrix34();
            overlay.SetOverlayTransformAbsolute(handle, SteamVR.settings.trackingSpace, ref matrix);
            return true;
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
                StartCoroutine(AfterEnable());
        }

        protected virtual IEnumerator AfterEnable()
        {
            yield return null;
            Show();
        }

        /// <summary>
        /// Remove this overlay from OVR, freeing up its resources.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (handle == OpenVR.k_ulOverlayHandleInvalid) return;
            
            if (!SteamVR.active) return;

            Debug.Log($"Destroying {key}");
            overlay?.DestroyOverlay(handle);

            handle = OpenVR.k_ulOverlayHandleInvalid;
        }

        protected void UploadTexture()
        {
            var tex = new Texture_t
            {
                handle = texture.GetNativeTexturePtr(),
                eType = SteamVR.instance.textureType,
                eColorSpace = EColorSpace.Auto
            };
            
            overlay.SetOverlayTexture(handle, ref tex);
            
            // var bounds = new VRTextureBounds_t
            // {
            //     uMin = 0,
            //     vMin = 0,
            //     uMax = 1,
            //     vMax = 1
            // };
            // overlay.SetOverlayTextureBounds(handle, ref bounds);
        }
    }
}