using BreakInfinity;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    public class EquipmentGachaResultStrategy : IGachaResultStrategy
    {
        public GachaType Type => GachaType.Equipment;

        private const EquipmentGrade HIGH_GRADE_THRESHOLD = EquipmentGrade.A3;

        private IEquipmentService _equipmentService;

        public EquipmentGachaResultStrategy()
        {
            if (_equipmentService == null && ServiceLocator.HasService<IEquipmentService>())
            {
                _equipmentService = ServiceLocator.Get<IEquipmentService>();
            }
        }

        public CurrencyType GetCurrencyType()
        {
            return CurrencyType.Diamond;
        }

        public bool IsHighGrade(string gradeKey)
        {
            if (string.IsNullOrEmpty(gradeKey))
                return false;

            if (System.Enum.TryParse<EquipmentGrade>(gradeKey, true, out var currentGrade))
            {
                return currentGrade >= HIGH_GRADE_THRESHOLD;
            }

            return false;
        }

        public bool IsNewItem(string itemCode)
        {
            if (string.IsNullOrEmpty(itemCode) || _equipmentService == null)
                return false;

            var info = _equipmentService.GetInventoryInfo(itemCode);
            return !info.IsOwned;
        }

        public string FormatGradeText(string gradeKey)
        {
            if (string.IsNullOrEmpty(gradeKey))
                return string.Empty;

            // "D3" → "D" (문자 부분만 추출)
            var (gradeLetter, _) = StringUtils.ParseLettersAndNumber(gradeKey);
            return gradeLetter;
        }

        public int GetGemCount(string gradeKey)
        {
            if (string.IsNullOrEmpty(gradeKey))
                return 0;

            var (_, gemCount) = StringUtils.ParseLettersAndNumber(gradeKey);
            return gemCount;
        }
    }
}