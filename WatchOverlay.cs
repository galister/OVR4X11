using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EasyOverlay.Overlay;
using EasyOverlay.UI;
using EasyOverlay.X11Keyboard;
using EasyOverlay.X11Screen;
using EasyOverlay.X11Screen.Interop;
using Valve.VR;

namespace EasyOverlay
{
    public sealed class WatchOverlay : InteractableOverlay
    {
        [SerializeField] 
        public string altTimeZone;

        [SerializeField] 
        public string altTimeZone2;

        [SerializeField] 
        public EasyUiManager ui;

        private TimeZoneInfo altTz;
        private TimeZoneInfo altTz2;

        private int maxDevices = 10;
        private float[] batteryStates;
        private bool[] chargeStates;

        private Task batteryTask;
        private DateTime nextBatteryCheck = DateTime.MinValue;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            ui.uiCamera.enabled = true;
        }

        protected override void OnDisable()
        {
            ui.uiCamera.enabled = false;
            base.OnDisable();
        }

        protected override void Update()
        {
            base.Update();
            
            var toHmd = manager.hmd.position - transform.position;
            var dot = Vector3.Dot(toHmd, transform.forward);
            
            if (dot > 0.20f)
            {
                if (visible)
                    Hide();
                return;
            }
            //if (!visible && dot > -0.5f)
            //    return;

            if (!visible)
                Show();

            var localDt = DateTime.Now;
            ui.GetTextField("time").text = $"{localDt:HH:mm}";
            ui.GetTextField("date").text = localDt.ToShortDateString();
            ui.GetTextField("weekday").text = localDt.DayOfWeek.ToString();

            var altDt = TimeZoneInfo.ConvertTime(localDt, TimeZoneInfo.Local, altTz);
            ui.GetTextField("alt_time").text = $"{altDt:HH:mm}";

            var alt2Dt = TimeZoneInfo.ConvertTime(localDt, TimeZoneInfo.Local, altTz2);
            ui.GetTextField("alt_time2").text = $"{alt2Dt:HH:mm}";

            if (nextBatteryCheck < DateTime.UtcNow)
            {
                if (batteryTask == null)
                {
                    batteryTask = UpdateBatteries();
                }
                else if (batteryTask.IsCompleted)
                {
                    RenderBatteries();
                    batteryTask = null;
                    nextBatteryCheck = DateTime.UtcNow.AddSeconds(5);
                }
            }
            
            UploadTexture();
        }
        
        private static readonly Color discharging = new(0, 0.7f, 0, 1);
        private static readonly Color critical = new(0.7f, 0, 0, 1);
        private static readonly Color charging = new(0, 0.5f, 0.7f, 1);
        private static readonly Color inactive = new(0, 0, 0, 0);
        
        private void RenderBatteries()
        {
            for (var i = 0; i < batteryStates.Length; i++)
            {
                var field = ui.GetTextField($"b{i}");
                field.text = ((int)Mathf.Clamp(batteryStates[i] * 100, 0, 99)).ToString();
                field.color = chargeStates[i]
                    ? charging
                    : Color.Lerp(critical, discharging, batteryStates[i] + 0.4f);
            }

            for (var i = batteryStates.Length; i < maxDevices; i++) 
                ui.GetTextField($"b{i}").color = inactive;
        }

        private readonly uint[] deviceIds = new uint[OpenVR.k_unMaxTrackedDeviceCount];
        
        private uint GetDeviceIds(ETrackedDeviceClass type)
        {
            return OpenVR.System.GetSortedTrackedDeviceIndicesOfClass(type, deviceIds, 0);
        }

        private Task UpdateBatteries()
        {
            var numDevs = GetDeviceIds(ETrackedDeviceClass.GenericTracker);
            var lastErr = ETrackedPropertyError.TrackedProp_Success;

            chargeStates = new bool[numDevs + 2];
            batteryStates = new float[chargeStates.Length];

            var tgtIdx = 1;
            
            for (var i = 0U; i < numDevs; i++)
            {
                tgtIdx++;

                batteryStates[tgtIdx] = OpenVR.System.GetFloatTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref lastErr);
                chargeStates[tgtIdx] = OpenVR.System.GetBoolTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool, ref lastErr);
            }

            numDevs = GetDeviceIds(ETrackedDeviceClass.Controller);
            numDevs = Math.Min(numDevs, 2);
            for (var i = 0U; i < numDevs; i++)
            {
                batteryStates[tgtIdx] = OpenVR.System.GetFloatTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref lastErr);
                
                if (lastErr != ETrackedPropertyError.TrackedProp_Success)
                    continue;

                chargeStates[tgtIdx] = OpenVR.System.GetBoolTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool, ref lastErr);
                
                if (lastErr != ETrackedPropertyError.TrackedProp_Success)
                    continue;
                
                tgtIdx++;
            }
            return Task.CompletedTask;
        }

        protected override void Start()
        {
            base.Start();

            RefreshLayout();
            UpdateTextureBounds();

            altTz = VerifyTimeZone(altTimeZone);
            altTz2 = VerifyTimeZone(altTimeZone2);
            
            ui.GetTextField("alt_time_label").text = altTimeZone.Split('/').Last();
            ui.GetTextField("alt_time2_label").text = altTimeZone2.Split('/').Last();
        }

        private TimeZoneInfo VerifyTimeZone(string s)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(s);
            }
            catch (TimeZoneNotFoundException)
            {
                Debug.Log($"Unknown timezone {s}! Run <b>timedatectl list-timezones</b to see available timezones!");
            }
            return TimeZoneInfo.Utc;
        }
        
        private void RefreshLayout()
        {
            var screens = FindObjectsOfType<ScreenOverlay>(true).OrderBy(x => x.screen).ToArray();
            
            ui.layer.Initialize(screens.Length + 1, texture.width, texture.height);

            var keyboardButton = ui.GetButton("keyboard");
            ui.layer.AddButton(keyboardButton,
                () => { KeyboardOverlay.instance.enabled = !KeyboardOverlay.instance.enabled; });


            var i = 0;
            for (; i < screens.Length; i++)
            {
                var i1 = i;
                SetupScreenButton(ui.GetButton($"screen{i+1}"), true, () =>
                {
                    if (screens[i1].visible)
                        screens[i1].Hide();
                    else
                        screens[i1].Show();
                });
            }

            for (; i < 3; i++)
                SetupScreenButton(ui.GetButton($"screen{i+1}"), false, null);
        }

        private void SetupScreenButton(Button b, bool active, Action clickAction)
        {
            var text = b.GetComponentInChildren<TextMeshProUGUI>();
            if (active)
            {
                b.image.color = Color.white;
                text.color = Color.white;
                ui.layer.AddButton(b, clickAction);
            }
            else
            {
                var col = b.colors.normalColor;
                b.image.color = new Color(1f-col.r, 1f-col.g, 1f-col.b);
                text.color = Color.gray;
            }
        }

        protected override bool OnMove(PointerHit pointer, bool primary)
        {
            if (clickState?.device == pointer.device)
                return true; // don't move the cursor while it's being held, to avoid accidental inputs
            
            ui.layer.OnMove(pointer);
            
            return true;
        }

        protected override bool OnLeft(TrackedDevice device, bool primary)
        {
            if (primary) 
                ui.layer.OnLeft(device);
            return true;
        }

        protected override bool OnPressed(PointerHit pointer)
        {
            ui.layer.OnPressed(pointer);
            return true;
        }

        #region Unused
        
        protected override bool OnReleased(PointerHit pointer)
        {
            ui.layer.OnReleased(pointer);
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

        #endregion
    }
}