using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "GachaEquipmentTable", menuName = "SahurRaising/Data/GachaEquipmentTable")]
    public class GachaEquipmentTable : TableBase<EquipmentGrade, GachaEquipmentRow>
    {
        protected override EquipmentGrade GetKey(GachaEquipmentRow value) => value.Grade;
    }
}
