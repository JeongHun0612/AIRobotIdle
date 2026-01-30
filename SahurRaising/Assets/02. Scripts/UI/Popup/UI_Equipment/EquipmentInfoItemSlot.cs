using TMPro;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Equipment 정보 표시용 아이템 슬롯 (버튼 및 상태 표시 없음)
    /// </summary>
    public class EquipmentInfoItemSlot : EquipmentItemSlotBase
    {
        [SerializeField] protected TMP_Text _nameText;

        protected override void UpdateItemName(string itemName)
        {
            if (_nameText == null)
                return;

            _nameText.text = itemName;
        }
    }
}