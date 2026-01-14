using System;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class ItemSlot : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private GameObject[] _gemIcons;

        [Header("버튼")]
        [SerializeField] private Button _slotButton;

        [Header("상태 표시")]
        [SerializeField] private GameObject _newText;
        [SerializeField] private GameObject _equipIcon;
        [SerializeField] private GameObject _close;
        [SerializeField] private GameObject _focus;

        [Header("강화 진행도 UI")]
        [SerializeField] private Slider _progressSlider;   // 슬라이더 바
        [SerializeField] private TMP_Text _progressText;   // "보유/필요" 텍스트

        private IEquipmentService _equipmentService;
        private IConfigService _configService;

        private EquipmentRow _data;
        private bool _isNew;

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

        public void SetData(EquipmentRow data)
        {
            _data = data;
            UpdateUI();
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

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(_data.Code))
                return;

            if (_configService == null)
                _configService = ServiceLocator.Get<IConfigService>();

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            if (_configService == null || _configService.ItemVisualConfig == null)
            {
                Debug.LogWarning("[ItemSlot] ItemVisualConfig를 찾을 수 없습니다.");
                return;
            }

            if (_equipmentService == null || !_equipmentService.IsInitialized)
            {
                Debug.LogWarning("[ItemSlot] EquipmentService를 찾을 수 없거나 초기화되지 않았습니다.");
                return;
            }

            // 인벤토리 정보 조회
            var info = _equipmentService.GetInventoryInfo(_data.Code);

            string gradeString = _data.Grade.ToString();
            var (gradeLetter, gemCount) = StringUtils.ParseLettersAndNumber(gradeString);

            // 배경 색깔 설정
            if (_bgImage != null)
            {
                _bgImage.color = _configService.GetColorForGrade(GachaType.Equipment, gradeString);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                var icon = _data.Icon;
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }

            // 랭크 텍스트 설정
            if (_rankText != null)
            {
                _rankText.text = gradeLetter;
            }

            // 레벨 텍스트 설정
            if (_levelText != null)
            {
                _levelText.text = $"Lv. {info.Level}";
            }

            // 잼(등급 카운트) 갯수 설정
            UpdateGemIcons(gemCount);

            // 아이템을 보유 하고 있는지
            bool isOwned = info.IsOwned;
            if (_slotButton != null)
            {
                _slotButton.interactable = isOwned;
            }

            if (_close != null)
            {
                _close.SetActive(!isOwned);
            }

            // 장착 여부 확인하여 아이콘 활성화/비활성화
            string equippedCode = _equipmentService.GetEquippedCode(_data.Type);
            bool isEquip = !string.IsNullOrEmpty(equippedCode) && equippedCode == _data.Code;

            if (_equipIcon != null)
            {
                _equipIcon.SetActive(isOwned && isEquip);
            }

            // 새로 획득한 아이템인지 확인
            _isNew = _equipmentService.IsNewEquipment(_data.Code);
            if (_newText != null)
            {
                _newText.SetActive(_isNew);
            }

            if (_isNew)
            {
                _equipmentService.MarkAsSeen(_data.Code);
            }

            // Focus 오브젝트 비활성화
            SetFocus(false);

            // 프로그래스바 UI 업데이트
            int ownedCount = info.Count;
            int requiredCount = _equipmentService.GetRequiredCountForAdvance();
            UpdateProgressUI(ownedCount, requiredCount);
        }

        private void UpdateGemIcons(int activeCount)
        {
            if (_gemIcons == null)
                return;

            for (int i = 0; i < _gemIcons.Length; i++)
            {
                if (_gemIcons[i] != null)
                {
                    // activeCount만큼 활성화
                    _gemIcons[i].SetActive(i < activeCount);
                }
            }
        }

        private void UpdateProgressUI(int ownedCount, int requiredCount)
        {
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
    }
}
