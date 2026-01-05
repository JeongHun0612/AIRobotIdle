using System;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class ItemSlot : MonoBehaviour
    {
        [Header("아이콘 및 기본 정보")]
        [SerializeField] protected Image _icon;
        [SerializeField] protected TMP_Text _rankText;
        [SerializeField] protected TMP_Text _levelText;

        [Header("버튼")]
        [SerializeField] protected Button _slotButton;

        [Header("상태 표시")]
        [SerializeField] protected GameObject _newText;
        [SerializeField] protected GameObject _equipIcon;
        [SerializeField] protected GameObject _close;
        [SerializeField] protected GameObject _focus;

        [Header("강화 진행도 UI")]
        [SerializeField] protected Slider _progressSlider;   // 슬라이더 바
        [SerializeField] protected TMP_Text _progressText;   // "보유/필요" 텍스트

        protected IEquipmentService _equipmentService;

        protected EquipmentRow _data;
        protected EquipmentInventoryInfo _info;
        protected bool _isNew;
        protected bool _isEquip;

        public EquipmentRow Data => _data;

        public void Initialize(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        public void RegisterClickHandler(Action<ItemSlot> callback)
        {
            _slotButton.onClick.RemoveAllListeners();
            _slotButton.onClick.AddListener(() => { callback?.Invoke(this); });
        }

        public virtual void SetData(EquipmentRow data)
        {
            _data = data;

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            // 인벤토리 정보, NEW 여부를 내부에서 조회
            _info = _equipmentService.GetInventoryInfo(_data.Code);
            _isNew = _equipmentService.IsNewEquipment(_data.Code);

            _icon.sprite = data.Icon;
            _icon.color = (data.Icon == null) ? Color.clear : Color.white;
            _rankText.text = data.Grade.ToString();
            _levelText.text = $"Lv. {_info.Level}";

            bool isOwned = _info.Level > 0 && _info.Count > 0;
            _slotButton.interactable = isOwned;

            // 장착 여부 확인하여 아이콘 활성화/비활성화
            _isEquip = false;
            if (isOwned)
            {
                string equippedCode = _equipmentService.GetEquippedCode(_data.Type);
                _isEquip = !string.IsNullOrEmpty(equippedCode) && equippedCode == _data.Code;
            }

            if (_equipIcon != null)
            {
                _equipIcon.SetActive(isOwned && _isEquip);
            }

            _close.SetActive(!isOwned);
            _newText.SetActive(_isNew);

            if (_isNew)
            {
                _equipmentService.MarkAsSeen(_data.Code);
            }

            if (_focus != null)
            {
                _focus.SetActive(false);
            }

            UpdateProgressUI();
        }

        private void UpdateProgressUI()
        {
            if (_equipmentService == null)
                return;

            int requiredCount = _equipmentService.GetRequiredCountForUpgrade();
            int ownedCount = _info.Count;

            if (_progressText != null)
            {
                _progressText.text = $"{ownedCount}/{requiredCount}";
            }

            if (_progressSlider != null)
            {
                // 슬라이더가 0~필요개수 기준으로 채워지도록 설정
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = requiredCount;
                _progressSlider.value = Mathf.Clamp(ownedCount, 0, requiredCount);
            }
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
                _newText.gameObject.SetActive(false);
        }
    }
}
