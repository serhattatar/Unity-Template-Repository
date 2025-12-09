/* ==================================================================================
 * 📱 M.S.T. SAFE AREA CONTROLLER
 * ==================================================================================
 * Author:        Muhammet Serhat Tatar (M.S.T.)
 * Description:   Automatically adjusts UI to fit within the safe area (Notch/Dynamic Island).
 * Uses Event-Driven approach (No Update Loop) for maximum performance.
 * ==================================================================================
 */

using UnityEngine;

namespace UI.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        // 🚀 PRO TIP: Only calculates when layout/screen dimensions actually change.
        // This eliminates the need for checking in Update().
        private void OnRectTransformDimensionsChange()
        {
            Refresh();
        }

        private void Refresh()
        {
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            // Double-check to prevent redundant calculations
            if (safeArea == _lastSafeArea && screenSize == _lastScreenSize)
                return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            ApplySafeArea(safeArea);
        }

        private void ApplySafeArea(Rect r)
        {
            // Ignore if in Editor logic where screen might be 0
            if (Screen.width == 0 || Screen.height == 0) return;

            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}