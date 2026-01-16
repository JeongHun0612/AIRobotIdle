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
            var probabilities = _gachaDroneTable.GetProbabilitiesForLevel(level);

            var results = new List<GachaResult>();
            for (int i = 0; i < count; i++)
            {
                var selectedDroneID = SelectDroneID(probabilities);

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

        private string SelectDroneID(List<DroneProbability> probabilities)
        {
            if (probabilities == null || probabilities.Count == 0)
            {
                Debug.LogWarning("[DroneGachaHandler] 확률 리스트가 비어있습니다.");
                return "";
            }

            float totalProb = 0f;
            foreach (var droneProb in probabilities)
            {
                if (droneProb.Probability > 0)
                    totalProb += droneProb.Probability;
            }

            if (totalProb <= 0)
            {
                Debug.LogWarning("[DroneGachaHandler] 전체 확률이 0입니다.");
                return "";
            }

            var random = UnityEngine.Random.Range(0f, totalProb);
            float accumulated = 0f;

            foreach (var droneProb in probabilities)
            {
                if (droneProb.Probability > 0)
                {
                    accumulated += droneProb.Probability;
                    if (random <= accumulated)
                    {
                        return droneProb.ID;
                    }
                }
            }

            return "";
        }
    }
}
