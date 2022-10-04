using System;
using EasyOverlay.X11Screen.Interop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasyOverlay
{
    public sealed class WatchOverlay : ClickableOverlay
    {
        [SerializeField] 
        public string altTimeZoneDisplayName;

        [SerializeField] 
        public float altTimeZoneGmtOffsetHours;

        [SerializeField] 
        public string altTimeZone2DisplayName;

        [SerializeField] 
        public float altTimeZone2GmtOffsetHours;
        
        [SerializeField]
        public TextMeshProUGUI time;
        
        [SerializeField]
        public TextMeshProUGUI altTime;
        
        [SerializeField]
        public TextMeshProUGUI altTime2;
        
        [SerializeField]
        public TextMeshProUGUI weekday;
        
        [SerializeField]
        public TextMeshProUGUI date;

        [SerializeField] 
        public Button keyboardButton;
        
        [SerializeField] 
        public Button screen1Button;
        
        [SerializeField] 
        public Button screen2Button;
        
        [SerializeField] 
        public Button screen3Button;

        [SerializeField] 
        public ButtonInterface uiLayer;

        [SerializeField] public Camera UiCamera;

        protected override void OnEnable()
        {
            base.OnEnable();
            UiCamera.enabled = true;
        }

        protected override void OnDisable()
        {
            UiCamera.enabled = false;
            base.OnDisable();
        }

        protected override void Update()
        {
            base.Update();
            
            var toHmd = manager.hmd.position - transform.position;
            var dot = Vector3.Dot(toHmd, transform.forward);
            
            if (dot > -0.20f)
            {
                if (visible)
                    Hide();
                return;
            }
            if (!visible && dot > -0.35f)
                return;

            if (!visible)
                Show();

            var localDt = DateTime.Now;
            time.text = $"{localDt:HH:mm}";
            date.text = localDt.ToShortDateString();
            weekday.text = localDt.DayOfWeek.ToString();

            var altDt = DateTime.UtcNow.AddHours(altTimeZoneGmtOffsetHours);
            altTime.text = $"{altDt:HH:mm}";

            var alt2Dt = DateTime.UtcNow.AddHours(altTimeZone2GmtOffsetHours);
            altTime2.text = $"{alt2Dt:HH:mm}";
        }

        protected override void Start()
        {
            base.Start();

            RefreshLayout();
            SetDeadzone(new Vector2Int(0, texture.width - texture.height));
        }

        private void RefreshLayout()
        {
            var numScreens = XScreenCapture.NumScreens();
            
            uiLayer.Initialize(numScreens + 1, texture.width, texture.height);

            var t = keyboardButton.GetComponent<RectTransform>();
            uiLayer.AddButton(keyboardButton);
            
            SetButtonActive(screen1Button, numScreens > 0);
            SetButtonActive(screen2Button, numScreens > 1);
            SetButtonActive(screen3Button, numScreens > 2);
        }

        private void SetButtonActive(Button b, bool active)
        {
            var text = b.GetComponentInChildren<TextMeshProUGUI>();
            if (active)
            {
                b.image.color = Color.white;
                text.color = Color.white;
                var t = keyboardButton.GetComponent<RectTransform>();
                uiLayer.AddButton(keyboardButton);
            }
            else
            {
                var col = b.colors.normalColor;
                b.image.color = new Color(1f-col.r, 1f-col.g, 1f-col.b);
                text.color = Color.gray;
            }
        }

        public override bool Render()
        {
            if (!base.Render()) 
                return false;
            
            UploadTexture();
            return true;
        }

        protected override bool OnMove(PointerHit pointer, bool primary)
        {
            if (clickState?.device == pointer.device)
                return true; // don't move the cursor if it's being held, to avoid accidental inputs
            
            uiLayer.OnMove(pointer);
            
            return true;
        }

        protected override bool OnLeft(TrackedDevice device, bool primary)
        {
            if (primary) 
                uiLayer.OnLeft(device);
            return true;
        }

        protected override bool OnPressed(PointerHit pointer)
        {
            uiLayer.OnPressed(pointer);
            return true;
        }

        protected override bool OnReleased(PointerHit pointer)
        {
            return true;
        }

        protected override bool OnGrabbed(PointerHit pointer)
        {
            return true;
        }

        protected override bool OnDropped(PointerHit pointer)
        {
            return true;
        }

        protected override bool OnScroll(PointerHit pointer, float value)
        {
            return true;
        }
    }
}