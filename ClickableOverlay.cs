#define DEBUG_LOG

using Modules;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay
{
    /// <summary>
    /// An overlay that can receive pointer events
    /// </summary>
    public abstract class ClickableOverlay : BaseOverlay
    {
        private const string ActionSet = "default";
        private const string ClickAction = "Click";
        private const string GrabAction = "Grab";
        private const string ScrollAction = "Scroll";

        private PointerHit clickState;
        private PointerHit grabState;

        private PointerHit primaryPointer;
        private PointerHit secondaryPointer;
        
        public Vector2 activeRatio { get; private set; }
        public Vector2 deadZone { get; private set; }

        protected void SetDeadzone(Vector2Int pixels)
        {
            activeRatio = (Vector2)pixels / texture.width;
            deadZone = activeRatio * 0.5f;
            activeRatio = new Vector2(1 - activeRatio.x, 1 - activeRatio.y);
        }

        protected abstract bool OnMove(PointerHit pointer, bool primary);
        protected abstract bool OnLeft(TrackedDevice device, bool primary);
        
        protected abstract bool OnPressed(PointerHit pointer);

        protected abstract bool OnReleased(PointerHit pointer);

        protected abstract bool OnGrabbed(PointerHit pointer);
        protected abstract bool OnDropped(PointerHit pointer);

        protected abstract bool OnScroll(PointerHit pointer, float value);
        
        
        /// <summary>
        /// Ensure both SteamVR Poses and Inputs are done in OnUpdate
        /// </summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            
            if (handle == OpenVR.k_ulOverlayHandleInvalid || !manager.enabled)
                return;

            for (var h = TrackedDevice.LeftHand; h <= TrackedDevice.RightHand; h++)
            {
                var hand = manager.GetController(h);
                var handTransform = hand.transform;

                var handT = handTransform.position;
                var handQ = handTransform.forward;

                var hInput = (SteamVR_Input_Sources) h;
            
                var intersectionParams = new VROverlayIntersectionParams_t
                {
                    vSource = new HmdVector3_t { v0 = handT.x, v1 = handT.y, v2 = -handT.z },
                    vDirection = new HmdVector3_t { v0 = handQ.x, v1 = handQ.y, v2 = -handQ.z },
                    eOrigin = SteamVR.settings.trackingSpace
                };

                VROverlayIntersectionResults_t results = default;
                
                if (overlay.ComputeOverlayIntersection(handle, ref intersectionParams, ref results))
                {   // hit
                    if (IsInDeadZone(results))
                        continue;
                    
                    var pointer = new PointerHit(this, hand, ref results);
                    
                    var grab = SteamVR_Input.GetState(ActionSet, GrabAction, hInput, true);
                    if (grab)
                    {
                        PointerActivated(pointer);
                        HandleGrab(pointer);
                    }
                    else
                        HandleGrabUp(pointer);
                    
                    var click = SteamVR_Input.GetState(ActionSet, ClickAction, hInput, true);
                    if (click)
                    {
                        PointerActivated(pointer);
                        HandleClick(pointer);
                    }
                    else
                        HandleClickUp(pointer);

                    if (!click && !grab)
                        PointerIdle(pointer);

                    if (h != TrackedDevice.RightHand) continue;
                    
                    var scroll = SteamVR_Input.GetVector2(ActionSet, ScrollAction, hInput, true);
                    if (Mathf.Abs(scroll.y) > 0.1f)
                        OnScroll(pointer, scroll.y);
                    
                    PointerOnIntersected(pointer, primaryPointer == pointer);
                }
                else // no hit
                {
                    PointerLeft(h);
                }
            }
        }

        private bool IsInDeadZone(VROverlayIntersectionResults_t res)
        {
            var uv = res.vUVs;
            return uv.v0 < deadZone.x || uv.v0 > 1 - deadZone.x 
                || uv.v1 < deadZone.y || uv.v1 > 1 - deadZone.y;
        }

        protected virtual void PointerOnIntersected(PointerHit p, bool primary)
        {
            p.laser.OnIntersected(p, primary);
        }
        
        private void PointerActivated(PointerHit p)
        {
            if (primaryPointer != null)
            {
                if (primaryPointer.device == p.device)
                    return;

                secondaryPointer = primaryPointer;
            }

            primaryPointer = p;
        }

        private void PointerIdle(PointerHit pointer)
        {
            if (primaryPointer == null || primaryPointer.device == pointer.device)
            {
                primaryPointer = pointer;
                OnMove(pointer, true);
                return;
            }
            secondaryPointer = pointer;
            OnMove(pointer, false);
        }

        private void PointerLeft(TrackedDevice h)
        {
            if (primaryPointer?.device == h)
            {
                if (secondaryPointer != null)
                {
                    primaryPointer = secondaryPointer;
                    secondaryPointer = null;
                }
                else
                    primaryPointer = null;
                
#if DEBUG_LOG
                Debug.Log($"OnLeft {h}, primary");
#endif
                OnLeft(h, true);
            }
            else if (secondaryPointer?.device == h)
            {
                secondaryPointer = null;
#if DEBUG_LOG
                Debug.Log($"OnLeft {h}, non-primary");
#endif
                OnLeft(h, false);
            }
        }

        private void HandleGrab(PointerHit p)
        {
            if (grabState != null)
            {
                if (grabState.device == p.device)
                    return; // held
                
                grabState.UpdateFrom(p);
                
#if DEBUG_LOG
                Debug.Log($"OnDropped {grabState}");
#endif
                OnDropped(grabState);
            }

#if DEBUG_LOG
            Debug.Log($"OnGrabbed {p}");
#endif
            OnGrabbed(p);
            grabState = p;
        }

        private void HandleGrabUp(PointerHit p)
        {
            if (grabState == null) return;
            
            grabState.UpdateFrom(p);
#if DEBUG_LOG
            Debug.Log($"OnDropped {grabState}");
#endif
            OnDropped(grabState);

            grabState = null;
        }
        
        private void HandleClick(PointerHit p)
        {
            if (clickState != null)
            {
                if (clickState.device == p.device)
                    return; // held
                
                clickState.UpdateFrom(p);
#if DEBUG_LOG
                Debug.Log($"OnReleased {clickState}");
#endif
                OnReleased(clickState);
            }

#if DEBUG_LOG
            Debug.Log($"OnPressed {p}");
#endif
            OnMove(p, true);
            OnPressed(p);
            clickState = p;
        }

        private void HandleClickUp(PointerHit p)
        {
            if (clickState == null) return;
            
            clickState.UpdateFrom(p);
                
#if DEBUG_LOG
            Debug.Log($"OnReleased {clickState}");
#endif
            OnReleased(clickState);

            clickState = null;
        }

        // protected virtual void RecreateCollider()
        // {
        //     if (texture == null)
        //         return;
        //     
        //     if (!gameObject.TryGetComponent<MeshCollider>(out var meshCollider))
        //         gameObject.AddComponent<MeshCollider>();
        //
        //     if (meshCollider.sharedMesh == null)
        //         meshCollider.sharedMesh = new Mesh();
        //     else
        //         meshCollider.sharedMesh.Clear();
        //
        //     var mesh = meshCollider.sharedMesh;
        //
        //     var height = width / (texture.width / (float) texture.height);
        //
        //     var halfW = width * 0.5f;
        //     var halfH = height * 0.5f;
        //
        //     var verts = new Vector3[4];
        //     var uv = new Vector2[4];
        //     var normals = new Vector3[4];
        //     var tris = new int[6];
        //
        //     verts[0] = new Vector3(-halfW, -halfH, 0);
        //     verts[1] = new Vector3(-halfW, halfH, 0);
        //     verts[2] = new Vector3(halfW, -halfH, 0);
        //     verts[3] = new Vector3(halfW, halfH, 0);
        //     
        //     uv[0] = Vector2.zero;
        //     uv[1] = Vector2.up;
        //     uv[2] = Vector2.right;
        //     uv[3] = Vector2.one;
        //     
        //     for (var i = 0; i < 4; i++)
        //         normals[i] = Vector3.back;
        //
        //     var t = 0;
        //     tris[t++] = 0;
        //     tris[t++] = 1;
        //     tris[t++] = 2;
        //     
        //     tris[t++] = 2;
        //     tris[t++] = 1;
        //     tris[t++] = 3;
        //
        //     mesh.vertices = verts;
        //     mesh.uv = uv;
        //     mesh.normals = normals;
        //     mesh.triangles = tris;
        //     
        //     mesh.RecalculateBounds();
        // }
    }

    public class PointerHit
    {
        public LaserPointer laser;
        public ClickableOverlay overlay;
        public TrackedDevice device;
        public float distance;
        public Vector2 texUv;
        public Vector2 nativeUv;
        public PointerModifier modifier;

        public PointerHit(ClickableOverlay screen, LaserPointer las, ref VROverlayIntersectionResults_t result)
        {
            overlay = screen;
            laser = las;
            device = las.trackedDevice;
            distance = result.fDistance;
            modifier = las.modifier;
            texUv = new Vector2(
                (result.vUVs.v0 - overlay.deadZone.x) / overlay.activeRatio.x,
                (result.vUVs.v1 - overlay.deadZone.y) / overlay.activeRatio.y
                );
            nativeUv = new Vector2(result.vUVs.v0, result.vUVs.v1);
        }

        public void UpdateFrom(PointerHit p)
        {
            distance = p.distance;
            texUv = p.texUv;
            nativeUv = p.nativeUv;
        }

        public override string ToString()
        {
            return $"{device} on {overlay.gameObject.name} at {texUv}";
        }
    }
}