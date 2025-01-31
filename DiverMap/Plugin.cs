using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DiverMap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static Plugin Instance { get; private set; } = null!;
    
    internal static ManualLogSource Logger { get; private set; } = null!;
    
    private Configuration _config = null!;

    private MapManager? _mapManager;

    public override void Load()
    {
        // Plugin startup logic
        Instance = this;
        Logger = Log;
        _config = Configuration.Create(Config);

        var assembly = Assembly.GetExecutingAssembly();
        
        // Register IL2CPP classes
        Logger.LogInfo("Registering IL2CPP classes");
        foreach (var type in assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(Il2CppObjectBase))))
        {
            Logger.LogDebug("Registering type: " + type.FullName);
            ClassInjector.RegisterTypeInIl2Cpp(type);
        }

        UnityAction<Scene, Scene> a = new Action<Scene, Scene>(OnActiveSceneChanged);
        SceneManager.add_activeSceneChanged(a);

        Harmony.CreateAndPatchAll(assembly, MyPluginInfo.PLUGIN_GUID);
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public override bool Unload()
    {
        _config.ClampValues();
        return base.Unload();
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        Logger.LogInfo($"Active scene changed! ({current.name ?? "Null"} -> {next.name ?? "Null"})");
        var context = SceneContext.Instance;
        
        if (context == null)
        {
            Logger.LogDebug("SceneContext is null!");
            return;
        }

        var isGame = context.IsCurrentSceneIngameType;
        Logger.LogDebug($"IsCurrentSceneIngameType: {isGame}");
        Logger.LogDebug($"SceneType: {context.SceneType}");
        
        if (!isGame) return;
        
        foreach (var cam in Camera.allCameras)
        {
            Logger.LogDebug($"Camera: {cam.name} (main? {cam == Camera.main})");
        }
        
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Logger.LogDebug("Main camera not found!");
            return;
        }
        
        Logger.LogDebug("Main camera culling mask:");
        foreach (var layer in Enumerable.Range(0, 31))
        {
            if ((mainCam.cullingMask & 1 << layer) == 1 << layer)
            {
                var name = LayerMask.LayerToName(layer);
                if (!string.IsNullOrEmpty(name))
                {
                    Logger.LogDebug($"Layer {layer}: {name}");
                }
            }
        }
    }

    public void CreateMinimap(HUDRoot hudRoot)
    {
        if (_mapManager != null)
        {
            Logger.LogWarning("MapManager already exists!");
            return;
        }
        
        _mapManager = AddComponent<MapManager>();
        _mapManager.Init(hudRoot, _config);
    }
}

[HarmonyPatch(typeof(HUDRoot), "Awake")]
public static class HudRootPatch
{
    [HarmonyPostfix]
    private static void Postfix(HUDRoot __instance)
    {
        Plugin.Logger.LogInfo("HUDRoot Awake!");
        
        var context = SceneContext.Instance;
        if (context == null)
        {
            Plugin.Logger.LogWarning("SceneContext is null!");
            return;
        }

        var isGame = context.IsCurrentSceneIngameType;
        
        if (!isGame)
        {
            Plugin.Logger.LogDebug("Not in game scene!");
            return;
        }

        if (context.SceneType is SceneType.village or SceneType.village_inside)
        {
            Plugin.Logger.LogDebug("Not creating minimap in village scene!");
            return;
        }
        
        Plugin.Instance.CreateMinimap(__instance);
    }
}
