using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasyOverlay.UI
{
    public class EasyButton : MonoBehaviour
    {
        private void Awake()
        {
            var map = gameObject.GetComponentInParent<EasyUiManager>();
            map.Register(gameObject.name, GetComponent<Button>());
        }
    }
}