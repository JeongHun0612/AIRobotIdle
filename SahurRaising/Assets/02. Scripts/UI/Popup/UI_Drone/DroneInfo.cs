using SahurRaising.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class DroneInfo : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private DroneInfoItemSlot _itemSlot;

        [Header("아이템 장착 스탯")]
        [SerializeField] private OptionStatPanel _equipOptionStatPanel;

        [Header("아이템 보유 스탯")]
        [SerializeField] private OptionStatPanel _heldOptionStatPanel;

        [Header("장착 버튼")]
        [SerializeField] private Button _equipButton;
        [SerializeField] private TMP_Text _equipButtonText;

        [SerializeField] private Sprite _equipButtonSprite;
        [SerializeField] private Sprite _unEquipButtonSprite;

        [Header("레벨업 버튼")]
        [SerializeField] private Button _levelUpButton;

        private IDroneService _droneService;
        private DroneRow _currentData;
        private Action _onEquipChanged; // 갱신 콜백

        public void Initialize(Action onEquipChanged = null)
        {
            _onEquipChanged = onEquipChanged;
            _itemSlot.Initialize();
        }

        public void RefreshDroneInfo(DroneRow data)
        {
            _currentData = data;

            _itemSlot.gameObject.SetActive(true);
            _itemSlot.SetData(data);

            if (_droneService == null)
                _droneService = ServiceLocator.Get<IDroneService>();

            // 인벤토리 정보에서 레벨 가져오기
            var inventoryInfo = _droneService.GetInventoryInfo(data.ID);
            int level = inventoryInfo.Level;

            // 장착 스탯 (EquipOption)
            if (_equipOptionStatPanel != null)
            {
                _equipOptionStatPanel.gameObject.SetActive(true);
                _equipOptionStatPanel.UpdateEquipmentStatText(data.EquipOption, level);
            }

            // 보유 스탯 (HeldOption)
            if (_heldOptionStatPanel != null)
            {
                _heldOptionStatPanel.gameObject.SetActive(true);
                _heldOptionStatPanel.UpdateEquipmentStatText(data.HeldOption1, level);
            }

            _equipButton.gameObject.SetActive(true);
            _levelUpButton.gameObject.SetActive(true);

            // 장착 버튼 상태 업데이트
            UpdateEquipButtonState();
        }

        public void HideEquipmentInfo()
        {
            _itemSlot.gameObject.SetActive(false);

            _equipOptionStatPanel.gameObject.SetActive(false);
            _heldOptionStatPanel.gameObject.SetActive(false);

            _equipButton.gameObject.SetActive(false);
            _levelUpButton.gameObject.SetActive(false);
        }

        private void UpdateEquipButtonState()
        {
            if (_equipButton == null || _currentData.ID == null)
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
            if (_droneService == null)
                _droneService = ServiceLocator.Get<IDroneService>();

            string equippedID = _droneService.GetEquippedID();
            return !string.IsNullOrEmpty(equippedID) && equippedID == _currentData.ID;
        }

        public void OnClickEquip()
        {
            if (_droneService == null || _currentData.ID == null)
                return;

            // 현재 장착 상태 확인
            bool isEquipped = GetIsEquipped();

            // 장착/해제 처리
            if (isEquipped)
            {
                _droneService.Unequip();
            }
            else
            {
                _droneService.Equip(_currentData.ID);
            }

            // 버튼 상태 업데이트
            UpdateEquipButtonState();

            // UI_Equipment 갱신 요청
            _onEquipChanged?.Invoke();
        }

        public void OnClickLevelUp()
        {
            if (_droneService == null || _currentData.ID == null)
                return;

            // 레벨업 수행
            bool success = _droneService.LevelUp(_currentData.ID);

            if (success)
            {
                // UI 갱신
                RefreshDroneInfo(_currentData);

                // 인벤토리 갱신
                _onEquipChanged?.Invoke();
            }
        }
    }
}
