using SahurRaising.Core;
using SahurRaising.Utils;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Drone 전용 아이템 슬롯 베이스 클래스
    /// </summary>
    public abstract class DroneItemSlotBase : ItemSlotBase<DroneRow, DroneInventoryInfo, IDroneService>
    {
        protected override string GetItemID()
        {
            return _data.ID;
        }

        protected override string GetItemName()
        {
            return string.Empty;
        }

        protected override Sprite GetIcon()
        {
            return _data.Icon;
        }

        protected override string GetGradeString()
        {
            return _data.ID;
        }

        protected override GachaType GetGachaType()
        {
            return GachaType.Drone;
        }

        protected override string GetRankText(string gradeString)
        {
            var (_, gradeNumber) = StringUtils.ParseLettersAndNumber(gradeString);

            string romanNumeral = NumberFormatUtil.ToRomanNumeral(gradeNumber);

            if (!string.IsNullOrEmpty(romanNumeral))
            {
                return romanNumeral;
            }

            return gradeString;
        }

        protected override DroneInventoryInfo GetInventoryInfo()
        {
            return _service.GetInventoryInfo(_data.ID);
        }

        protected override int GetLevel(DroneInventoryInfo info)
        {
            return info.Level;
        }

        protected override int GetCount(DroneInventoryInfo info)
        {
            return info.Count;
        }

        protected override int GetRequiredCountForAdvance()
        {
            return _service.GetRequiredCountForAdvance();
        }
    }
}