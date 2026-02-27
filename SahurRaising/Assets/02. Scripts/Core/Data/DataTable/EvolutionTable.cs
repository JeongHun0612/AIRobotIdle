using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "EvolutionTable", menuName = "SahurRaising/Data/EvolutionTable")]
    public class EvolutionTable : TableBase<int, EvolutionRow>
    {
        protected override int GetKey(EvolutionRow value) => value.EvolutionLevel;
    }
}
