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
        // TODO: 드론 서비스가 생기면 주입

        public DroneGachaHandler(GachaDroneTable gachaDroneTable, DroneTable droneTable)
        {
            _gachaDroneTable = gachaDroneTable;
            _droneTable = droneTable;
        }

        public List<GachaResult> Pull(int level, int count)
        {
            var probabilities = _gachaDroneTable.GetProbabilitiesForLevel(level);

            var results = new List<GachaResult>();
            for (int i = 0; i < count; i++)
            {
                var selectedDroneID = SelectDroneID(probabilities);

                results.Add(new GachaResult
                {
                    Type = GachaType.Drone,
                    ItemCode = selectedDroneID,
                });
            }

            return results;
        }

        public void AddToInventory(GachaResult result)
        {
            if (result.Type != GachaType.Drone)
                return;

            // TODO: 드론 인벤토리에 추가하는 로직
            Debug.Log($"[DroneGachaHandler] 드론 {result.ItemCode} 획득 (인벤토리 추가 로직 필요)");
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
