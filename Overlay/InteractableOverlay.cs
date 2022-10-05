using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay.Overlay
{
    /// <summary>
    /// An overlay that can receive pointer events
    /// </summary>
    public abstract class InteractableOverlay : BaseOverlay
    {
        private const string ActionSet = "default";
        private const string ClickAction = "Click";
        private const string GrabAction = "Grab";
        private const string ScrollAction = "Scroll";

        protected PointerHit clickState;
        protected PointerHit grabState;
        protected PointerHit primaryPointer { get; private set; }
        protected PointerHit secondaryPointer { get; private set; }

        protected Vector2 activeRatio { get; private set; }
        private Vector2 interactiveSize { get; set; }

        private List<PointerHit> hitsThisFrame = new(2);

        private float referenceWidth;
        protected void UpdateTextureBounds()
        {
            referenceWidth = width;
            
            var pixels = new Vector2Int(0, texture.width - texture.height);
            activeRatio = (Vector2)pixels / texture.width;
            activeRatio = new Vector2(1 - activeRatio.x, 1 - activeRatio.y);
            interactiveSize = new Vector2(width * activeRatio.x, width * activeRatio.y);

            if (!gameObject.TryGetComponent<MeshCollider>(out var meshCollider))
                meshCollider = gameObject.AddComponent<MeshCollider>();

            var halfSize = interactiveSize / 2;
            
            var vertices = new Vector3[]
            {
                new(-halfSize.x, -halfSize.y),
                new(-halfSize.x, halfSize.y),
                new(halfSize.x, -halfSize.y),
                new(halfSize.x, halfSize.y)
            };

            var normals =  new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            var uv = new Vector2[] { new(0,0), new(0, 1), new(1,0), new(1,1) };
            var triangles = new[] { 0, 1, 2,  2, 1 ,3 };
            
            var mesh = new Mesh
            {
                vertices = vertices,
                normals = normals,
                uv = uv,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = mesh;
        }

        protected internal override void UploadWidth()
        {
            if (referenceWidth != 0)
                transform.localScale = Vector3.one * (width / referenceWidth);
            base.UploadWidth();
        }

        /// <summary>
        /// Guaranteed to be called before OnPressed.
        /// </summary>
        protected abstract bool OnMove(PointerHit pointer, bool primary);
        
        protected abstract bool OnLeft(TrackedDevice device, bool primary);
        
        protected abstract bool OnPressed(PointerHit pointer);

        protected abstract bool OnReleased(PointerHit pointer);

        protected abstract bool OnGrabbed(PointerHit pointer);
        protected abstract bool OnDropped(PointerHit pointer);

        protected abstract bool OnScroll(PointerHit pointer, float value);
        
        /// <summary>
        /// Called after all interactions were handled.
        /// </summary>
        protected virtual void DrawPointer(PointerHit p, bool primary)
        {
            p.pointer.OnIntersected(p, primary, false);
        }
        
        /// <summary>
        /// Called when a pointer needs to be promoted to primary due to a click, grab or scroll.
        /// </summary>
        protected virtual void OnPointerPromotion(PointerHit p)
        {
            if (primaryPointer != null)
            {
                if (primaryPointer.device == p.device)
                    return;

                secondaryPointer = primaryPointer;
            }

            primaryPointer = p;
        }
        
        /// <summary>
        /// Called by OverlayManager after LateUpdate, for each pointer pointed at us.
        /// </summary>
        internal void OnInteractPointer(PointerHit hit)
        {
            hitsThisFrame.Add(hit);
        }
        
        /// <summary>
        /// Called by OverlayManager after all OnPointerInternal calls were made.
        /// </summary>
        internal void OnInteractFinalize()
        {
            var primary = HandleBoolInput(GrabAction, HandleGrab, HandleGrabUp);
            if (primary != null)
            {
                var tempHits = hitsThisFrame;
                hitsThisFrame = new List<PointerHit> { primary };
                HandleBoolInput(ClickAction, HandleClick, HandleClickUp);
                HandleScrollInput(ScrollAction, x => Mathf.Abs(x) > 0.1f, OnScroll);
                hitsThisFrame = tempHits;
            }
            else
            {
                primary = HandleBoolInput(ClickAction, HandleClick, HandleClickUp);
                primary ??= HandleScrollInput(ScrollAction, x => Mathf.Abs(x) > 0.1f, OnScroll);
            }

            foreach (var p in hitsThisFrame)
            {
                var isPrimary = p == primary;
                if (!isPrimary)
                    PointerIdle(p);
                DrawPointer(p, isPrimary);
            }

            hitsThisFrame.Clear();
        }

        /// <summary>
        /// Called by OverlayManager instead of OnPointerInternal if the cursor is no longer on this overlay.
        /// </summary>
        /// <param name="device"></param>
        protected internal void OnInteractLeft(TrackedDevice device)
        {
            if (primaryPointer?.device == device)
            {
                if (secondaryPointer != null)
                {
                    primaryPointer = secondaryPointer;
                    secondaryPointer = null;
                }
                else
                    primaryPointer = null;
                
#if DEBUG_LOG
                Debug.Log($"{name}: OnLeft {device}, primary");
#endif
                OnLeft(device, true);
            }
            else if (secondaryPointer?.device == device)
            {
                secondaryPointer = null;
#if DEBUG_LOG
                Debug.Log($"{name}: OnLeft {device}, non-primary");
#endif
                OnLeft(device, false);
            }
        }

        private PointerHit HandleScrollInput<T>(string action, Func<float, bool> test, 
            Func<PointerHit, float, T> pass)
        {
            foreach (var p in hitsThisFrame)
            {
                var result = SteamVR_Input.GetVector2(ActionSet, action, (SteamVR_Input_Sources)p.device, true);
                if (test(result.y))
                {
                    OnPointerPromotion(p);
                    pass(p, result.y);
                    return p;
                }
            }
            return null;
        }
        
        private PointerHit HandleBoolInput(string action, Action<PointerHit> pass, Action<PointerHit> fail)
        {
            foreach (var p in hitsThisFrame)
            {
                var result = SteamVR_Input.GetState(ActionSet, action, (SteamVR_Input_Sources)p.device, true);
                if (result)
                {
                    OnPointerPromotion(p);
                    pass(p);
                    return p;
                }
                fail(p);
            }
            return null;
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

        private void HandleGrab(PointerHit p)
        {
            if (grabState != null)
            {
                if (grabState.device == p.device)
                    return; // held
                
                grabState.UpdateFrom(p);
                
#if DEBUG_LOG
                Debug.Log($"{name}: OnDropped {grabState}");
#endif
                OnDropped(grabState);
            }

#if DEBUG_LOG
            Debug.Log($"{name}: OnGrabbed {p}");
#endif
            OnGrabbed(p);
            grabState = p;
        }

        private void HandleGrabUp(PointerHit p)
        {
            if (grabState == null) return;
            
            grabState.UpdateFrom(p);
#if DEBUG_LOG
            Debug.Log($"{name}: OnDropped {grabState}");
#endif
            OnDropped(grabState);

            grabState = null;
        }
        
        private void HandleClick(PointerHit p)
        {
            if (clickState != null)
            {
                if (clickState.device == p.device)
                {
                    OnMove(p, true);
                    return; // held
                }

                clickState.UpdateFrom(p);
#if DEBUG_LOG
                Debug.Log($"{name}: OnReleased {clickState}");
#endif
                OnReleased(clickState);
            }

#if DEBUG_LOG
            Debug.Log($"{name}: OnPressed {p}");
#endif
            clickState = p;
            OnMove(p, true);
            OnPressed(p);
        }

        private void HandleClickUp(PointerHit p)
        {
            if (clickState == null　|| p.device != clickState.device) return;
            
            clickState.UpdateFrom(p);
                
#if DEBUG_LOG
            Debug.Log($"{name}: OnReleased {clickState}");
#endif
            OnReleased(clickState);

            clickState = null;
        }
    }
}