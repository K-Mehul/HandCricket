using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StadiumRegistry", menuName = "HandCricket/Stadium Registry")]
public class StadiumRegistry : ScriptableObject
{
    public List<StadiumData> stadiums;
}

[System.Serializable]
public class StadiumData
{
    public string name;
    public string displayName;
    public int stake;
    public int overs;
    public int wickets;
    public Sprite icon;
    public Color themeColor = Color.white;
    public Sprite cardBackground;
    public string description;
    public int minLevel = 1;
}
