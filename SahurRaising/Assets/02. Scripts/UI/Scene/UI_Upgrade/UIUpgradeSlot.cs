using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SahurRaising.Core;

namespace SahurRaising.UI
{
    /// <summary>
    /// 업그레이드 슬롯 1칸(아이콘/레벨/잠금/선택).
    /// 왜: 장비 슬롯 UI 리소스(프레임/자물쇠 등)는 재사용하되, 데이터 소스/로직은 업그레이드 전용으로 분리한다.
    /// </summary>
    public sealed class UIUpgradeSlot : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _levelText;

        [Header("Lock UI")]
        [SerializeField] private GameObject _lockRoot;
        [SerializeField] private Image _lockIconImage;
        [SerializeField] private TMP_Text _lockText;

        [Header("Selection (Optional)")]
        [SerializeField] private GameObject _selectedRoot;

        private UpgradeRow _row;
        private Action<string> _onSelected;

        public string Code => _row.Code;
        public UpgradeTier Tier => _row.Tier;

        public void Initialize(UpgradeRow row, Action<string> onSelected)
        {
            _row = row;
            _onSelected = onSelected;

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
                _button.onClick.AddListener(HandleClicked);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_selectedRoot != null)
                _selectedRoot.SetActive(selected);
        }

        public void Refresh(int currentLevel, bool isLocked, string lockReason, Sprite fallbackIcon)
        {
            if (_iconImage != null)
                _iconImage.sprite = _row.Icon != null ? _row.Icon : fallbackIcon;

            if (_levelText != null)
                _levelText.text = $"LV {Mathf.Max(0, currentLevel)}";

            if (_lockRoot != null)
                _lockRoot.SetActive(isLocked);

            if (_lockText != null)
                _lockText.text = isLocked ? lockReason : string.Empty;

            if (_button != null)
                _button.interactable = !isLocked;

            // 프레임/색상 등은 리소스 통일을 위해 프리팹에서 세팅한 값을 그대로 사용한다.
        }

        public void SetLockIcon(Sprite lockSprite)
        {
            if (_lockIconImage != null)
                _lockIconImage.sprite = lockSprite;
        }

        private void HandleClicked()
        {
            if (string.IsNullOrEmpty(_row.Code))
                return;

            _onSelected?.Invoke(_row.Code);
        }
    }
}


