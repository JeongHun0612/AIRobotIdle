using SahurRaising.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class EquipmentInfo : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private EquipmentInfoItemSlot _itemSlot;

        [Header("아이템 장착 스탯")]
        [SerializeField] private EquipmentStatPanel _equipOptionStatPanel;

        [Header("아이템 보유 스탯")]
        [SerializeField] private List<EquipmentStatPanel> _heldOptionStatPanels;

        [Header("장착 버튼")]
        [SerializeField] private Button _equipButton;
        [SerializeField] private TMP_Text _equipButtonText;

        [SerializeField] private Sprite _equipButtonSprite;
        [SerializeField] private Sprite _unEquipButtonSprite;

        private IEquipmentService _equipmentService;
        private EquipmentRow _currentData;
        private Action _onEquipChanged; // 갱신 콜백

        public void Initialize(IEquipmentService equipmentService, Action onEquipChanged = null)
        {
            _equipmentService = equipmentService;
            _onEquipChanged = onEquipChanged;
        }

        public void RefreshEquipmentInfo(EquipmentRow data)
        {
            _currentData = data;
            _itemSlot.SetData(data);

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            // 인벤토리 정보에서 레벨 가져오기
            var inventoryInfo = _equipmentService.GetInventoryInfo(data.Code);
            int level = inventoryInfo.Level;

            // 장착 스탯 (EquipOption)
            if (_equipOptionStatPanel != null)
            {
                _equipOptionStatPanel.UpdateEquipmentStatText(data.EquipOption, level);
            }

            // 보유 스탯 (HeldOption1, 2, 3)
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

            // 장착 버튼 상태 업데이트
            UpdateEquipButtonState();
        }

        private void UpdateEquipButtonState()
        {
            if (_equipButton == null || _currentData.Code == null)
                return;

            // 현재 장착 상태 확인
            bool isEquipped = GetIsEquipped();

            // 버튼 업데이트
            if (_equipButtonText != null)
            {
                _equipButton.image.sprite = isEquipped ? _unEquipButtonSprite : _equipButtonSprite;
                _equipButtonText.text = isEquipped ? "UnEquip" : "Equip";
            }
        }

        private bool GetIsEquipped()
        {
            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            string equippedCode = _equipmentService.GetEquippedCode(_currentData.Type);
            return !string.IsNullOrEmpty(equippedCode) && equippedCode == _currentData.Code;
        }

        public void OnClickEquip()
        {
            if (_equipmentService == null || _currentData.Code == null)
                return;

            // 현재 장착 상태 확인
            bool isEquipped = GetIsEquipped();

            // 장착/해제 처리
            if (isEquipped)
            {
                _equipmentService.Unequip(_currentData.Type);
            }
            else
            {
                _equipmentService.Equip(_currentData.Type, _currentData.Code);
            }

            // 버튼 상태 업데이트
            UpdateEquipButtonState();

            // UI_Equipment 갱신 요청
            _onEquipChanged?.Invoke();
        }
    }
}
