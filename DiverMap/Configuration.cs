using BepInEx.Configuration;
using UnityEngine;

namespace DiverMap;

public class Configuration
{
    private readonly ConfigEntry<bool> _showMiniMap;

    public bool ShowMiniMap
    {
        get => _showMiniMap.Value; 
        set => _showMiniMap.Value = value;
    }
    
    private readonly ConfigEntry<float> _miniMapWidth;
    
    public float MiniMapWidth
    {
        get => _miniMapWidth.Value;
        set => _miniMapWidth.Value = value;
    }
    
    private readonly ConfigEntry<float> _miniMapHeight;
    
    public float MiniMapHeight
    {
        get => _miniMapHeight.Value;
        set => _miniMapHeight.Value = value;
    }
    
    private readonly ConfigEntry<float> _miniMapZoom;
    
    public float MiniMapZoom
    {
        get => _miniMapZoom.Value;
        set => _miniMapZoom.Value = value;
    }
    
    private Configuration(ConfigFile configFile)
    {
        _showMiniMap = configFile.Bind("General", "ShowMiniMap", true, "Show the minimap");
        _miniMapWidth = configFile.Bind("General", "MiniMapWidth", 0.15f, "Width of the minimap with respect to the screen width");
        _miniMapHeight = configFile.Bind("General", "MiniMapHeight", 0.3f, "Height of the minimap with respect to the screen height");
        _miniMapZoom = configFile.Bind("General", "MiniMapZoom", 1f, "Zoom level of the minimap");
        ClampValues();
    }
    
    public void ClampValues()
    {
        MiniMapWidth = Mathf.Clamp(MiniMapWidth, 0.05f, 0.5f);
        MiniMapHeight = Mathf.Clamp(MiniMapHeight, 0.05f, 0.5f);
        MiniMapZoom = Mathf.Clamp(MiniMapZoom, 0.25f, 4f);
    }
    
    private static Configuration? _instance;

    public static Configuration Create(ConfigFile configFile)
    {
        return _instance ??= new Configuration(configFile);
    }

}