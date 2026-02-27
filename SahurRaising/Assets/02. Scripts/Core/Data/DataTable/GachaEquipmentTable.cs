using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "GachaEquipmentTable", menuName = "SahurRaising/Data/GachaEquipmentTable")]
    public class GachaEquipmentTable : TableBase<int, GachaEquipmentRow>
    {
        protected override int GetKey(GachaEquipmentRow value) => value.Level;

        /// <summary>
        /// 테이블에 있는 최대 레벨을 반환합니다
        /// </summary>
        public int GetMaxLevel()
        {
            if (Index == null || Index.Count == 0)
                return 1;

            int maxLevel = 1;
            foreach (var key in Index.Keys)
            {
                if (key > maxLevel)
                    maxLevel = key;
            }

            return maxLevel;
        }

        /// <summary>
        /// 특정 레벨에 따른 장비 등급별 확률 리스트를 반환합니다. (UI 표시용)
        /// </summary>
        /// <param name="level">가챠 레벨 (1부터 시작)</param>
        /// <returns>등급별 확률 리스트 (확률이 0보다 큰 항목만)</returns>
        public List<GachaProbability> GetProbabilitiesForLevel(int level)
        {
            int maxLevel = GetMaxLevel();
            var clampedLevel = Mathf.Clamp(level, 1, maxLevel);

            if (Index.TryGetValue(clampedLevel, out var row))
            {
                if (row.Probabilities == null || row.Probabilities.Count == 0)
                    return new List<GachaProbability>();

                // EquipmentProbability를 GachaProbability로 변환
                var result = new List<GachaProbability>();
                foreach (var prob in row.Probabilities)
                {
                    if (prob.Probability > 0)
                    {
                        result.Add(new GachaProbability
                        {
                            GradeKey = prob.Grade.ToString(),
                            Probability = prob.Probability
                        });
                    }
                }
                return result;
            }

            return new List<GachaProbability>();
        }

        /// <summary>
        /// 확률에 따라 등급을 뽑습니다.
        /// </summary>
        /// <param name="level">가챠 레벨 (1부터 시작)</param>
        /// <returns>뽑힌 등급</returns>
        public EquipmentGrade DrawGrade(int level)
        {
            int maxLevel = GetMaxLevel();
            var clampedLevel = Mathf.Clamp(level, 1, maxLevel);

            if (!Index.TryGetValue(clampedLevel, out var row) || row.Probabilities == null || row.Probabilities.Count == 0)
            {
                Debug.LogWarning($"[GachaEquipmentTable] 레벨 {level}에 대한 확률 데이터가 없습니다.");
                return EquipmentGrade.F;
            }

            // 전체 확률 합계 계산
            float totalProb = 0f;
            foreach (var gradeProb in row.Probabilities)
            {
                if (gradeProb.Probability > 0)
                    totalProb += gradeProb.Probability;
            }

            if (totalProb <= 0)
            {
                Debug.LogWarning($"[GachaEquipmentTable] 레벨 {level}의 전체 확률이 0입니다.");
                return EquipmentGrade.F;
            }

            // 확률에 따라 등급 결정
            var random = UnityEngine.Random.Range(0f, totalProb);
            float accumulated = 0f;
            EquipmentGrade selectedGrade = EquipmentGrade.F;

            foreach (var prob in row.Probabilities)
            {
                if (prob.Probability > 0)
                {
                    accumulated += prob.Probability;
                    if (random <= accumulated)
                    {
                        selectedGrade = prob.GetGradeEnum();
                        break;
                    }
                }
            }

            return selectedGrade;
        }
    }
}
