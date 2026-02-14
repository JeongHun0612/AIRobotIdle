using SahurRaising.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    public class DroneGachaHandler : IGachaHandler
    {
        public GachaType Type => GachaType.Drone;

        private readonly GachaDroneTable _gachaDroneTable;
        private readonly DroneTable _droneTable;
        private readonly IDroneService _droneService;

        public DroneGachaHandler(GachaDroneTable gachaDroneTable, DroneTable droneTable, IDroneService droneService)
        {
            _gachaDroneTable = gachaDroneTable;
            _droneTable = droneTable;
            _droneService = droneService;
        }

        public List<GachaResult> Pull(int level, int count)
        {
            var results = new List<GachaResult>();
            for (int i = 0; i < count; i++)
            {
                var selectedDroneID = _gachaDroneTable.DrawDroneID(level);

                // 드론 정보 가져오기
                if (_droneService.TryGetByID(selectedDroneID, out var drone))
                {
                    results.Add(new GachaResult
                    {
                        Type = GachaType.Drone,
                        ItemCode = selectedDroneID,
                        GradeKey = selectedDroneID,
                        TypeKey = string.Empty,
                        Icon = drone.Icon
                    });
                }
            }

            return results;
        }

        public void AddToInventory(GachaResult result)
        {
            if (result.Type != GachaType.Drone)
                return;

            if (string.IsNullOrEmpty(result.ItemCode))
            {
                Debug.LogWarning("[DroneGachaHandler] 드론 ItemCode가 비어있습니다.");
                return;
            }

            // 드론 인벤토리에 추가
            _droneService?.AddToInventory(result.ItemCode, 1);
        }

        public List<GachaProbability> GetProbabilitiesForLevel(int level)
        {
            return _gachaDroneTable.GetProbabilitiesForLevel(level);
        }

        public int GetMaxLevel()
        {
            return _gachaDroneTable.GetMaxLevel();
        }
    }
}
