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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DiverMap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static Plugin Instance { get; private set; } = null!;
    
    internal static ManualLogSource Logger { get; private set; } = null!;
    
    private Configuration _config = null!;
    
    private Camera? _miniMapCamera;
    
    private GameObject? _miniMap;

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

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        Logger.LogInfo($"Active scene changed! ({current.name ?? "Null"} -> {next.name ?? "Null"})");
        var context = SceneContext.Instance;
        
        if (context == null)
        {
            Logger.LogWarning("SceneContext is null!");
            return;
        }

        var isGame = context.IsCurrentSceneIngameType;
        Logger.LogInfo($"IsCurrentSceneIngameType: {isGame}");
    }
    
    /**
     *
        Layer 0: Default
        Layer 1: TransparentFX
        Layer 2: Ignore Raycast
        Layer 4: Water
        Layer 5: UI
        Layer 6: EnemyPhysical
        Layer 7: PlayerPhysical
        Layer 8: Player
        Layer 9: UIEffect
        Layer 10: Background
        Layer 11: Fish
        Layer 12: InteractionTrigger
        Layer 13: Projectile
        Layer 14: SushiBar_3dBackground
        Layer 15: Cutscene_Trigger
        Layer 16: DavePhone
        Layer 17: SushuBar_Main
        Layer 18: Liquid
        Layer 19: WithBackground
        Layer 20: DaveBlock
        Layer 21: FishAttack
        Layer 22: TrapZone
        Layer 23: NPCPlayer
        Layer 24: NPCBackground
        Layer 25: Additive_Cutscene
        Layer 26: LightLayer_1
        Layer 27: DestroyObject
        Layer 28: LightLayer_2
        Layer 29: DetectMapBackground
        Layer 30: CommonPhysical
     */
    private void SetupMiniMapCamera()
    {
        foreach (var cam in Camera.allCameras)
        {
            Logger.LogInfo($"Camera: {cam.name} (main? {cam == Camera.main})");
        }
        
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Logger.LogWarning("Main camera not found!");
            return;
        }
        
        Logger.LogInfo("Main camera culling mask:");
        foreach (var layer in Enumerable.Range(0, 31))
        {
            if ((mainCam.cullingMask & 1 << layer) == 1 << layer)
            {
                var name = LayerMask.LayerToName(layer);
                if (!string.IsNullOrEmpty(name))
                {
                    Logger.LogInfo($"Layer {layer}: {name}");
                }
            }
        }
        
        var cameraManager = Object.FindObjectOfType<CameraManager>();
        if (cameraManager == null)
        {
            Logger.LogWarning("CameraManager not found!");
            return;
        }

        if (cameraManager.mainCamera != mainCam)
        {
            Logger.LogWarning("CameraManager.mainCamera != Camera.main");
            mainCam = cameraManager.mainCamera;
        }

        var camGo = new GameObject("MiniMapCamera");
        camGo.transform.SetParent(cameraManager.transform);
        
        var newCam = camGo.AddComponent<Camera>();
        newCam.CopyFrom(mainCam);
        newCam.tag = "Untagged";
        // newCam.transform.localPosition = new Vector3(0, 0, -25);
        
        var w = (int)Math.Round(0.2 * Screen.width);
        // var h = (int)Math.Round(0.5 * Screen.height);
        var h = w;
        var renderTexture = new RenderTexture(w, h, 24) {
            useMipMap = false,
            anisoLevel = 1,
            useDynamicScale = false
        };
        newCam.aspect = w / (float) h;
        newCam.targetTexture = renderTexture;
        newCam.backgroundColor = new Color(0, 0, 0, 0.3f);
        newCam.orthographic = true;
        newCam.orthographicSize = 50;

        // var mask = newCam.cullingMask;
        // mask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
        // mask &= ~(1 << LayerMask.NameToLayer("Liquid"));
        // newCam.cullingMask = mask;
        // newCam.clearFlags = CameraClearFlags.SolidColor;
        
        _miniMapCamera = newCam;
    }

    public void CreateMinimap(HUDRoot hudRoot)
    {
        if (_miniMap != null)
        {
            Logger.LogDebug("MiniMap already exists!");
            return;
        }
        
        if (_miniMapCamera == null)
        {
            SetupMiniMapCamera();
            
            if (_miniMapCamera == null)
            {
                Logger.LogWarning("MiniMap camera not found!");
                return;
            }
        }

        var texture = _miniMapCamera.targetTexture;
        
        var miniMap = new GameObject("MiniMap");
        miniMap.layer = LayerMask.NameToLayer("UI");
        var rectTransform = miniMap.AddComponent<RectTransform>();
        rectTransform.SetParent(hudRoot.transform);
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        var image = miniMap.AddComponent<RawImage>();
        image.texture = texture;
        
        _miniMap = miniMap;
        _miniMapCamera.gameObject.AddComponent<MapManager>().Init(rectTransform, _config);
    }
}

[HarmonyPatch(typeof(HUDRoot), "Awake")]
public static class HudRootPatch
{
    [HarmonyPostfix]
    private static void Postfix(HUDRoot __instance)
    {
        Plugin.Logger.LogInfo("HUDRoot Awake!");
        Plugin.Instance.CreateMinimap(__instance);
    }
}
