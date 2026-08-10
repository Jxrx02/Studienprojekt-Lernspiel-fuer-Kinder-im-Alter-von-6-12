using UnityEngine;

namespace TowerDefense
{
    [RequireComponent(typeof(RectTransform))]
    public class EdgeIndicatorUI : MonoBehaviour
    {
        [SerializeField] private RectTransform arrowIcon; // optional – rotiert sich; falls leer, dreht sich das ganze Indicator-Objekt

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

            // Fallback: wenn kein separates Pfeil-Icon zugewiesen ist,
            // rotiere den Indicator selbst
            if (arrowIcon == null)
                arrowIcon = _rect;
        }

        public void UpdatePosition(Camera cam, Vector3 worldTarget, RectTransform canvasRect, float padding)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(worldTarget);
            bool behindCamera = viewportPos.z < 0f;

            if (behindCamera)
            {
                viewportPos.x = 1f - viewportPos.x;
                viewportPos.y = 1f - viewportPos.y;
            }

            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 screenPos = new Vector2(
                (viewportPos.x - 0.5f) * canvasSize.x,
                (viewportPos.y - 0.5f) * canvasSize.y
            );

            float angle = Mathf.Atan2(screenPos.y, screenPos.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float halfWidth = canvasSize.x / 2f - padding;
            float halfHeight = canvasSize.y / 2f - padding;

            float slope = cos != 0f ? sin / cos : float.MaxValue;
            Vector2 clamped;

            if (Mathf.Abs(halfHeight * cos) <= Mathf.Abs(halfWidth * sin) && sin != 0f)
                clamped = new Vector2(halfHeight / Mathf.Abs(sin) * cos, halfHeight * Mathf.Sign(sin));
            else
                clamped = new Vector2(halfWidth * Mathf.Sign(cos), halfWidth * slope * Mathf.Sign(cos));

            _rect.anchoredPosition = clamped;

            // arrowIcon ist jetzt garantiert nicht null (Fallback in Awake)
            arrowIcon.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
        }
    }
}