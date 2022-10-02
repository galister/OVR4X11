using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyOverlay
{
    /// <summary>
    /// Forwards input events from OpenVR to Unity UI
    /// </summary>
    public class ButtonInterface : MonoBehaviour
    {
        private const int ResDivider = 4;
        
        private byte[,] uvToButtonMap;
        
        private Button[] buttons;
        private byte nextButton;

        private readonly Button[] litButtons = new Button[3];

        /// <summary>
        /// Call this when creating your UI
        /// </summary>
        public void Initialize(int numButtons, float width, float height)
        {
            buttons = new Button[numButtons+1];
            
            uvToButtonMap = new byte[(int)width / ResDivider, (int)height / ResDivider];
        }

        /// <summary>
        /// Call this with Unity UI coordinates when adding buttons to your UI
        /// </summary>
        public void AddButton(Button b, float x, float y, float w, float h)
        {
            buttons[++nextButton] = b;
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
            var currentHighlight = litButtons[(int)d];
            if (currentHighlight != null)
                currentHighlight.OnPointerExit(null);
        }
        
        public void OnMove(PointerHit p)
        {
            var button = ButtonFromUv(p.texUv);
            if (button == null) return;

            var dint = (int) p.device;
            
            var litButton = litButtons[dint];
            if (litButton == button)
                return;
            
            if (litButton != null)
                litButton.OnPointerExit(null);

            litButtons[dint] = button;
            button.OnPointerEnter(null);
        }

        public void OnPressed(PointerHit p)
        {
            var button = ButtonFromUv(p.texUv);
            if (button == null) return;

            button.OnPointerClick(new PointerEventData(EventSystem.current){button = PointerEventData.InputButton.Left});
        }

        private Button ButtonFromUv(Vector2 uv)
        {
            var i = (int)((uvToButtonMap.GetLength(0) - 1) * uv.x);
            var j = (int)((uvToButtonMap.GetLength(1) - 1) * uv.y);
            
            try
            {
                var btnIdx = uvToButtonMap[i, j];
                try
                {
                    return btnIdx == 0 ? null : buttons[btnIdx];
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Index out of range on buttons: ({btnIdx}), uv: {uv}, ij: ({i},{j}), size: {buttons.Length}");
                }
            }
            catch (IndexOutOfRangeException)
            {
                Debug.Log($"Index out of range on uvMap: ({i},{j}), uv: {uv}, size: ({uvToButtonMap.GetLength(0)}, {uvToButtonMap.GetLength(1)})");
            }
            return null;
        }
    }
}