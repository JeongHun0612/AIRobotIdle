using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "DroneTable", menuName = "SahurRaising/Data/DroneTable")]
    public class DroneTable : TableBase<string, DroneRow>
    {
        protected override string GetKey(DroneRow value) => value.ID;
    }
}
