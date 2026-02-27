using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "StatsTable", menuName = "SahurRaising/Data/StatsTable")]
    public class StatsTable : TableBase<int, StatsRow>
    {
        protected override int GetKey(StatsRow value) => value.Level;
    }
}

