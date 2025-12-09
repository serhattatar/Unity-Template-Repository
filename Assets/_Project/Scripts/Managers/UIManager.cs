using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UI.Core;

/// <summary>
/// Scene-Based UI Manager.
/// Manages the UI Stack, Blocker, and View Registry for the ACTIVE scene only.
/// </summary>
[DefaultExecutionOrder(-50)]
public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas _canvas;

    [Tooltip("Black semi-transparent background to block clicks.")]
    [SerializeField] private Image _blocker;

    [Header("Configuration")]
    [Tooltip("List of all Views (Popups/Panels) used in THIS scene.")]
    [SerializeField] private List<UIView> _sceneViews = new List<UIView>();

    // Static instance specific to the loaded scene
    private static UIManager _instance;

    private Dictionary<Type, UIView> _viewRegistry = new Dictionary<Type, UIView>();
    private Stack<UIView> _history = new Stack<UIView>();

    private void Awake()
    {
        // Enforce Scene-Based Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        InitializeRegistry();

        if (_blocker != null)
            _blocker.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        // Hardware Back Button (Android / Escape) logic
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_history.Count > 0) Back();
        }
    }

    private void InitializeRegistry()
    {
        foreach (var view in _sceneViews)
        {
            if (view == null) continue;

            // Determine if it's a Prefab (Asset) or Scene Object
            if (view.gameObject.scene.rootCount == 0)
            {
                // It's a Prefab, instantiate it
                var instance = Instantiate(view, _canvas.transform);
                instance.gameObject.SetActive(false);
                _viewRegistry[instance.GetType()] = instance;
            }
            else
            {
                // It's already in the scene
                view.gameObject.SetActive(false);
                _viewRegistry[view.GetType()] = view;
            }
        }
    }

    // --- PUBLIC API ---

    public static async void Show<T>(object data = null) where T : UIView
    {
        if (_instance == null)
        {
            Debug.LogError("[UIManager] No UIManager found in this scene!");
            return;
        }
        await _instance.ShowInternal<T>(data);
    }

    public static async void Hide<T>() where T : UIView
    {
        if (_instance == null) return;
        await _instance.HideInternal(typeof(T));
    }

    public static async void Hide(Type type)
    {
        if (_instance == null) return;
        await _instance.HideInternal(type);
    }

    public static void Back()
    {
        if (_instance != null) _instance.HandleBack();
    }

    // --- INTERNAL LOGIC ---

    private async UniTask ShowInternal<T>(object data) where T : UIView
    {
        Type type = typeof(T);

        if (!_viewRegistry.TryGetValue(type, out var view))
        {
            Debug.LogError($"[UIManager] View '{type.Name}' is not registered in this scene's UIManager.");
            return;
        }

        if (view.IsActive)
        {
            view.transform.SetAsLastSibling();
            return;
        }

        // 1. Manage Stack & Blocker
        UpdateBlocker(true, view.transform);
        _history.Push(view);
        view.transform.SetAsLastSibling();

        // 2. Show View
        await view.Show(data);
    }

    private async UniTask HideInternal(Type type)
    {
        if (!_viewRegistry.TryGetValue(type, out var view)) return;
        if (!view.IsActive) return;

        // 1. Manage Stack
        if (_history.Count > 0 && _history.Peek() == view)
        {
            _history.Pop();
        }

        // 2. Hide View
        await view.Hide();

        // 3. Restore Blocker for the previous view
        if (_history.Count > 0)
        {
            UpdateBlocker(true, _history.Peek().transform);
        }
        else
        {
            UpdateBlocker(false, null);
        }
    }

    private void HandleBack()
    {
        if (_history.Count > 0)
        {
            var topView = _history.Peek();
            Hide(topView.GetType());
        }
    }

    private void UpdateBlocker(bool active, Transform targetView)
    {
        if (_blocker == null) return;

        if (active)
        {
            _blocker.gameObject.SetActive(true);
            // Place blocker right behind the target view
            int targetIndex = targetView.GetSiblingIndex();
            _blocker.transform.SetSiblingIndex(Mathf.Max(0, targetIndex - 1));
        }
        else
        {
            _blocker.gameObject.SetActive(false);
        }
    }
}