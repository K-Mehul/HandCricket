using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Nakama;
using Nakama.TinyJson;

public class StadiumService : MonoBehaviour
{
    public static StadiumService Instance { get; private set; }

    [SerializeField] private StadiumRegistry registry;
    
    // Runtime cached list of stadiums synced from backend
    private List<StadiumData> syncedStadiums = new List<StadiumData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Fallback for registry in builds
            if (registry == null)
            {
                registry = Resources.Load<StadiumRegistry>("StadiumRegistry");
                if (registry != null) Debug.Log("StadiumService: Loaded registry from Resources.");
                else Debug.LogWarning("StadiumService: Failed to load registry from Resources.");
            }
        }
        else
        {
            // If the new one has a registry but the instance doesn't, keep the registry!
            if (Instance.registry == null && this.registry != null)
            {
                Instance.registry = this.registry;
                Debug.Log("StadiumService: Transferred registry to existing Instance.");
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Fetches the master stadium configuration from the backend.
    /// This should be called after a successful login/connect.
    /// </summary>
    public async Task SyncStadiumsAsync(ISession session)
    {
        Debug.Log("Syncing Stadiums from Backend...");

        try
        {
            var response = await NakamaService.Client.RpcAsync(session, "get_stadiums");
            Debug.Log($"StadiumService: Raw Payload: {response.Payload}");
            
            // Parse the JSON dictionary of stadiums
            var data = response.Payload.FromJson<Dictionary<string, StadiumData>>();

            if (data != null)
            {
                syncedStadiums.Clear();
                Debug.Log($"StadiumService: Parsed {data.Count} stadiums from backend.");
                
                foreach (var entry in data)
                {
                    StadiumData stadium = entry.Value;
                    if (stadium == null) 
                    {
                        Debug.LogWarning($"StadiumService: Found null stadium data for key {entry.Key}");
                        continue;
                    }

                    stadium.name = entry.Key; 
                    
                    if (registry == null)
                    {
                        Debug.LogWarning("StadiumService: Registry is NULL! cannot map icons.");
                    }
                    else if (registry.stadiums == null)
                    {
                        Debug.LogWarning("StadiumService: Registry.stadiums is NULL!");
                    }
                    else
                    {
                        Debug.Log($"StadiumService: Attempting to match key: '{entry.Key}' among {registry.stadiums.Count} registry items.");
                        foreach(var s in registry.stadiums) if(s != null) Debug.Log($"  - Registry Item: Name='{s.name}', Display='{s.displayName}'");

                        var localMatch = registry.stadiums.Find(s => 
                            s != null && (
                            string.Equals(s.name.Trim(), entry.Key.Trim(), StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(s.displayName.Trim(), entry.Key.Trim(), StringComparison.OrdinalIgnoreCase)));

                        if (localMatch != null)
                        {
                            Debug.Log($"StadiumService: Match found for {entry.Key}. Applying icon: {(localMatch.icon != null ? localMatch.icon.name : "Fallback")}");
                            stadium.icon = localMatch.icon != null ? localMatch.icon : localMatch.cardBackground;
                            stadium.themeColor = localMatch.themeColor;
                            stadium.cardBackground = localMatch.cardBackground;
                            stadium.description = localMatch.description;
                            
                            if (string.IsNullOrEmpty(stadium.displayName))
                                stadium.displayName = localMatch.displayName;
                        }
                        else
                        {
                            Debug.LogWarning($"StadiumService: No match in registry for DB key: '{entry.Key}'");
                        }
                    }
                    
                    syncedStadiums.Add(stadium);
                }
            }
            else
            {
                Debug.LogError("StadiumService: Failed to parse stadium data JSON (result was null).");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to sync stadiums: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            // Fallback to registry if sync fails
            if (registry != null && registry.stadiums != null) 
                syncedStadiums = new List<StadiumData>(registry.stadiums);
        }
    }

    public List<StadiumData> GetStadiums()
    {
        return syncedStadiums.Count > 0 ? syncedStadiums : (registry != null ? registry.stadiums : new List<StadiumData>());
    }

    public StadiumData GetStadium(string nameOrDisplayName)
    {
        return GetStadiums().Find(s => 
            string.Equals(s.name, nameOrDisplayName, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(s.displayName, nameOrDisplayName, StringComparison.OrdinalIgnoreCase));
    }
}
