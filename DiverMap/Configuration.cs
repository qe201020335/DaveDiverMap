using BepInEx.Configuration;

namespace DiverMap;

public class Configuration
{
    private readonly ConfigEntry<bool> _showMiniMap;

    public bool ShowMiniMap
    {
        get => _showMiniMap.Value; 
        set => _showMiniMap.Value = value;
    }
    
    private Configuration(ConfigFile configFile)
    {
        _showMiniMap = configFile.Bind("General", "ShowMiniMap", true, "Show the minimap");
    }
    
    private static Configuration? _instance;

    public static Configuration Create(ConfigFile configFile)
    {
        return _instance ??= new Configuration(configFile);
    }

}