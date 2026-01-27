using System;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    /// <summary>
    /// Equipment 아이템 슬롯 (버튼 및 상태 표시 포함)
    /// </summary>
    public class EquipmentItemSlot : EquipmentItemSlotBase
    {
        [Header("버튼")]
        [SerializeField] private Button _slotButton;

        [Header("상태 표시")]
        [SerializeField] private GameObject _newText;
        [SerializeField] private GameObject _equipIcon;
        [SerializeField] private GameObject _itemDisable;
        [SerializeField] private GameObject _focus;

        [Header("강화 진행도 UI")]
        [SerializeField] private GameObject _sliderDisable;

        private bool _isNew;

        public void RegisterClickHandler(Action<EquipmentItemSlot> callback)
        {
            _slotButton.onClick.RemoveAllListeners();
            _slotButton.onClick.AddListener(() => { callback?.Invoke(this); });
        }

        public void SetFocus(bool isSelected)
        {
            if (_focus != null)
            {
                _focus.SetActive(isSelected);
            }
        }

        public void HideNewIfActive()
        {
            if (!_isNew)
                return;

            _isNew = false;
            if (_newText != null)
            {
                _newText.gameObject.SetActive(false);
            }
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();

            if (!IsValidData())
                return;

            var info = GetInventoryInfo();

            // 아이템 보유 여부 확인
            bool isOwned = info.IsOwned;

            if (_slotButton != null)
            {
                _slotButton.interactable = isOwned;
            }

            if (_itemDisable != null)
            {
                _itemDisable.SetActive(!isOwned);
            }

            // 장착 여부 확인
            string equippedCode = _service.GetEquippedCode(_data.Type);
            bool isEquip = !string.IsNullOrEmpty(equippedCode) && equippedCode == _data.Code;

            if (_equipIcon != null)
            {
                _equipIcon.SetActive(isOwned && isEquip);
            }

            // 새로 획득한 아이템인지 확인
            _isNew = _service.IsNewEquipment(_data.Code);
            if (_newText != null)
            {
                _newText.SetActive(_isNew);
            }

            if (_isNew)
            {
                _service.MarkAsSeen(_data.Code);
            }

            // Focus 오브젝트 비활성화
            SetFocus(false);

            if (_sliderDisable != null)
            {
                _sliderDisable.SetActive(!isOwned);
            }
        }
    }
}
