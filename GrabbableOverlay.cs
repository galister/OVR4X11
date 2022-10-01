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
            transform.position = manager.spawn.TransformPoint(referencePoint);
            LootAtHmd();
            base.Show();
        }

        // TODO protected override void OnMove(PointerHit pointer, bool primary);

        protected override void OnGrabbed(PointerHit pointer)
        {
            transform.parent = manager.GetController(pointer.device).transform;
            grabbed = true;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            
            if (grabbed)
                transform.LookAt(manager.hmd);
        }

        protected override void OnDropped(PointerHit pointer)
        {
            var t = transform;
            grabbed = false;

            if (t.parent != manager.transform)
            {
                referencePoint = t.localPosition;
                t.parent = manager.transform;
            }
        }
    }
}