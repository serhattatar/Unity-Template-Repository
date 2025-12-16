/* ==================================================================================
 * 🖥️ UI MANAGER (V3.1 - FIX)
 * ==================================================================================
 * Author:        Muhammet Serhat Tatar (M.S.T.)
 * Description:   Fixed missing overload for Hide(Type).
 * ==================================================================================
 */

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UI.Core;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

[DefaultExecutionOrder(-50)]
public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Image _blocker; // Blocks input behind Popups

    [Header("Configuration")]
    [Tooltip("Assign all UI Prefabs (Popups & Panels) here.")]
    [SerializeField] private List<UIView> _sceneViews = new List<UIView>();

    private static UIManager _instance;
    private Dictionary<Type, UIView> _registry = new Dictionary<Type, UIView>();

    // Only Popups go into the history stack
    private Stack<UIPopup> _popupHistory = new Stack<UIPopup>();

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;

        InitializeRegistry();

        if (_blocker) _blocker.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Handle Android Back Button / Escape Key for Popups only
        if (Input.GetKeyDown(KeyCode.Escape) && _popupHistory.Count > 0)
        {
            Back();
        }
    }

    // --- STATIC API ---

    // Generic Hide (Compile-time known types)
    public static async void Show<T>(object data = null) where T : UIView => await _instance.ShowInternal(typeof(T), data);
    public static async void Hide<T>() where T : UIView => await _instance.HideInternal(typeof(T));

    // 👇 EKSİK OLAN KISIM BURASIYDI (Type Parameter Overload) 👇
    public static void Hide(Type type) => _instance?.HideInternal(type).Forget();

    public static void Refresh<T>(object data) where T : UIView => _instance?.RefreshInternal(typeof(T), data);
    public static void Back() => _instance?.HandleBack();

    // --- INTERNAL LOGIC ---

    private async UniTask ShowInternal(Type type, object data)
    {
        if (!_registry.TryGetValue(type, out var view))
        {
            GameLogger.Error($"[UIManager] View not found: {type.Name}");
            return;
        }

        view.transform.SetAsLastSibling(); // Bring to front

        // POLYMORPHIC CHECK
        if (view is UIPopup popup)
        {
            _popupHistory.Push(popup);
            UpdateBlocker(popup);
        }
        else if (view is UIPanel panel)
        {
            // Panels do not affect the stack or blocker
        }

        await view.Show(data);
    }

    private async UniTask HideInternal(Type type)
    {
        if (!_registry.TryGetValue(type, out var view)) return;
        if (!view.IsActive) return;

        if (view is UIPopup popup)
        {
            if (_popupHistory.Count > 0 && _popupHistory.Peek() == popup)
            {
                _popupHistory.Pop();
            }

            // Update blocker for the next popup in stack
            UpdateBlocker(_popupHistory.Count > 0 ? _popupHistory.Peek() : null);
        }

        await view.Hide();
    }

    private void RefreshInternal(Type type, object data)
    {
        if (_registry.TryGetValue(type, out var view))
            view.Refresh(data);
    }

    private void HandleBack()
    {
        if (_popupHistory.Count > 0)
        {
            HideInternal(_popupHistory.Peek().GetType()).Forget();
        }
    }

    private void UpdateBlocker(UIView target)
    {
        if (!_blocker) return;

        bool show = target != null;
        _blocker.gameObject.SetActive(show);

        if (show)
        {
            // Place blocker immediately behind the target popup
            _blocker.transform.SetSiblingIndex(Mathf.Max(0, target.transform.GetSiblingIndex() - 1));
        }
    }

    private void InitializeRegistry()
    {
        foreach (var viewPrefab in _sceneViews)
        {
            if (!viewPrefab) continue;

            // Instantiate if it's a prefab asset, otherwise use existing scene object
            var viewInstance = viewPrefab.gameObject.scene.rootCount == 0
                ? Instantiate(viewPrefab, _canvas.transform)
                : viewPrefab;

            viewInstance.gameObject.SetActive(false);
            _registry[viewInstance.GetType()] = viewInstance;
        }
    }
}