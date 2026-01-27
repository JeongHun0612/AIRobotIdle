using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Equipment 전용 아이템 슬롯 베이스 클래스
    /// </summary>
    public abstract class EquipmentItemSlotBase : ItemSlotBase<EquipmentRow, EquipmentInventoryInfo, IEquipmentService>
    {
        [Header("잼 아이콘 (Equipment 전용)")]
        [SerializeField] protected GameObject[] _gemIcons;

        protected override string GetItemID()
        {
            return _data.Code;
        }

        protected override Sprite GetIcon()
        {
            return _data.Icon;
        }

        protected override string GetGradeString()
        {
            return _data.Grade.ToString();
        }

        protected override GachaType GetGachaType()
        {
            return GachaType.Equipment;
        }

        protected override string GetRankText(string gradeString)
        {
            var (gradeLetter, _) = StringUtils.ParseLettersAndNumber(gradeString);
            return gradeLetter;
        }

        protected override EquipmentInventoryInfo GetInventoryInfo()
        {
            return _service.GetInventoryInfo(_data.Code);
        }

        protected override int GetLevel(EquipmentInventoryInfo info)
        {
            return info.Level;
        }

        protected override int GetCount(EquipmentInventoryInfo info)
        {
            return info.Count;
        }

        protected override int GetRequiredCountForAdvance()
        {
            return _service.GetRequiredCountForAdvance();
        }

        protected override void UpdateGemIcons(string gradeString)
        {
            if (_gemIcons == null)
                return;

            var (_, gemCount) = StringUtils.ParseLettersAndNumber(gradeString);
            if (gemCount > 0)
                gemCount = 4 - gemCount;

            for (int i = 0; i < _gemIcons.Length; i++)
            {
                if (_gemIcons[i] != null)
                {
                    _gemIcons[i].SetActive(i < gemCount);
                }
            }
        }
    }
}