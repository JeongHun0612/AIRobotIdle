using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "MonsterTable", menuName = "SahurRaising/Data/MonsterTable")]
    public class MonsterTable : TableBase<int, MonsterRow>
    {
        protected override int GetKey(MonsterRow value) => value.MonsterLevel;
    }
}

