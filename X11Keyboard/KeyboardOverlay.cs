using System.Diagnostics;
using System.Linq;
using EasyOverlay.X11Screen.Interop;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace EasyOverlay.X11Keyboard
{
    public class KeyboardOverlay : GrabbableOverlay
    {
        [SerializeField] 
        public Canvas Canvas;
        
        [SerializeField] 
        public GameObject ButtonTemplate;

        [SerializeField] 
        public ButtonInterface MainLayer;
        
        [SerializeField]
        public ButtonInterface ShiftLayer;
        
        [SerializeField]
        public ButtonInterface AltLayer;

        [SerializeField] 
        public Camera UiCamera;

        [SerializeField] 
        public float UiWidth;
        
        [SerializeField] 
        public float UiHeight;
        
        [SerializeField] 
        public float ButtonPadding;
        
        private readonly Color mainLayoutColor = new(0x00, 0x60, 0x80, 0x80);
        private readonly Color shiftLayoutColor = new(0xB0, 0x30, 0x00, 0x80);
        private readonly Color altLayoutColor = new(0x60, 0x00, 0x80, 0x80);

        private static EasyKeyboardConfig config = new MyLayout();
        private float unitSize;
        private int shiftCode;

        private PointerModifier activeModifier;
        private ButtonInterface[] modifierLayerMap = new ButtonInterface[3];

        private bool dirty = true;

        protected override void Start()
        {
            base.Start();

            if (config.CheckConfig())
            {
                BuildKeyboard();
                shiftCode = config.keycodes["RSHFT"];
                modifierLayerMap[0] = MainLayer;
                modifierLayerMap[1] = ShiftLayer;
                modifierLayerMap[2] = AltLayer;
                StartCoroutine(AfterEnable());
                
                SetDeadzone(new Vector2Int(0, texture.width - texture.height)); 
            }
        }

        private void OnKeyPressed(int keycode, bool shift)
        {
            if (shift)
            {
                XScreenCapture.SendKey(shiftCode, true);
                XScreenCapture.SendKey(keycode, true);
                XScreenCapture.SendKey(keycode, false);
                XScreenCapture.SendKey(shiftCode, false);
            }
            else
            {
                XScreenCapture.SendKey(keycode, true);
                XScreenCapture.SendKey(keycode, false);
            }
        }
        
        public override bool Render()
        {
            if (!base.Render()) 
                return false;
            
            if (true || dirty)
                UploadTexture();
            dirty = false;
            return true;
        }

        private void BuildKeyboard()
        {
            Debug.Log($"Building keyboard {config.name}");

            unitSize = UiWidth / config.row_size;
            UiHeight = unitSize * config.sizes.Length;
            
            ReinitializeLayers();

            Canvas.pixelRect.Set(0, 0, UiWidth, UiHeight);
            var rt = new RenderTexture((int)UiWidth, (int)UiHeight, 0, GraphicsFormat.R16G16B16_SFloat, 0);
            UiCamera.targetTexture = rt;
            texture = rt;

            var h = unitSize - 2 * ButtonPadding;
            
            for (var row = 0; row < config.sizes.Length; row++)
            {
                var y = -unitSize * row - ButtonPadding;
                var prevUnits = 0f;
                
                for (var col = 0; col < config.sizes[row].Length; col++)
                {
                    var myUnits = config.sizes[row][col];
                    var x = unitSize * prevUnits + ButtonPadding;
                    var w = unitSize * myUnits - 2 * ButtonPadding;
                    
                    CreateKey(MainLayer, config.main_layout[row][col], false, mainLayoutColor, x, y, w, h);
                    CreateKey(ShiftLayer, config.main_layout[row][col], true, shiftLayoutColor, x, y, w, h);
                    CreateKey(AltLayer, config.alt_layout[row][col], false, altLayoutColor, x, y, w, h);
                    
                    prevUnits += myUnits;
                }
            }
            
            ShiftLayer.gameObject.SetActive(false);
            AltLayer.gameObject.SetActive(false);
            Destroy(ButtonTemplate);
        }

        private void ReinitializeLayers()
        {
            foreach (var layer in new [] {MainLayer, ShiftLayer, AltLayer})
            {
                // clear container
                foreach (var t in layer.GetComponentsInChildren<Transform>())
                    if (t != layer.transform)
                        Destroy(t.gameObject);

                var numBtn = (layer == AltLayer ? config.alt_layout : config.main_layout)
                    .SelectMany(x => x).Count(x => x != null);
                layer.Initialize(numBtn, UiWidth, UiHeight);
            }
        }

        private void CreateKey(ButtonInterface layer, string keyStr, bool shift, Color textCol, float x, float y, float w, float h)
        {
            if (keyStr == null)
                return;
            
            var go = Instantiate(ButtonTemplate, layer.transform, false);
            go.name = keyStr;
            var keyT = go.GetComponent<RectTransform>();

            keyT.localScale = Vector3.one;
            keyT.sizeDelta = new Vector2(w, h);
            keyT.anchoredPosition = new Vector2(x, y);

            ((RectTransform)go.transform).rect.Set(x, y, w, h);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = config.NameOfKey(keyStr, shift);
            tmp.color = textCol;

            var btn = go.GetComponent<Button>();
            btn.onClick ??= new Button.ButtonClickedEvent();
            layer.AddButton(btn, x, y, w, h);
            
            if (config.keycodes.TryGetValue(keyStr, out var keycode))
                btn.onClick.AddListener(() => OnKeyPressed(keycode, shift));
            else if (config.exec_commands.TryGetValue(keyStr, out var argv))
            {
                btn.onClick.AddListener(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = argv[0],
                    };
                    
                    foreach (var arg in argv.Skip(1)) 
                        psi.ArgumentList.Add(arg);

                    Process.Start(psi);
                });
            }
            else
            {
                Debug.Log($"No action found for key: {keyStr}");
            }
        }

        protected override bool OnMove(PointerHit pointer, bool primary)
        {
            if (clickState?.device == pointer.device)
                return true; // don't move the cursor if it's being held, to avoid accidental inputs
            
            if (primary)
                HandleLayerChange(pointer.device, pointer.modifier);
            
            var mint = (int)activeModifier;
            var layer = modifierLayerMap[mint];
            layer.OnMove(pointer);
            dirty = true;
            
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

                dirty = true;
            }
            return true;
        }

        protected override bool OnPressed(PointerHit pointer)
        {
            var mint = (int)activeModifier;
            var layer = modifierLayerMap[mint];
            layer.OnPressed(pointer);
            dirty = true;
            return true;
        }

        protected override bool OnReleased(PointerHit pointer)
        {
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
    }
}