/* ==================================================================================
 * 🛠️ M.S.T. FORCE BOOTSTRAPPER
 * ==================================================================================
 * Author:        Muhammet Serhat Tatar (M.S.T.)
 * Description:   Forces the editor to always start from Build Index 0 (Bootstrap).
 * Prevents initialization errors when testing specific levels.
 * ==================================================================================
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Utilities.Editor
{
    [InitializeOnLoad]
    public static class ForceBootstrapper
    {
        private const string MENU_PATH = "Tools/M.S.T./Enable Auto-Bootstrap";
        private const string PREF_KEY = "MST_AutoBootstrap_Enabled";

        // Property to check or set the enabled state using EditorPrefs
        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PREF_KEY, true);
            set
            {
                EditorPrefs.SetBool(PREF_KEY, value);
                SetPlayModeStartScene(value);
            }
        }

        // Static constructor is called when the Editor loads or scripts recompile
        static ForceBootstrapper()
        {
            // Apply the setting logic after a short delay to ensure Editor is ready
            EditorApplication.delayCall += () => SetPlayModeStartScene(IsEnabled);
        }

        [MenuItem(MENU_PATH)]
        private static void ToggleAction()
        {
            IsEnabled = !IsEnabled;
        }

        // Validates and updates the checkmark in the Tools menu
        [MenuItem(MENU_PATH, true)]
        private static bool ValidateToggleAction()
        {
            Menu.SetChecked(MENU_PATH, IsEnabled);
            return true;
        }

        private static void SetPlayModeStartScene(bool enable)
        {
            if (!enable)
            {
                // If disabled, reset the start scene (Unity uses currently open scene)
                EditorSceneManager.playModeStartScene = null;
                Debug.Log($"<color=yellow>[ForceBootstrapper]</color> Disabled. Game will start from current scene.");
                return;
            }

            // Verify if there are scenes in Build Settings
            if (EditorBuildSettings.scenes.Length == 0) return;

            // Retrieve the scene at Index 0 (Bootstrap)
            string scenePath = EditorBuildSettings.scenes[0].path;
            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (bootstrapScene != null)
            {
                // Force Unity to start from the Bootstrap scene
                EditorSceneManager.playModeStartScene = bootstrapScene;
                Debug.Log($"<color=cyan>[ForceBootstrapper]</color> Active. Play Mode forced to: <b>{bootstrapScene.name}</b>");
            }
            else
            {
                Debug.LogError("[ForceBootstrapper] Scene at Build Index 0 not found! Please check File > Build Settings.");
            }
        }
    }
}
#endif