using System;
using System.Diagnostics;
using System.Linq;
using EasyOverlay.Overlay;
using EasyOverlay.UI;
using EasyOverlay.X11Screen.Interop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace EasyOverlay.X11Keyboard
{
    public class KeyboardOverlay : GrabbableOverlay
    {
        [SerializeField] 
        public GameObject buttonTemplate;

        [SerializeField] 
        public ButtonIntermediateLayer mainLayer;
        
        [SerializeField]
        public ButtonIntermediateLayer shiftLayer;
        
        [SerializeField]
        public ButtonIntermediateLayer altLayer;

        [SerializeField] 
        public Camera uiCamera;
        
        [SerializeField] 
        public float buttonPadding;

        public static KeyboardOverlay instance;
        private static readonly EasyKeyboardConfig Config = new MyLayout();
        
        private readonly Color mainLayoutColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color shiftLayoutColor = new(0xB0, 0x30, 0x00, 0x80);
        private readonly Color altLayoutColor = new(0x60, 0x00, 0x80, 0x80);

        private float unitSize;
        private int shiftCode;

        private PointerModifier activeModifier;
        private readonly ButtonIntermediateLayer[] modifierLayerMap = new ButtonIntermediateLayer[3];

        public KeyboardOverlay()
        {
            if (instance != null)
                throw new ApplicationException("Can't have more than one KeyboardOverlay components!");
            instance = this;
        }
        
        protected override void Start()
        {
            base.Start();

            if (Config.LoadAndCheckConfig())
            {
                BuildKeyboard();
                shiftCode = Config.keycodes["Shift_R"];
                modifierLayerMap[0] = mainLayer;
                modifierLayerMap[1] = shiftLayer;
                modifierLayerMap[2] = altLayer;
                StartCoroutine(FirstShow());
                
                UpdateTextureBounds(); 
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            uiCamera.enabled = true;
        }

        protected override void OnDisable()
        {
            uiCamera.enabled = false;
            base.OnDisable();
        }
        
        protected override void DrawPointer(PointerHit p, bool primary)
        {
            if (!primary)
            {
                var orig = p.modifier;
                p.modifier = PointerModifier.Neutral;
                p.pointer.OnIntersected(p, false, false);
                p.modifier = orig;
            }
            else 
                p.pointer.OnIntersected(p, true, false);
            
        }
        
        protected override void OnPointerPromotion(PointerHit p) { } // don't switch primary pointers on click

        protected override bool OnMove(PointerHit pointer, bool primary)
        {
            if (clickState?.device == pointer.device)
                return true; // don't move the cursor if it's being held, to avoid accidental inputs
            
            if (primary)
                HandleLayerChange(pointer.device, pointer.modifier);
            
            var mint = (int)activeModifier;
            var layer = modifierLayerMap[mint];
            layer.OnMove(pointer);

            return true;
        }

        protected override bool OnLeft(TrackedDevice device, bool primary)
        {
            if (primary)
            {
                var mint = (int)activeModifier;
                var layer = modifierLayerMap[mint];
                layer.OnLeft(device);

                if (primaryPointer != null) 
                    HandleLayerChange(device, primaryPointer.modifier);
            }
            return true;
        }

        protected override bool OnPressed(PointerHit pointer)
        {
            var mint = (int)activeModifier;
            var layer = modifierLayerMap[mint];
            layer.OnPressed(pointer);
            return true;
        }

        protected override bool OnReleased(PointerHit pointer)
        {
            
            var mint = (int)activeModifier;
            var layer = modifierLayerMap[mint];
            layer.OnReleased(pointer);
            return true;
        }
        
        private void HandleLayerChange(TrackedDevice d, PointerModifier m)
        {
            if (activeModifier == m) return;

            var layer = modifierLayerMap[(int)activeModifier];
            layer.OnLeft(d);
            layer.gameObject.SetActive(false);
            activeModifier = m;
            layer = modifierLayerMap[(int)m];
            layer.gameObject.SetActive(true);
        }

        #region Generate Keyboard

        private void BuildKeyboard()
        {
            // ReSharper disable once PossibleLossOfFraction
            unitSize = texture.width / Config.row_size;

            var wantHeight = (int)(unitSize * Config.sizes.Length);
            if (texture.height != wantHeight)
                Debug.Log($"Check aspect ratio on {texture.name} -- height should be {wantHeight}, got {texture.height}");

            ReinitializeLayers();

            var h = unitSize - 2 * buttonPadding;
            
            for (var row = 0; row < Config.sizes.Length; row++)
            {
                var y = -unitSize * row - buttonPadding;
                var prevUnits = 0f;
                
                for (var col = 0; col < Config.sizes[row].Length; col++)
                {
                    var myUnits = Config.sizes[row][col];
                    var x = unitSize * prevUnits + buttonPadding;
                    var w = unitSize * myUnits - 2 * buttonPadding;
                    
                    CreateKey(mainLayer, Config.main_layout[row][col], false, mainLayoutColor, x, y, w, h);
                    CreateKey(shiftLayer, Config.main_layout[row][col], true, shiftLayoutColor, x, y, w, h);
                    CreateKey(altLayer, Config.alt_layout[row][col], false, altLayoutColor, x, y, w, h);
                    
                    prevUnits += myUnits;
                }
            }
            
            shiftLayer.gameObject.SetActive(false);
            altLayer.gameObject.SetActive(false);
            Destroy(buttonTemplate);
        }

        private void ReinitializeLayers()
        {
            foreach (var layer in new [] {mainLayer, shiftLayer, altLayer})
            {
                // clear container
                foreach (var t in layer.GetComponentsInChildren<Transform>())
                    if (t != layer.transform)
                        Destroy(t.gameObject);

                var numBtn = (layer == altLayer ? Config.alt_layout : Config.main_layout)
                    .SelectMany(x => x).Count(x => x != null);
                layer.Initialize(numBtn, texture.width, texture.height);
            }
        }

        private void CreateKey(ButtonIntermediateLayer layer, string keyStr, bool shift, Color textCol, float x, float y, float w, float h)
        {
            if (keyStr == null)
                return;
            
            var go = Instantiate(buttonTemplate, layer.transform, false);
            go.name = keyStr;
            var keyT = go.GetComponent<RectTransform>();

            keyT.localScale = Vector3.one;
            keyT.sizeDelta = new Vector2(w, h);
            keyT.anchoredPosition = new Vector2(x, y);

            ((RectTransform)go.transform).rect.Set(x, y, w, h);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = Config.NameOfKey(keyStr, shift);
            tmp.color = textCol;

            var btn = go.GetComponent<Button>();
            btn.onClick ??= new Button.ButtonClickedEvent();

            Action pressed;
            Action released;

            if (Config.keycodes.TryGetValue(keyStr, out var keycode))
            {
                pressed = () => OnKeyPressed(keycode, shift);
                released = () => OnKeyReleased(keycode, shift);
            }
            else if (Config.macros.TryGetValue(keyStr, out var macro))
            {
                var events = Config.KeyEventsFromMacro(macro);
                pressed = () =>
                {
                    foreach (var (kc, down) in events) 
                        XScreenCapture.SendKey(kc, down);
                };
                released = null;
            }
            else if (Config.exec_commands.TryGetValue(keyStr, out var argv))
            {
                pressed = () =>
                {
                    var psi = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = argv[0],
                    };
                    
                    foreach (var arg in argv.Skip(1)) 
                        psi.ArgumentList.Add(arg);

                    Process.Start(psi);
                };
                released = null;
            }
            else
            {
                Debug.Log($"No action found for key: {keyStr}");
                return;
            }

            layer.AddButton(btn, x, y, w, h, pressed, released);
        }

        private void OnKeyPressed(int keycode, bool shift)
        {
            if (shift)
            {
                XScreenCapture.SendKey(shiftCode, true);
                XScreenCapture.SendKey(keycode, true);
            }
            else
            {
                XScreenCapture.SendKey(keycode, true);
            }
        }
        
        private void OnKeyReleased(int keycode, bool shift)
        {
            if (shift)
            {
                XScreenCapture.SendKey(keycode, false);
                XScreenCapture.SendKey(shiftCode, false);
            }
            else
            {
                XScreenCapture.SendKey(keycode, false);
            }
        }
        
        #endregion
    }
}