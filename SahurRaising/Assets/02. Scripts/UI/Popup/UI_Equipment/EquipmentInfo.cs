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
        [SerializeField] private OptionStatPanel _equipOptionStatPanel;

        [Header("아이템 보유 스탯")]
        [SerializeField] private List<OptionStatPanel> _heldOptionStatPanels;

        [Header("장착 버튼")]
        [SerializeField] private Button _equipToggleButton;
        [SerializeField] private GameObject _equipButtonPanel;
        [SerializeField] private GameObject _unEquipButtonPanel;

        [Header("레벨업 버튼")]
        [SerializeField] private Button _levelUpButton;

        private IEquipmentService _equipmentService;
        private EquipmentRow _currentData;
        private Action _onEquipChanged; // 갱신 콜백

        public void Initialize(Action onEquipChanged = null)
        {
            _onEquipChanged = onEquipChanged;
            _itemSlot.Initialize();
        }

        public void RefreshEquipmentInfo(EquipmentRow data)
        {
            _currentData = data;

            _itemSlot.gameObject.SetActive(true);
            _itemSlot.SetData(data);

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            // 인벤토리 정보에서 레벨 가져오기
            var inventoryInfo = _equipmentService.GetInventoryInfo(data.Code);
            int level = inventoryInfo.Level;

            // 장착 스탯 (EquipOption)
            if (_equipOptionStatPanel != null)
            {
                _equipOptionStatPanel.gameObject.SetActive(true);
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

            _equipToggleButton.gameObject.SetActive(true);
            _levelUpButton.gameObject.SetActive(true);

            // 장착 버튼 상태 업데이트
            UpdateEquipButtonState();
        }

        public void HideEquipmentInfo()
        {
            _itemSlot.gameObject.SetActive(false);

            _equipOptionStatPanel.gameObject.SetActive(false);

            foreach (var heldOptionStatPanel in _heldOptionStatPanels)
            {
                heldOptionStatPanel.gameObject.SetActive(false);
            }

            _equipToggleButton.gameObject.SetActive(false);
            _levelUpButton.gameObject.SetActive(false);
        }

        private void UpdateEquipButtonState()
        {
            if (_equipToggleButton == null || _currentData.Code == null)
                return;

            // 현재 장착 상태 확인
            bool isEquipped = GetIsEquipped();

            _equipButtonPanel.SetActive(!isEquipped);
            _unEquipButtonPanel.SetActive(isEquipped);
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

        public void OnClickLevelUp()
        {
            if (_equipmentService == null || _currentData.Code == null)
                return;

            // 레벨업 수행
            bool success = _equipmentService.LevelUp(_currentData.Code);

            if (success)
            {
                // UI 갱신
                RefreshEquipmentInfo(_currentData);

                // 인벤토리 갱신
                _onEquipChanged?.Invoke();
            }
        }
    }
}
