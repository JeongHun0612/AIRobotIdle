using SahurRaising.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    public class EquipmentGachaHandler : IGachaHandler
    {
        public GachaType Type => GachaType.Equipment;

        private readonly GachaEquipmentTable _gachaEquipmentTable;
        private readonly EquipmentTable _equipmentTable;
        private readonly GachaLevelConfig _levelConfig;
        private readonly IEquipmentService _equipmentService;

        public EquipmentGachaHandler(
            GachaEquipmentTable gachaEquipmentTable,
            EquipmentTable equipmentTable,
            GachaLevelConfig levelConfig,
            IEquipmentService equipmentService)
        {
            _gachaEquipmentTable = gachaEquipmentTable;
            _equipmentTable = equipmentTable;
            _levelConfig = levelConfig;
            _equipmentService = equipmentService;
        }

        public List<GachaResult> Pull(int level, int count)
        {
            var maxLevel = _levelConfig.GetMaxLevel(GachaType.Equipment);
            var probabilities = _gachaEquipmentTable.GetProbabilitiesForLevel(level, maxLevel);

            var results = new List<GachaResult>();
            for (int i = 0; i < count; i++)
            {
                var selectedGrade = SelectGrade(probabilities);
                var equipmentList = _equipmentTable?.GetByGrade(selectedGrade);

                if (equipmentList == null || equipmentList.Count == 0)
                {
                    Debug.LogError($"[EquipmentGachaHandler] 등급 {selectedGrade}에 해당하는 장비가 없습니다.");
                    results.Add(new GachaResult
                    {
                        Type = GachaType.Equipment,
                        ItemCode = string.Empty,
                        GradeKey = string.Empty,
                        TypeKey = string.Empty,
                        Icon = null
                    });
                    continue;
                }

                var randomIndex = UnityEngine.Random.Range(0, equipmentList.Count);
                var selectedEquipment = equipmentList[randomIndex];

                results.Add(new GachaResult
                {
                    Type = GachaType.Equipment,
                    ItemCode = selectedEquipment.Code,
                    GradeKey = selectedEquipment.Grade.ToString(),
                    TypeKey = selectedEquipment.Type.ToString(),
                    Icon = selectedEquipment.Icon
                });
            }

            return results;
        }

        public void AddToInventory(GachaResult result)
        {
            if (result.Type != GachaType.Equipment)
                return;

            _equipmentService?.AddToInventory(result.ItemCode, 1);
        }

        private EquipmentGrade SelectGrade(List<GradeProbability> probabilities)
        {
            if (probabilities == null || probabilities.Count == 0)
            {
                Debug.LogWarning("[EquipmentGachaHandler] 확률 리스트가 비어있습니다.");
                return EquipmentGrade.F;
            }

            float totalProb = 0f;
            foreach (var gradeProb in probabilities)
            {
                if (gradeProb.Probability > 0)
                    totalProb += gradeProb.Probability;
            }

            if (totalProb <= 0)
            {
                Debug.LogWarning("[EquipmentGachaHandler] 전체 확률이 0입니다.");
                return EquipmentGrade.F;
            }

            var random = UnityEngine.Random.Range(0f, totalProb);
            float accumulated = 0f;

            foreach (var gradeProb in probabilities)
            {
                if (gradeProb.Probability > 0)
                {
                    accumulated += gradeProb.Probability;
                    if (random <= accumulated)
                    {
                        return gradeProb.Grade;
                    }
                }
            }

            return EquipmentGrade.F;
        }
    }
}
