/* ==================================================================================
 * 🛠️ M.S.T. SCENE SWITCHER OVERLAY (V2)
 * ==================================================================================
 * Author:        Muhammet Serhat Tatar (M.S.T.)
 * Description:   A native Unity Toolbar Overlay for instant scene switching.
 * Features:      Play Mode Lock, Event-Driven Updates, Zero Overhead.
 * ==================================================================================
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

namespace Utilities.Editor
{
    [Overlay(typeof(SceneView), "Scene Switcher (M.S.T.)", true)]
    public class SceneSwitcherOverlay : ToolbarOverlay
    {
        SceneSwitcherOverlay() : base(SceneDropdown.ID) { }

        [EditorToolbarElement(ID, typeof(SceneView))]
        class SceneDropdown : EditorToolbarDropdown
        {
            public const string ID = "MST_SceneSwitcher";

            public SceneDropdown()
            {
                // Initial Setup
                tooltip = "M.S.T. Scene Switcher: Click to switch scenes.";
                icon = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;
                UpdateTitle();

                // Subscribe to Events (Event-Driven Performance)
                EditorSceneManager.sceneOpened += OnSceneOpened;
                EditorBuildSettings.sceneListChanged += OnSceneListChanged;

                clicked += ShowDropdown;
            }

            // Cleanup to prevent memory leaks
            ~SceneDropdown()
            {
                EditorSceneManager.sceneOpened -= OnSceneOpened;
                EditorBuildSettings.sceneListChanged -= OnSceneListChanged;
            }

            // Event Callbacks
            private void OnSceneOpened(Scene scene, OpenSceneMode mode) => UpdateTitle();
            private void OnSceneListChanged() => UpdateTitle();

            private void UpdateTitle()
            {
                Scene current = EditorSceneManager.GetActiveScene();
                text = string.IsNullOrEmpty(current.name) ? "Unsaved Scene" : current.name;
            }

            private void ShowDropdown()
            {
                var menu = new GenericMenu();

                // 🔒 Play Mode Lock
                if (EditorApplication.isPlaying)
                {
                    menu.AddDisabledItem(new GUIContent($"Current: {text}"));
                    menu.AddSeparator("");
                    menu.AddDisabledItem(new GUIContent("🚫 Locked in Play Mode"));
                    menu.ShowAsContext();
                    return;
                }

                var scenes = EditorBuildSettings.scenes;

                if (scenes.Length == 0)
                {
                    menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Open Build Settings..."), false, () =>
                        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor")));
                }
                else
                {
                    // List Scenes
                    foreach (var scene in scenes)
                    {
                        if (string.IsNullOrEmpty(scene.path)) continue;

                        string name = Path.GetFileNameWithoutExtension(scene.path);
                        bool isActive = EditorSceneManager.GetActiveScene().name == name;

                        // Add tick mark for active scene
                        menu.AddItem(new GUIContent(name), isActive, () => SwitchScene(scene.path));
                    }

                    // Extra Options
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Reload Current Scene"), false, () =>
                        SwitchScene(EditorSceneManager.GetActiveScene().path));
                }

                menu.ShowAsContext();
            }

            private void SwitchScene(string path)
            {
                if (EditorApplication.isPlaying) return; // Safety check

                if (string.IsNullOrEmpty(path)) return;

                // Prompt to save if there are unsaved changes
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
            }
        }
    }
}
#endif