using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Valve.VR;

namespace EasyOverlay.Overlay
{
    public class OverlayManager : MonoBehaviour
    {
        public static OverlayManager instance;
        
        private const string ActionSet = "default";
        private const string ShowHideAction = "ShowHide";
        
        [SerializeField] public Transform hmd;

        [SerializeField] public Transform spawn;

        private readonly LaserPointer[] controllers = new LaserPointer[2];
        private readonly List<BaseOverlay> overlays = new();

        [SerializeField] public bool windowsVisible;
        private bool showHidePressed = false;
        
        [SerializeField] 
        public BaseOverlay desktopCursor;

        public OverlayManager()
        {
            if (instance != null)
                throw new ApplicationException("Can't have more than one OverlayManager!");
            instance = this;
        }
        
        private void Start()
        {
            StartCoroutine(Render());

            Application.quitting += () =>
            {
                enabled = false;
            };
        }

        public void RegisterWindow(BaseOverlay o)
        {
            overlays.Add(o);
        }
        
        public void UnregisterWindow(BaseOverlay o)
        {
            overlays.Remove(o);
        }

        public void RegisterPointer(LaserPointer o, TrackedDevice hand)
        {
            controllers[(int)hand-1] = o;
        }

        private readonly WaitForEndOfFrame waitForEndOfFrame = new();

        private IEnumerator Render()
        {
            while (enabled)
            {
                yield return waitForEndOfFrame;

                foreach (var o in overlays.Where(o => o.isActiveAndEnabled && o.visible))
                {
                    o.BeforeRender();
                    o.Render();
                }
            }
            Debug.Log("OverlayRenderer stopped.");
        }

        private readonly InteractableOverlay[] hitOverlays = new InteractableOverlay[2];
        private int numHitOverlays;
        
        private void HandleInteractions()
        {
            numHitOverlays = 0;
            
            foreach (var pointer in controllers)
            {
                var pT = pointer.transform;
                
                
                if (!Physics.Raycast(new Ray(pT.position, pT.forward), out var raycastHit))
                {
                    if (pointer.owner != null)
                    {
                        ((InteractableOverlay)pointer.owner).OnInteractLeft(pointer.trackedDevice);
                        pointer.owner = null;
                    }

                    continue;
                }

                var hit = new PointerHit(pointer, raycastHit);

                if (pointer.owner != hit.overlay)
                {
                    if (pointer.owner != null)
                        ((InteractableOverlay)pointer.owner).OnInteractLeft(pointer.trackedDevice);

                    pointer.owner = hit.overlay;
                }

                hit.overlay.OnInteractPointer(hit);
                hitOverlays[numHitOverlays++] = hit.overlay;
            }

            foreach (var overlay in hitOverlays.Take(numHitOverlays).Distinct()) 
                overlay.OnInteractFinalize();
        }

        private void LateUpdate()
        {
            HandleInteractions();
            
            var showHide = SteamVR_Input.GetState(ActionSet, ShowHideAction, SteamVR_Input_Sources.Any, true);
            if (!showHide)
            {
                showHidePressed = false;
                return;
            }
            if (showHidePressed)
                return;

            showHidePressed = true;
            
            windowsVisible = !windowsVisible;
            foreach (var overlay in overlays.Where(x => x.showHideBinding))
                if (!windowsVisible || overlay.wantVisible)
                    overlay.gameObject.SetActive(windowsVisible);
        }

        private void OnDisable()
        {
            foreach (var o in overlays) 
                o.enabled = false;
        }
    }
    

    public class PointerHit
    {
        public readonly InteractableOverlay overlay;
        public readonly LaserPointer pointer;
        public readonly TrackedDevice device;
        public float distance;
        public Vector2 uv;
        public Vector3 point;
        public Vector3 normal;
        public PointerModifier modifier;

        public PointerHit(LaserPointer p, RaycastHit h)
        {
            overlay = h.transform.GetComponent<InteractableOverlay>();
            
            pointer = p;
            device = p.trackedDevice;
            modifier = p.modifier;
            distance = h.distance;
            uv = h.textureCoord;
            normal = h.normal;
            point = h.point;
        }

        public void UpdateFrom(PointerHit p)
        {
            modifier = p.modifier;
            distance = p.distance;
            normal = p.normal;
            point = p.point;
            uv = p.uv;
        }

        public override string ToString()
        {
            return $"{device} on {overlay.gameObject.name} at {uv} ({point})";
        }
    }
}