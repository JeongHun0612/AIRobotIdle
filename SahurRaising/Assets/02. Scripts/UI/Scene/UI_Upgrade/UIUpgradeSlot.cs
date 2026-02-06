using System;
using BreakInfinity;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    /// <summary>
    /// 업그레이드 슬롯 (아이콘, 이름, 설명, 수치, 강화 버튼 포함)
    /// </summary>
    public sealed class UIUpgradeSlot : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _valueText; // 예: "17.3B -> 18.4B"

        [Header("Action")]
        [SerializeField] private Button _upgradeButton;

        [Header("Button States - Normal")]
        [SerializeField] private GameObject _normalState;       // 버튼 활성화 상태 오브젝트
        [SerializeField] private TMP_Text _normalLabelText;     // Normal 상태 "강화" 라벨
        [SerializeField] private TMP_Text _normalCostText;      // Normal 상태 비용 텍스트

        [Header("Button States - Disabled")]
        [SerializeField] private GameObject _disabledState;     // 버튼 비활성화 상태 오브젝트
        [SerializeField] private TMP_Text _disabledLabelText;   // Disabled 상태 라벨
        [SerializeField] private TMP_Text _disabledCostText;    // Disabled 상태 비용 텍스트

        [Header("Lock UI")]
        [SerializeField] private GameObject _lockRoot;
        [SerializeField] private TMP_Text _lockText;

        private UpgradeRow _row;
        private Action<string> _onUpgrade;

        public string Code => _row.Code;
        public UpgradeTier Tier => _row.Tier;

        public void Initialize(UpgradeRow row, Action<string> onUpgrade)
        {
            _row = row;
            _onUpgrade = onUpgrade;

            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.RemoveAllListeners();
                _upgradeButton.onClick.AddListener(HandleUpgradeClicked);
            }
        }

        public void Refresh(int currentLevel, BigDouble currentValue, BigDouble nextValue, BigDouble cost, bool isLocked, string lockReason, Sprite fallbackIcon, bool hasEnoughCurrency)
        {
            // 기본 정보
            if (_iconImage != null)
                _iconImage.sprite = _row.Icon != null ? _row.Icon : fallbackIcon;

            if (_titleText != null) _titleText.text = _row.Name;
            if (_descText != null) _descText.text = _row.Description;
            if (_levelText != null) _levelText.text = $"LV {currentLevel}";

            // 수치 변화 (예: 10 -> 15)
            if (_valueText != null)
            {
                // 포맷은 필요에 따라 조정 (예: G3, F1 등)
                _valueText.text = $"{currentValue} -> {nextValue}";
            }

            // 버튼 상태 결정
            // - cost <= 0: MAX 레벨 도달
            // - cost > 0 && !hasEnoughCurrency: 비용 부족
            // - cost > 0 && hasEnoughCurrency: 강화 가능
            bool isMaxLevel = cost <= 0;
            bool canUpgrade = !isLocked && !isMaxLevel && hasEnoughCurrency;
            
            // 라벨 결정: 강화하기 / 비용부족 / MAX
            string normalLabel = "강화하기";
            string disabledLabel = isMaxLevel ? "MAX" : "비용부족";
            string costText = isMaxLevel ? "" : $"{cost}";
            
            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = canUpgrade;
            }
            
            // 버튼 시각적 상태 전환
            if (_normalState != null) _normalState.SetActive(canUpgrade);
            if (_disabledState != null) _disabledState.SetActive(!canUpgrade);
            
            // Normal 상태 텍스트 (강화 가능할 때만 보임)
            if (_normalLabelText != null) _normalLabelText.text = normalLabel;
            if (_normalCostText != null) _normalCostText.text = costText;
            
            // Disabled 상태 텍스트 (비용부족 또는 MAX)
            if (_disabledLabelText != null) _disabledLabelText.text = disabledLabel;
            if (_disabledCostText != null) _disabledCostText.text = costText;

            // 잠금 상태
            if (_lockRoot != null) _lockRoot.SetActive(isLocked);
            if (_lockText != null) _lockText.text = isLocked ? lockReason : "";
        }

        private void HandleUpgradeClicked()
        {
            if (string.IsNullOrEmpty(_row.Code)) return;
            _onUpgrade?.Invoke(_row.Code);
        }
    }
}
