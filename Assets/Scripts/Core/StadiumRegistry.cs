using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StadiumRegistry", menuName = "HandCricket/Stadium Registry")]
public class StadiumRegistry : ScriptableObject
{
    [field:SerializeField] public List<StadiumData> stadiums;
}

[System.Serializable]
public class StadiumData
{
    [field:SerializeField] public string name;
    [field:SerializeField] public string displayName;
    [field:SerializeField] public int stake;
    [field:SerializeField] public int overs;
    [field:SerializeField] public int wickets;
    [field:SerializeField] public Sprite icon;
    [field:SerializeField] public Color themeColor = Color.white;
    [field:SerializeField] public Sprite cardBackground;
    [field:SerializeField] public string description;
    [field: SerializeField] public int minLevel = 1;
}
