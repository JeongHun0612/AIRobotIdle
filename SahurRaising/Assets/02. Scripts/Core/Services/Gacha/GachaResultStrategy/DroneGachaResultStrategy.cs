using System.Collections.Generic;
using UnityEngine;
using SahurRaising.Utils;

namespace SahurRaising.Core
{
    public class DroneGachaResultStrategy : IGachaResultStrategy
    {
        public GachaType Type => GachaType.Drone;

        private const int HIGH_GRADE_THRESHOLD = 6;

        private IDroneService _droneService;

        public DroneGachaResultStrategy()
        {
            if (_droneService == null && ServiceLocator.HasService<IDroneService>())
            {
                _droneService = ServiceLocator.Get<IDroneService>();
            }
        }

        public void UpdateGachaButtons(UI_GachaResult ui, List<GachaButton> buttons)
        {
            foreach (var button in buttons)
            {
                if (button != null && button.GachaType == GachaType.Drone)
                {
                    button.Refresh(GachaType.Drone);
                }
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

            if (int.TryParse(gradeKey, out int droneGrade))
            {
                if (droneGrade >= 1 && droneGrade <= 9)
                {
                    return droneGrade >= HIGH_GRADE_THRESHOLD;
                }
            }

            return false;
        }

        public bool IsNewItem(string itemCode)
        {
            if (string.IsNullOrEmpty(itemCode) || _droneService == null)
                return false;

            var info = _droneService.GetInventoryInfo(itemCode);
            return !info.IsOwned;
        }

        public string FormatGradeText(string gradeKey)
        {
            if (string.IsNullOrEmpty(gradeKey))
                return string.Empty;

            var (letters, gradeNumber) = StringUtils.ParseLettersAndNumber(gradeKey);

            // 숫자를 로마 숫자로 변환 (예: "5" → "V")
            string romanNumeral = NumberFormatUtil.ToRomanNumeral(gradeNumber);

            if (!string.IsNullOrEmpty(romanNumeral))
            {
                return romanNumeral + letters;
            }
            
            // 변환 실패 시 원본 반환
            return gradeKey;
        }

        public int GetGemCount(string gradeKey)
        {
            if (string.IsNullOrEmpty(gradeKey))
                return 0;

            return 0;
        }
    }
}