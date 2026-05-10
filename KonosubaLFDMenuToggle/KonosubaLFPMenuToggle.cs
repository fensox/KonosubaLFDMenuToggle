using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KonosubaLFDMenuToggle
{
    // -------------------------------------------------------------------------
    // KonosubaLFD Menu Toggle
    // Toggles the bottom menu bar (CanvasGuide) on/off via a configurable hotkey.
    //
    // The game destroys BepInEx_Manager shortly after startup, so we spawn our
    // own hidden, protected GameObject to host the MonoBehaviour update loop.
    // -------------------------------------------------------------------------
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyCode> ToggleKey;

        private void Awake()
        {
            Log = base.Logger;

            ToggleKey = Config.Bind(
                "General",
                "ToggleKey",
                KeyCode.F10,
                "Hotkey to toggle the bottom menu bar on/off."
            );

            // The game destroys BepInEx_Manager after startup, killing Update().
            // Spawn our own hidden GameObject that survives this sweep.
            GameObject runner = new GameObject("__KonosubaMenuToggleRunner__");
            runner.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(runner);
            runner.AddComponent<MenuToggleRunner>();

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded. Press [{ToggleKey.Value}] to toggle the menu bar.");
        }
    }

    public class MenuToggleRunner : MonoBehaviour
    {
        private GameObject _canvasGuide;
        private bool _menuVisible = true;

        private void Update()
        {
            if (_canvasGuide == null)
                TryFindCanvasGuide();

            if (UnityEngine.Input.GetKeyDown(Plugin.ToggleKey.Value))
                ToggleMenuBar();
        }

        private void TryFindCanvasGuide()
        {
            // Strategy 1: standard find — works for active objects in loaded scenes
            _canvasGuide = GameObject.Find("CanvasGuide");
            if (_canvasGuide != null)
            {
                Plugin.Log.LogInfo($"CanvasGuide found in scene '{_canvasGuide.scene.name}'.");
                return;
            }

            // Strategy 2: search all loaded scenes including inactive root objects
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "CanvasGuide")
                    {
                        _canvasGuide = root;
                        Plugin.Log.LogInfo($"CanvasGuide found in scene '{scene.name}' (inactive search).");
                        return;
                    }
                }
            }
        }

        private void ToggleMenuBar()
        {
            if (_canvasGuide == null)
            {
                Plugin.Log.LogWarning("CanvasGuide not found yet — cannot toggle.");
                return;
            }

            _menuVisible = !_menuVisible;
            _canvasGuide.SetActive(_menuVisible);
            Plugin.Log.LogInfo($"Menu bar is now {(_menuVisible ? "visible" : "hidden")}.");
        }
    }

    internal static class PluginInfo
    {
        internal const string PLUGIN_GUID = "com.fensox.konosubalfd.menutoggle";
        internal const string PLUGIN_NAME = "KonosubaLFD Menu Toggle";
        internal const string PLUGIN_VERSION = "1.0.0";
    }
}