using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "UpgradeTable", menuName = "SahurRaising/Data/UpgradeTable")]
    public class UpgradeTable : TableBase<string, UpgradeRow>
    {
        protected override string GetKey(UpgradeRow value) => value.Code;
    }
}

