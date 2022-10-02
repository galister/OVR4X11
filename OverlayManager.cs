using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

namespace EasyOverlay
{
    public class OverlayManager : MonoBehaviour
    {
        private const string ActionSet = "default";
        private const string ShowHideAction = "ShowHide";
        
        [SerializeField] public Transform hmd;

        [SerializeField] public Transform spawn;

        private readonly LaserPointer[] controllers = new LaserPointer[3];
        private readonly List<BaseOverlay> overlays = new();

        [SerializeField] public bool windowsVisible;
        private bool showHidePressed = false;

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

        public void RegisterPointer(LaserPointer o, TrackedDevice hand)
        {
            controllers[(int)hand] = o;
        }

        public void DeregisterWindow(BaseOverlay o)
        {
            overlays.Remove(o);
        }

        public LaserPointer GetController(TrackedDevice hand)
        {
            return controllers[(int)hand];
        }

        private readonly WaitForEndOfFrame waitForEndOfFrame = new();

        private IEnumerator Render()
        {
            while (enabled)
            {
                yield return waitForEndOfFrame;

                foreach (var o in overlays.Where(o => o.enabled))
                    o.Render();
            }
            Debug.Log("OverlayRenderer stopped.");
        }

        private void LateUpdate()
        {
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
            {
                overlay.enabled = windowsVisible;
            }
        }
    }
}