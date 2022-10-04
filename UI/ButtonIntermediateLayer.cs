using System;
using EasyOverlay.Overlay;
using UnityEngine;
using UnityEngine.UI;

namespace EasyOverlay.UI
{
    public class ButtonIntermediateLayer : MonoBehaviour
    {
        private const int ResDivider = 4;
        
        private byte[,] uvToButtonMap;
        
        private Button[] buttons;
        private Action[] pressedActions;
        private Action[] releasedActions;
        
        private byte nextButton;

        private readonly int[] litButtons = new int[2];

        /// <summary>
        /// Call this when creating your UI
        /// </summary>
        public void Initialize(int numButtons, float width, float height)
        {
            buttons = new Button[numButtons+1];
            pressedActions = new Action[buttons.Length];
            releasedActions = new Action[buttons.Length];
            
            uvToButtonMap = new byte[(int)width / ResDivider, (int)height / ResDivider];
        }

        public void AddButton(Button b, Action onPressed, Action onReleased = null)
        {
            var t = b.GetComponent<RectTransform>();
            var rectT = t.rect;
            var posT = t.anchoredPosition;
            AddButton(b, posT.x, posT.y, rectT.width, rectT.height, onPressed, onReleased);
        }
        
        /// <summary>
        /// Call this with Unity UI coordinates when adding buttons to your UI
        /// </summary>
        public void AddButton(Button b, float x, float y, float w, float h, Action onPressed, Action onReleased)
        {
            buttons[++nextButton] = b;
            pressedActions[nextButton] = onPressed;
            releasedActions[nextButton] = onReleased;
            
            var xMin = (int)(x / ResDivider);
            var yMax = uvToButtonMap.GetLength(1) + (int)(y / ResDivider) - 1;
            var xMax = xMin + (int)(w / ResDivider) - 1;
            var yMin = yMax - (int)(h / ResDivider);

            
            for (var i = xMin; i < xMax; i++)
            for (var j = yMin; j < yMax; j++)
                try
                {
                    uvToButtonMap[i, j] = nextButton;
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Index ouf of range: ({i}, {j}). xywh: ({x}, {y}, {w}, {h}) xyXY: ({xMin}, {yMin}, {xMax}, {yMax}), size: ({uvToButtonMap.GetLength(0)}, {uvToButtonMap.GetLength(1)})");
                    throw;
                }
        }

        public void OnLeft(TrackedDevice d)
        {
            if (!IdxFromDevice(d, out var idx)
                || !ButtonFromIdx(idx, out var button))
                return;
            
            button.OnPointerExit(null);
        }
        
        public void OnMove(PointerHit p)
        {
            if (!IdxFromUv(p.uv, out var idx)
                || !ButtonFromIdx(idx, out var button))
                return;

            var dint = (int) p.device - 1;
            
            var litIdx = litButtons[dint];
            if (litIdx == idx)
                return;
            
            if (ButtonFromIdx(litIdx, out var otherBtn))
                otherBtn.OnPointerExit(null);

            litButtons[dint] = idx;
            button.OnPointerEnter(null);
        }

        public void OnPressed(PointerHit p)
        {
            if (!IdxFromUv(p.uv, out var idx)
                || !ButtonFromIdx(idx, out var button))
                return;
                
            var dint = (int) p.device - 1;
            litButtons[dint] = idx;

            button.OnSelect(null);
            pressedActions[idx]?.Invoke();
        }

        public void OnReleased(PointerHit p)
        {
            if (!IdxFromDevice(p.device, out var idx) 
                || !ButtonFromIdx(idx, out var button)) return;   
            
            button.OnDeselect(null);
            releasedActions[idx]?.Invoke();
        }

        private bool IdxFromDevice(TrackedDevice device, out int idx)
        {
            idx = litButtons[(int) device - 1];
            return idx != 0;
        }

        private bool ButtonFromIdx(int idx, out Button button)
        {
            button = buttons[idx];
            return button != null;
        }
        
        private bool IdxFromUv(Vector2 uv, out int idx)
        {
            var i = (int)((uvToButtonMap.GetLength(0) - 1) * uv.x);
            var j = (int)((uvToButtonMap.GetLength(1) - 1) * uv.y);
            
            try
            {
                idx = uvToButtonMap[i, j];
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                Debug.Log($"Index out of range on uvMap: ({i},{j}), uv: {uv}, size: ({uvToButtonMap.GetLength(0)}, {uvToButtonMap.GetLength(1)})");
            }

            idx = 0;
            return false;
        }
    }
}