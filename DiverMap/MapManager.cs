using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logging.Logger;

namespace DiverMap;

public class MapManager: MonoBehaviour
{
    private const float BaseOrthoSize = 24;
    
    private readonly ManualLogSource _logger = Logger.CreateLogSource($"  {MyPluginInfo.PLUGIN_NAME}][{nameof(MapManager)}");
    
    private Configuration _config = null!;
    
    private Camera _miniMapCamera = null!;

    private RectTransform _miniMap = null!;
    
    private GameObject _miniMapGameObject = null!;
    
    private float _keyDownTime;
    
    private float _currentZoom;
    
    internal void Init(HUDRoot hudRoot, Configuration config)
    {
        _logger.LogDebug("Initializing MiniMap");
        _config = config;
        _currentZoom = config.MiniMapZoom;
        if (_miniMapCamera != null || _miniMap != null)
        {
            _logger.LogWarning("MiniMap already initialized!");
            return;
        }
        
        if (!SetupMiniMapCamera())
        {
            _logger.LogWarning("Failed to setup MiniMap camera!");
            enabled = false;
            return;
        }
        
        CreateMinimap(hudRoot);
        _miniMap!.gameObject.SetActive(config.ShowMiniMap);
        _logger.LogInfo("MiniMap initialized");
    }

    private bool SetupMiniMapCamera()
    {
        _logger.LogDebug("Setting up MiniMap camera");
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            _logger.LogWarning("Main camera not found!");
            return false;
        }
        
        var cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager == null)
        {
            _logger.LogWarning("CameraManager not found!");
            return false;
        }

        if (cameraManager.mainCamera != mainCam)
        {
            _logger.LogWarning("CameraManager.mainCamera != Camera.main");
            mainCam = cameraManager.mainCamera;
        }

        var camGo = new GameObject("MiniMapCamera");
        camGo.transform.SetParent(cameraManager.transform);
        
        var newCam = camGo.AddComponent<Camera>();
        newCam.CopyFrom(mainCam);
        newCam.tag = "Untagged";
        
        var w = (int)Mathf.Round(_config.MiniMapWidth * Screen.width);
        var h = (int)Mathf.Round(_config.MiniMapHeight * Screen.height);
        var renderTexture = new RenderTexture(w, h, 24) {
            useMipMap = false,
            anisoLevel = 1,
            useDynamicScale = false
        };
        newCam.aspect = w / (float) h;
        newCam.targetTexture = renderTexture;
        newCam.backgroundColor = new Color(0, 0, 0, 0.3f);
        newCam.orthographic = true;
        newCam.orthographicSize = BaseOrthoSize / _currentZoom;

        // var mask = newCam.cullingMask;
        // mask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
        // mask &= ~(1 << LayerMask.NameToLayer("Liquid"));
        // newCam.cullingMask = mask;
        // newCam.clearFlags = CameraClearFlags.SolidColor;
        
        _miniMapCamera = newCam;
        return true;
    }

    private void CreateMinimap(HUDRoot hudRoot)
    {
        _logger.LogDebug("Creating MiniMap");
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
        
        _miniMap = rectTransform;
        _miniMapGameObject = miniMap;
    }

    private void OnDestroy()
    {
        if(_miniMapCamera == null) return;
        
        _config.MiniMapZoom = _currentZoom;
        
        var texture = _miniMapCamera.targetTexture;
        _miniMapCamera.targetTexture = null;
        texture.Release();
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.JoystickButton6)) && Time.time - _keyDownTime > 0.1f)
        {
            _config.ShowMiniMap = !_config.ShowMiniMap;
            _miniMapGameObject.active = _config.ShowMiniMap;
            _keyDownTime = Time.time;
        }
    }
}