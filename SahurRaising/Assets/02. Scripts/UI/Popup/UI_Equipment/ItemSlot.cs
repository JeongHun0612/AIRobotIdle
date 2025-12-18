using SahurRaising.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class ItemSlot : MonoBehaviour
    {
        [Header("아이콘 및 기본 정보")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _levelText;

        [Header("버튼")]
        [SerializeField] private Button _slotButton;
        [SerializeField] private Button _equipToggleButton;

        [Header("장착 토글 색상")]
        [SerializeField] private Color _equippedColor = Color.red;
        [SerializeField] private Color _unequippedColor = Color.gray;

        [Header("상태 표시")]
        [SerializeField] private GameObject _newText;
        [SerializeField] private GameObject _close;

        [Header("강화 진행도 UI")]
        [SerializeField] private Slider _progressSlider;   // 슬라이더 바
        [SerializeField] private TMP_Text _progressText;   // "보유/필요" 텍스트

        private IEquipmentService _equipmentService;

        private EquipmentRow _data;
        private EquipmentInventoryInfo _info;
        private bool _isNew;

        public EquipmentRow Data => _data;

        public void Initialize(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        public void RegisterEquipToggleCallback(Action<ItemSlot> callback)
        {
            if (_equipToggleButton == null)
            {
                Debug.LogError("[ItemSlot] EquipToggleButton is Not found!");
                return;
            }

            _equipToggleButton.onClick.RemoveAllListeners();
            _equipToggleButton.onClick.AddListener(() => callback?.Invoke(this));
        }

        public void SetData(EquipmentRow data)
        {
            _data = data;

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            // 인벤토리 정보, NEW 여부를 내부에서 조회
            _info = _equipmentService.GetInventoryInfo(_data.Code);
            _isNew = _equipmentService.IsNewEquipment(_data.Code);

            _icon.sprite = data.Icon;
            _rankText.text = data.Grade.ToString();
            _levelText.text = $"Lv. {_info.Level}";

            bool isOwned = _info.Level > 0 && _info.Count > 0;

            _slotButton.interactable = isOwned;
            _equipToggleButton.gameObject.SetActive(isOwned);
            _close.SetActive(!isOwned);

            _newText.SetActive(_isNew);

            if (_isNew)
            {
                _equipmentService.MarkAsSeen(_data.Code);
            }

            // EquipToggle Update
            string equippedCode = _equipmentService.GetEquippedCode(_data.Type);
            bool isEquipped = isOwned && !string.IsNullOrEmpty(equippedCode) && equippedCode == _data.Code;
            UpdateEquipToggleColor(isEquipped);

            // Progress Update
            UpdateProgressUI();
        }

        private void UpdateEquipToggleColor(bool isEquipped)
        {
            if (_equipToggleButton == null)
                return;

            var image = _equipToggleButton.image;
            if (image == null)
                return;

            image.color = isEquipped ? _equippedColor : _unequippedColor;
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

        private void HideNewIfActive()
        {
            if (!_isNew)
                return;

            _isNew = false;
            if (_newText != null)
                _newText.gameObject.SetActive(false);
        }

        public void OnEquipped()
        {
            HideNewIfActive();
        }

        public void OnClickSlot()
        {
            HideNewIfActive();
        }
    }
}
