using UnityEngine;

namespace EasyOverlay
{
    /// <summary>
    /// An overlay that exists in world space and can be grabbed using the pointers
    /// </summary>
    public abstract class GrabbableOverlay : ClickableOverlay
    {
        /// <summary>
        /// Where to spawn after just being shown.
        /// </summary>
        private Vector3 referencePoint;
        private bool grabbed;

        public override void Show()
        {
            if ((referencePoint - manager.hmd.position).magnitude > 5f)
                // bring closer if we dropped it too far
                referencePoint = Vector3.zero;

            transform.position = manager.spawn.TransformPoint(referencePoint);
            LootAtHmd();
            base.Show();
        }

        // TODO protected override void OnMove(PointerHit pointer, bool primary);
        // What for?

        protected override bool OnGrabbed(PointerHit pointer)
        {
            if (!grabbed)
            {
                transform.parent = manager.GetController(pointer.device).transform;
                grabbed = true;
            }

            return true;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            
            if (grabbed)
                LootAtHmd();
        }

        protected override bool OnScroll(PointerHit pointer, float value)
        {
            if (!grabbed) return false;

            if (pointer.modifier == PointerModifier.RightClick)
                width = Mathf.Clamp(width - Mathf.Pow(value, 3) * 2, 0.1f, 5f);
            else if (pointer.modifier == PointerModifier.MiddleClick)
                return true; // TODO curving
            else
                transform.localPosition += Vector3.forward * (Mathf.Pow(value, 3) * 2);
            
            return true;
        }

        protected override bool OnDropped(PointerHit pointer)
        {
            if (grabbed)
            {
                var t = transform;
                grabbed = false;

                if (t.parent != manager.transform)
                {
                    referencePoint = t.localPosition;
                    t.parent = manager.transform;
                }
            }
            return true;
        }
    }
}