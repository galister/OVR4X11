using TMPro;
using UnityEngine;

namespace EasyOverlay.UI
{
    public class EasyTextElement : MonoBehaviour
    {
        [SerializeField]
        public string field;

        private void Awake()
        {
            var map = gameObject.GetComponentInParent<EasyUiManager>();
            map.Register(field, GetComponent<TextMeshProUGUI>());
        }
    }
}