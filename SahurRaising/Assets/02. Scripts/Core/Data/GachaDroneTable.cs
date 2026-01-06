using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "GachaDroneTable", menuName = "SahurRaising/Data/GachaDroneTable")]
    public class GachaDroneTable : TableBase<int, GachaDroneRow>
    {
        protected override int GetKey(GachaDroneRow value) => value.Level;

        /// <summary>
        /// 특정 레벨에 따른 드론 ID별 확률 리스트를 반환합니다. (UI 표시용)
        /// </summary>
        /// <param name="level">가챠 레벨 (1부터 시작)</param>
        /// <returns>드론 ID별 확률 리스트</returns>
        public List<DroneProbability> GetProbabilitiesForLevel(int level)
        {
            if (Index.TryGetValue(level, out var row))
            {
                return row.Probabilities ?? new List<DroneProbability>();
            }

            return new List<DroneProbability>();
        }

        /// <summary>
        /// 확률에 따라 드론 ID를 뽑습니다.
        /// </summary>
        /// <param name="level">가챠 레벨 (1부터 시작)</param>
        /// <returns>뽑힌 드론 ID</returns>
        public string DrawDroneID(int level)
        {
            if (!Index.TryGetValue(level, out var row) || row.Probabilities == null || row.Probabilities.Count == 0)
            {
                Debug.LogWarning($"[GachaDroneTable] 레벨 {level}에 대한 확률 데이터가 없습니다.");
                return "";
            }

            // 전체 확률 합계 계산
            float totalProb = 0f;
            foreach (var droneProb in row.Probabilities)
            {
                if (droneProb.Probability > 0)
                    totalProb += droneProb.Probability;
            }

            if (totalProb <= 0)
            {
                Debug.LogWarning($"[GachaDroneTable] 레벨 {level}의 전체 확률이 0입니다.");
                return "";
            }

            // 확률에 따라 드론 ID 결정
            var random = UnityEngine.Random.Range(0f, totalProb);
            float accumulated = 0f;
            string selectedDroneID = "";

            foreach (var droneProb in row.Probabilities)
            {
                if (droneProb.Probability > 0)
                {
                    accumulated += droneProb.Probability;
                    if (random <= accumulated)
                    {
                        selectedDroneID = droneProb.ID;
                        break;
                    }
                }
            }

            return selectedDroneID;
        }
    }
}
