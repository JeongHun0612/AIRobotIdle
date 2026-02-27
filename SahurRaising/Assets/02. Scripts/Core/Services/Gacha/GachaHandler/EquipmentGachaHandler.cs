using Cysharp.Threading.Tasks;
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

        private readonly ICloudCodeService _cloudCodeService;
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

        public UniTask InitializeRemoteConfigAsync(IRemoteConfigService remoteConfigService)
        {
            throw new System.NotImplementedException();
        }

        public List<GachaResult> Pull(int level, int count)
        {
            var results = new List<GachaResult>();
            for (int i = 0; i < count; i++)
            {
                var selectedGrade = _gachaEquipmentTable.DrawGrade(level);
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

        public List<GachaProbability> GetProbabilitiesForLevel(int level)
        {
            return _gachaEquipmentTable.GetProbabilitiesForLevel(level);
        }

        public int GetMaxLevel()
        {
            return _gachaEquipmentTable.GetMaxLevel();
        }
    }
}
