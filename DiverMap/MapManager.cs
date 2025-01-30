using UnityEngine;

namespace DiverMap;

public class MapManager: MonoBehaviour
{
    private RectTransform _miniMap = null!;
    
    private float _keyDownTime;
    
    private Configuration _config = null!;
    
    internal void Init(RectTransform miniMap, Configuration config)
    {
        _miniMap = miniMap;
        _config = config;
        _miniMap.gameObject.SetActive(config.ShowMiniMap);
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.JoystickButton6)) && Time.time - _keyDownTime > 0.1f)
        {
            _config.ShowMiniMap = !_config.ShowMiniMap;
            _miniMap.gameObject.active = _config.ShowMiniMap;
            _keyDownTime = Time.time;
        }
    }
}