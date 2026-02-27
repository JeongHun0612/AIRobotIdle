using UnityEngine;

public enum AdKey
{
    Rewarded_Default = 1000,

    Interstitial_Default = 2000,

    Banner_Default = 3000,
}

public enum AdFormat
{
    Rewarded,
    Interstitial,
    Banner,
}

[System.Serializable]
public class AdUnitEntry
{
    public AdKey Key;
    public AdFormat Format;
    public string AndroidAdUnitId;
    public string IosAdUnitId;
}

[CreateAssetMenu(fileName = "AdUnitConfig", menuName = "SahurRaising/Config/AdUniConfig")]
public class AdUnitConfig : ScriptableObject
{
    public AdUnitEntry[] Entries;
}