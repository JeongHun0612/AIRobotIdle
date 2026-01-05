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
        [SerializeField] private TMP_Text _upgradeButtonText; // 예: "강화\n7QZ"

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

        public void Refresh(int currentLevel, BigDouble currentValue, BigDouble nextValue, BigDouble cost, bool isLocked, string lockReason, Sprite fallbackIcon)
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

            // 버튼 상태 및 가격
            if (_upgradeButtonText != null)
            {
                _upgradeButtonText.text = cost > 0 ? $"강화\n{cost}" : "MAX";
            }

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = !isLocked;
            }

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
