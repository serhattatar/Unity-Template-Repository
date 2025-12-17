/* ==================================================================================
 * 🖥️ UI MANAGER (V3.2 - RETURN TYPE SUPPORT)
 * ==================================================================================
 * Author:         Muhammet Serhat Tatar (M.S.T.)
 * Description:    Show<T> now returns the instance of T immediately.
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
    [SerializeField] private Image _blocker;

    [Header("Configuration")]
    [Tooltip("Assign all UI Prefabs (Popups & Panels) here.")]
    [SerializeField] private List<UIView> _sceneViews = new List<UIView>();

    private static UIManager _instance;
    private Dictionary<Type, UIView> _registry = new Dictionary<Type, UIView>();
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
        if (Input.GetKeyDown(KeyCode.Escape) && _popupHistory.Count > 0)
        {
            Back();
        }
    }

    // --- STATIC API (UPDATED) ---

    // [NEW] Returns the view instance immediately so we can assign it to a variable
    public static T Show<T>(object data = null) where T : UIView
    {
        if (_instance == null) return null;

        // Trigger async animation but don't await it here (Fire and Forget)
        _instance.ShowInternal(typeof(T), data).Forget();

        // Return the reference from registry
        if (_instance._registry.TryGetValue(typeof(T), out var view))
        {
            return view as T;
        }

        GameLogger.Error($"[UIManager] Could not find view of type {typeof(T).Name}");
        return null;
    }

    public static async void Hide<T>() where T : UIView => await _instance.HideInternal(typeof(T));
    public static void Hide(Type type) => _instance?.HideInternal(type).Forget(); // Fixed missing overload

    public static void Refresh<T>(object data) where T : UIView => _instance?.RefreshInternal(typeof(T), data);
    public static void Back() => _instance?.HandleBack();

    public static T GetView<T>() where T : UIView
    {
        if (_instance != null && _instance._registry.TryGetValue(typeof(T), out var view))
            return view as T;
        return null;
    }

    // --- INTERNAL LOGIC ---

    private async UniTask ShowInternal(Type type, object data)
    {
        if (!_registry.TryGetValue(type, out var view))
        {
            GameLogger.Error($"[UIManager] View not found: {type.Name}");
            return;
        }

        view.transform.SetAsLastSibling();

        if (view is UIPopup popup)
        {
            _popupHistory.Push(popup);
            UpdateBlocker(popup);
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
    }

    private void InitializeRegistry()
    {
        foreach (var viewPrefab in _sceneViews)
        {
            if (!viewPrefab) continue;
            var viewInstance = viewPrefab.gameObject.scene.rootCount == 0
                ? Instantiate(viewPrefab, _canvas.transform)
                : viewPrefab;

            viewInstance.gameObject.SetActive(false);
            _registry[viewInstance.GetType()] = viewInstance;
        }
    }
}