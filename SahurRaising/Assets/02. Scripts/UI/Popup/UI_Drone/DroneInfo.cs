using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    public class DroneInfo : ItemInfoBase<DroneRow, IDroneService>
    {
        [Header("아이템 정보")]
        [SerializeField] private DroneInfoItemSlot _itemSlot;

        [Header("아이템 보유 스탯")]
        [SerializeField] private OptionStatPanel _heldOptionStatPanel;

        protected override void UpdateItemSlot(DroneRow data)
        {
            if (_itemSlot == null)
                return;

            _itemSlot.gameObject.SetActive(true);
            _itemSlot.SetData(data);
        }

        protected override void UpdateEquipOptionStat(DroneRow data, int level)
        {
            if (_equipOptionStatPanel == null)
                return;

            _equipOptionStatPanel.gameObject.SetActive(true);
            _equipOptionStatPanel.UpdateEquipmentStatText(data.EquipOption, level);
        }

        protected override void UpdateHeldOptionStats(DroneRow data, int level)
        {
            if (_heldOptionStatPanel == null)
                return;

            _heldOptionStatPanel.gameObject.SetActive(true);
            _heldOptionStatPanel.UpdateEquipmentStatText(data.HeldOption1, level);
        }

        public override void HideInfo()
        {
            base.HideInfo();

            if (_itemSlot != null)
                _itemSlot.gameObject.SetActive(false);

            if (_heldOptionStatPanel != null)
                _heldOptionStatPanel.gameObject.SetActive(false);
        }

        protected override bool GetIsEquipped()
        {
            string equippedID = _service.GetEquippedID();
            return !string.IsNullOrEmpty(equippedID) && equippedID == _currentData.ID;
        }

        protected override void Equip()
        {
            _service.Equip(_currentData.ID);
        }

        protected override void Unequip()
        {
            _service.Unequip();
        }

        protected override bool LevelUp()
        {
            return _service.LevelUp(_currentData.ID);
        }

        protected override string GetItemID()
        {
            return _currentData.ID;
        }

        protected override int GetItemLevel()
        {
            var inventoryInfo = _service.GetInventoryInfo(_currentData.ID);
            return inventoryInfo.Level;
        }
    }
}
