using SahurRaising.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    public class EquipmentInfo : ItemInfoBase<EquipmentRow, IEquipmentService>
    {
        [Header("아이템 정보")]
        [SerializeField] private EquipmentInfoItemSlot _itemSlot;

        [Header("아이템 보유 스탯")]
        [SerializeField] private List<OptionStatPanel> _heldOptionStatPanels;

        protected override void UpdateItemSlot(EquipmentRow data)
        {
            if (_itemSlot == null)
                return;

            _itemSlot.gameObject.SetActive(true);
            _itemSlot.SetData(data);
        }

        protected override void UpdateEquipOptionStat(EquipmentRow data, int level)
        {
            if (_equipOptionStatPanel == null)
                return;

            _equipOptionStatPanel.gameObject.SetActive(true);
            _equipOptionStatPanel.UpdateEquipmentStatText(data.EquipOption, level);
        }

        protected override void UpdateHeldOptionStats(EquipmentRow data, int level)
        {
            if (_heldOptionStatPanels == null || _heldOptionStatPanels.Count == 0)
                return;

            var heldOptions = new List<OptionValue>
            {
                data.HeldOption1,
                data.HeldOption2,
                data.HeldOption3
            };

            // 모든 보유 옵션 패널을 먼저 비활성화
            for (int i = 0; i < _heldOptionStatPanels.Count; i++)
            {
                if (_heldOptionStatPanels[i] != null)
                {
                    _heldOptionStatPanels[i].gameObject.SetActive(false);
                }
            }

            // 보유 옵션이 있는 것만 활성화하고 업데이트
            int activePanelIndex = 0;
            for (int i = 0; i < heldOptions.Count; i++)
            {
                // Type이 비어있지 않으면 유효한 옵션으로 간주
                if (!string.IsNullOrEmpty(heldOptions[i].Type))
                {
                    if (activePanelIndex < _heldOptionStatPanels.Count && _heldOptionStatPanels[activePanelIndex] != null)
                    {
                        _heldOptionStatPanels[activePanelIndex].gameObject.SetActive(true);
                        _heldOptionStatPanels[activePanelIndex].UpdateEquipmentStatText(heldOptions[i], level);
                        activePanelIndex++;
                    }
                }
            }
        }

        public override void HideInfo()
        {
            base.HideInfo();

            if (_itemSlot != null)
                _itemSlot.gameObject.SetActive(false);

            foreach (var heldOptionStatPanel in _heldOptionStatPanels)
            {
                if (heldOptionStatPanel != null)
                    heldOptionStatPanel.gameObject.SetActive(false);
            }
        }

        protected override bool GetIsEquipped()
        {
            string equippedCode = _service.GetEquippedCode(_currentData.Type);
            return !string.IsNullOrEmpty(equippedCode) && equippedCode == _currentData.Code;
        }

        protected override void Equip()
        {
            _service.Equip(_currentData.Type, _currentData.Code);
        }

        protected override void Unequip()
        {
            _service.Unequip(_currentData.Type);
        }

        protected override bool LevelUp()
        {
            return _service.LevelUp(_currentData.Code);
        }

        protected override string GetItemID()
        {
            return _currentData.Code;
        }

        protected override int GetItemLevel()
        {
            var inventoryInfo = _service.GetInventoryInfo(_currentData.Code);
            return inventoryInfo.Level;
        }
    }
}
