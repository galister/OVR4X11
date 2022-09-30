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

        protected abstract void OnMove(PointerHit pointer, bool primary);
        protected abstract void OnLeft(TrackedDevice device, bool primary);
        
        protected abstract void OnPressed(PointerHit pointer);

        protected abstract void OnReleased(PointerHit pointer);

        protected abstract void OnGrabbed(PointerHit pointer);
        protected abstract void OnDropped(PointerHit pointer);

        protected abstract void OnScroll(PointerHit pointer, float value);
        
        
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
                var handT = hand.transform.localPosition;
                var handR = hand.transform.forward * -1;

                var hInput = (SteamVR_Input_Sources) h;
            
                var intersectionParams = new VROverlayIntersectionParams_t
                {
                    vSource = new HmdVector3_t { v0 = handT.x, v1 = handT.y, v2 = handT.z },
                    vDirection = new HmdVector3_t { v0 = handR.x, v1 = handR.y, v2 = handR.z },
                    eOrigin = SteamVR.settings.trackingSpace
                };

                VROverlayIntersectionResults_t results = default;
                
                if (overlay.ComputeOverlayIntersection(handle, ref intersectionParams, ref results))
                {   // hit
                    var pointer = new PointerHit(h, hand.modifier, ref results);
                    
                    Debug.Log($"{h} pointer @ {gameObject.name} {pointer.uv}");
                    
                    var click = SteamVR_Input.GetState(ActionSet, ClickAction, hInput, true);
                    if (click)
                    {
                        PointerActivated(pointer);
                        HandleClick(pointer);
                    }
                    else
                        HandleClickUp(pointer);
                    
                    var grab = SteamVR_Input.GetState(ActionSet, GrabAction, hInput, true);
                    if (grab)
                    {
                        PointerActivated(pointer);
                        HandleGrab(pointer);
                    }
                    else
                        HandleGrabUp(pointer);

                    if (!click && !grab)
                        PointerIdle(pointer);

                    if (h != TrackedDevice.RightHand) continue;
                    
                    var scroll = SteamVR_Input.GetVector2(ActionSet, ScrollAction, hInput, true);
                    if (Mathf.Abs(scroll.y) > 0.1f)
                        OnScroll(pointer, scroll.y);
                    
                    PointerOnIntersected(hand, results.fDistance, primaryPointer == pointer);
                }
                else // no hit
                {
                    PointerLeft(h);
                }
            }
        }

        protected virtual void PointerOnIntersected(LaserPointer l, float length, bool primary)
        {
            l.OnIntersected(length, primary);
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
                
                OnLeft(h, true);
            }
            else if (secondaryPointer?.device == h)
            {
                secondaryPointer = null;
                OnLeft(h, false);
            }
        }

        private void HandleGrab(PointerHit p)
        {
            if (grabState != null && grabState.DifferentTypeFrom(p))
            {
                grabState.UpdateFrom(p);
                OnDropped(grabState);
            }
            OnGrabbed(p);
            grabState = p;
        }

        private void HandleGrabUp(PointerHit p)
        {
            if (grabState == null) return;
            
            grabState.UpdateFrom(p);
            OnDropped(grabState);

            grabState = null;
        }
        
        private void HandleClick(PointerHit p)
        {
            if (clickState != null && clickState.DifferentTypeFrom(p))
            {
                clickState.UpdateFrom(p);
                
                OnReleased(clickState);
            }

            OnPressed(p);
            clickState = p;
        }

        private void HandleClickUp(PointerHit p)
        {
            if (clickState == null) return;
            
            clickState.UpdateFrom(p);
                
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
        public TrackedDevice device;
        public float distance;
        public Vector2 uv;
        public Vector3 position;
        public Vector3 normal;
        public PointerModifier modifier;

        public PointerHit()
        {
            
        }

        public PointerHit(TrackedDevice dev, PointerModifier mod, ref VROverlayIntersectionResults_t result)
        {
            device = dev;
            distance = result.fDistance;
            uv = new Vector2(result.vUVs.v0, result.vUVs.v1);
            position = new Vector3(result.vPoint.v0, result.vPoint.v1, result.vPoint.v2);
            normal = new Vector3(result.vPoint.v0, result.vPoint.v1, result.vPoint.v2);
            modifier = mod;
        }

        public void UpdateFrom(PointerHit p)
        {
            distance = p.distance;
            normal = p.normal;
            position = p.position;
            uv = p.uv;
        }

        public bool DifferentTypeFrom(PointerHit p)
        {
            return device != p.device || modifier != p.modifier;
        }
    }
}