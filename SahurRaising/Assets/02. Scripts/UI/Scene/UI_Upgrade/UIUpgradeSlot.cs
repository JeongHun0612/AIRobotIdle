using System;
using BreakInfinity;
using SahurRaising.Core;
using SahurRaising.Utils;
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

        [Header("Appearance")]
        [SerializeField] private Image _iconBgImage;     // Icon (배경)
        [SerializeField] private Image _iconBorderImage; // IconBorder (테두리)
        [SerializeField] private Image _iconLightImage;  // IconLight (상단 하이라이트)

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

        /// <summary>
        /// 티어 색상 세트를 적용합니다. (배경, 테두리, 하이라이트)
        /// </summary>
        public void SetTierColor(TierColorSet colorSet)
        {
            if (_iconBgImage != null)
            {
                Color bg = colorSet.BaseColor;
                bg.a = 1f;
                _iconBgImage.color = bg;
            }

            if (_iconBorderImage != null)
            {
                Color border = colorSet.BorderColor;
                border.a = 1f;
                _iconBorderImage.color = border;
            }

            if (_iconLightImage != null)
            {
                Color light = colorSet.LightColor;
                light.a = Mathf.Max(light.a, 0.1f); // 하이라이트는 반투명 허용, 완전 투명 방지
                _iconLightImage.color = light;
            }
        }

        public void Refresh(int currentLevel, int maxLevel, BigDouble currentValue, BigDouble nextValue, BigDouble cost, bool isLocked, string lockReason, Sprite fallbackIcon, bool hasEnoughCurrency)
        {
            // 기본 정보
            if (_iconImage != null)
                _iconImage.sprite = _row.Icon != null ? _row.Icon : fallbackIcon;

            if (_titleText != null) _titleText.text = _row.Name;
            if (_descText != null) _descText.text = _row.Description;
            if (_levelText != null) _levelText.text = $"LV {currentLevel}";

            // 수치 변화 (예: 10 -> 15)
            // NumberFormatUtil을 사용하여 큰 수를 가독성 있게 표시
            if (_valueText != null)
            {
                string currentStr = FormatStatValue(currentValue);
                string nextStr = FormatStatValue(nextValue);
                _valueText.text = $"{currentStr} -> {nextStr}";
            }

            // 버튼 상태 결정
            // - MaxLevel과 비교하여 정확히 MAX 판단
            bool isMaxLevel = currentLevel >= maxLevel;
            bool canUpgrade = !isLocked && !isMaxLevel && hasEnoughCurrency;
            
            // 라벨 결정: 강화하기 / 비용부족 / MAX
            string normalLabel = "강화하기";
            string disabledLabel = isMaxLevel ? "MAX" : "강화하기";
            
            // 비용 텍스트: MAX면 빈 문자열, 아니면 포맷팅된 값
            string costText = isMaxLevel ? "" : NumberFormatUtil.FormatBigDouble(cost);
            
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

        /// <summary>
        /// 스탯 값을 포맷팅합니다.
        /// 비율 스탯(ATKSP, CR 등 소수값)은 소수 형태로,
        /// 큰 스탯(ATK, HP 등)은 알파벳 표기법으로 표시합니다.
        /// </summary>
        private string FormatStatValue(BigDouble value)
        {
            if (value == 0)
                return "0";

            // 1 미만의 작은 비율 값 (퍼센트/비율 스탯)
            if (value < 1)
            {
                double d = value.ToDouble();
                // 소수점 이하 유효숫자가 있는 경우 적절한 자릿수 사용
                if (System.Math.Abs(d) < 0.001)
                    return d.ToString("G4");
                return d.ToString("F4").TrimEnd('0').TrimEnd('.');
            }

            // 1000 미만은 소수점 포함 가능
            if (value < 1000)
            {
                double d = value.ToDouble();
                if (System.Math.Abs(d % 1) < double.Epsilon)
                    return ((long)d).ToString();
                return d.ToString("F2").TrimEnd('0').TrimEnd('.');
            }

            // 큰 숫자는 NumberFormatUtil 사용
            return NumberFormatUtil.FormatBigDouble(value);
        }

        private void HandleUpgradeClicked()
        {
            if (string.IsNullOrEmpty(_row.Code)) return;
            _onUpgrade?.Invoke(_row.Code);
        }
    }
}
