using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasyOverlay.UI
{
    public class EasyButton : MonoBehaviour
    {
        [SerializeField]
        public string field;

        private void Awake()
        {
            var map = gameObject.GetComponentInParent<EasyUiManager>();
            map.Register(field, GetComponent<Button>());
        }
    }
}