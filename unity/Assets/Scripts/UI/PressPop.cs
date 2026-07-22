using UnityEngine;
using UnityEngine.EventSystems;

namespace Reveal.UI
{
    /// <summary>
    /// Scale-down-on-press feedback for tappable elements (0.94 pressed →
    /// spring back). Gives buttons a physical, responsive feel per the
    /// scale-feedback interaction guideline. Non-layout (transform only).
    /// </summary>
    public class PressPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Vector3 _base = Vector3.one;
        Vector3 _target = Vector3.one;

        void OnEnable() { transform.localScale = _base; _target = _base; }

        public void OnPointerDown(PointerEventData e) => _target = _base * 0.94f;
        public void OnPointerUp(PointerEventData e) => _target = _base;

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _target, Time.unscaledDeltaTime * 16f);
        }
    }
}
