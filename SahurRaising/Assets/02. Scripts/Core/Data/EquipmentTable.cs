using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "EquipmentTable", menuName = "SahurRaising/Data/EquipmentTable")]
    public class EquipmentTable : TableBase<string, EquipmentRow>
    {
        protected override string GetKey(EquipmentRow value) => value.Code;
    }
}

