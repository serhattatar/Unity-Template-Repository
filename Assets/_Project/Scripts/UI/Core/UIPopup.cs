/* ==================================================================================
 * 🖥️ UI POPUP (WINDOW)
 * ==================================================================================
 * Description:   For blocking windows (Settings, Win/Fail Screens).
 * Has Scale Animation, Close Button, and enters History Stack.
 * ==================================================================================
 */

using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI.Core;

public class UIPopup : UIView
{
    [Header("Popup Settings")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private bool _destroyOnClose;

    protected override void Awake()
    {
        base.Awake();
        if (_closeButton)
            _closeButton.onClick.AddListener(() => UIManager.Hide(GetType()));
    }

    protected override async UniTask AnimateShow()
    {
        // Juicy Animation: Fade In + Scale Up
        transform.localScale = Vector3.one * 0.8f;

        var fade = CanvasGroup.DOFade(1f, 0.3f).SetUpdate(true).ToUniTask();
        var scale = transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).ToUniTask();

        await UniTask.WhenAll(fade, scale);
    }

    protected override async UniTask AnimateHide()
    {
        // Animation: Fade Out + Scale Down
        var fade = CanvasGroup.DOFade(0f, 0.2f).SetUpdate(true).ToUniTask();
        var scale = transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).ToUniTask();

        await UniTask.WhenAll(fade, scale);
    }

    protected override void OnHideCompleted()
    {
        if (_destroyOnClose) Destroy(gameObject);
    }
}