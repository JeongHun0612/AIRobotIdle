using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "GachaDroneTable", menuName = "SahurRaising/Data/GachaDroneTable")]
    public class GachaDroneTable : TableBase<string, GachaDroneRow>
    {
        protected override string GetKey(GachaDroneRow value) => value.ID;
    }
}
