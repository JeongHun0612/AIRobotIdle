using BreakInfinity;
using SahurRaising.Core;
using SahurRaising.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    public class UICurrencySlot : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _amountText;

        public CurrencyType Type { get; private set; }

        public void Initialize(CurrencyType type, Sprite icon, BigDouble amount)
        {
            Type = type;

            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.gameObject.SetActive(icon != null);
            }

            Refresh(amount);
            gameObject.SetActive(true);
        }

        public void Refresh(BigDouble amount)
        {
            if (_amountText != null)
            {
                // TODO: 필요하다면 전역 포맷팅 유틸리티 사용 (예: 1.2A, 1.5B 등)
                _amountText.text = amount.ToString("F0");
                //_amountText.text = NumberFormatUtil.FormatBigDouble(amount);
            }
        }
    }
}
