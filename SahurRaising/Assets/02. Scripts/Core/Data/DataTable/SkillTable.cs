using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "SkillTable", menuName = "SahurRaising/Data/SkillTable")]
    public class SkillTable : TableBase<string, SkillRow>
    {
        protected override string GetKey(SkillRow value) => value.ID;
    }
}
