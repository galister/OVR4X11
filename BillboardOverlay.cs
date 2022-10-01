using UnityEngine;

namespace EasyOverlay
{
    // A billboard icon to debug positions
    public class BillboardOverlay : BaseOverlay
    {
        protected override void Update()
        {
            base.Update();

            var hmd = manager.hmd;
            var dir = hmd.position - transform.position;
            var localDir = transform.InverseTransformDirection(dir);
            var dir2d = new Vector2(localDir.x, localDir.z).normalized;
            var angle = Mathf.Atan2(dir2d.y, dir2d.x) * -Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.AngleAxis(angle, transform.up);
        }
    }
}