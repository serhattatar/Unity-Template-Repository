/* ==================================================================================
 * 🖥️ UI VIEW (ABSTRACT BASE)
 * ==================================================================================
 * Author:        Muhammet Serhat Tatar (M.S.T.)
 * Description:   Base class for all UI elements (Popups & Panels).
 * ==================================================================================
 */

using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace UI.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : MonoBehaviour
    {
        public bool IsActive { get; protected set; }
        public CanvasGroup CanvasGroup { get; private set; }
        public RectTransform RectTransform { get; private set; }

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            RectTransform = GetComponent<RectTransform>();
        }

        // --- PUBLIC API ---

        public virtual async UniTask Show(object data = null)
        {
            if (IsActive)
            {
                OnRefresh(data);
                return;
            }

            gameObject.SetActive(true);
            IsActive = true;

            if (data != null) Setup(data);

            // Prepare for animation
            CanvasGroup.alpha = 0;
            CanvasGroup.interactable = false;

            OnShowing();
            await AnimateShow();

            CanvasGroup.interactable = true;
            OnShowCompleted();
        }

        public virtual async UniTask Hide()
        {
            if (!IsActive) return;

            CanvasGroup.interactable = false;
            OnHiding();

            await AnimateHide();

            gameObject.SetActive(false);
            IsActive = false;
            OnHideCompleted();
        }

        public void Refresh(object data) => OnRefresh(data);

        // --- ANIMATION STRATEGY ---

        protected virtual async UniTask AnimateShow()
        {
            // Default: Simple Fade In
            await CanvasGroup.DOFade(1f, 0.2f).SetUpdate(true).ToUniTask();
        }

        protected virtual async UniTask AnimateHide()
        {
            // Default: Simple Fade Out
            await CanvasGroup.DOFade(0f, 0.2f).SetUpdate(true).ToUniTask();
        }

        // --- HOOKS ---
        protected virtual void Setup(object data) { }
        protected virtual void OnRefresh(object data) { }
        protected virtual void OnShowing() { }
        protected virtual void OnShowCompleted() { }
        protected virtual void OnHiding() { }
        protected virtual void OnHideCompleted() { }
    }
}