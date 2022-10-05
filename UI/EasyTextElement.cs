using TMPro;
using UnityEngine;

namespace EasyOverlay.UI
{
    public class EasyTextElement : MonoBehaviour
    {
        private void Awake()
        {
            var map = gameObject.GetComponentInParent<EasyUiManager>();
            map.Register(gameObject.name, GetComponent<TextMeshProUGUI>());
        }
    }
}