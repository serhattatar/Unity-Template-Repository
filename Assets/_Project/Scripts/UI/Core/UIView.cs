using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace UI.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : MonoBehaviour
    {
        [Header("Base Settings")]
        [Tooltip("If true, the object is destroyed when hidden.")]
        [SerializeField] private bool _destroyOnClose = false;

        [Tooltip("Optional: Assign the close button (X) to auto-bind hide action.")]
        [SerializeField] private Button _closeButton;

        private CanvasGroup _canvasGroup;
        public bool IsActive { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => UIManager.Hide(this.GetType()));
        }

        // --- PUBLIC API ---

        public async UniTask Show(object data = null)
        {
            if (IsActive) return;

            gameObject.SetActive(true);
            IsActive = true;

            // Inject payload data if provided
            if (data != null) Setup(data);

            // Prepare for animation
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            transform.localScale = Vector3.one * 0.9f;

            OnShowing(); // Hook for derived classes

            // DOTween Animation (Fade In + Scale Up)
            var fadeTask = _canvasGroup.DOFade(1f, 0.3f).SetUpdate(true).ToUniTask();
            var scaleTask = transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).ToUniTask();

            await UniTask.WhenAll(fadeTask, scaleTask);

            _canvasGroup.interactable = true;
            OnShowCompleted();
        }

        public async UniTask Hide()
        {
            if (!IsActive) return;

            _canvasGroup.interactable = false;
            OnHiding();

            // DOTween Animation (Fade Out + Scale Down)
            var fadeTask = _canvasGroup.DOFade(0f, 0.2f).SetUpdate(true).ToUniTask();
            var scaleTask = transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).ToUniTask();

            await UniTask.WhenAll(fadeTask, scaleTask);

            gameObject.SetActive(false);
            IsActive = false;
            OnHideCompleted();

            if (_destroyOnClose) Destroy(gameObject);
        }

        // --- OVERRIDABLE METHODS ---

        /// <summary>
        /// Override to handle data passed during Show().
        /// </summary>
        protected virtual void Setup(object data) { }

        protected virtual void OnShowing() { }
        protected virtual void OnShowCompleted() { }
        protected virtual void OnHiding() { }
        protected virtual void OnHideCompleted() { }
    }
}