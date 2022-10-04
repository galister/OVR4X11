using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasyOverlay.UI
{
    public class EasyUiManager : MonoBehaviour
    {
        public Camera uiCamera;
        public ButtonIntermediateLayer layer;
        
        private readonly Dictionary<string, TextMeshProUGUI> textFields = new();
        private readonly Dictionary<string, Button> buttons = new();

        public void Register(string field, TextMeshProUGUI e)
        {
            textFields[field] = e;
        }
        public void Register(string field, Button e)
        {
            buttons[field] = e;
        }

        public TextMeshProUGUI GetTextField(string field)
        {
            return textFields.TryGetValue(field, out var elem) ? elem : null;
        }
        public Button GetButton(string field)
        {
            return buttons.TryGetValue(field, out var elem) ? elem : null;
        }
    }
}