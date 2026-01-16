using SahurRaising.Core;
using TMPro;
using UnityEngine;

namespace SahurRaising
{
    public class OptionStatPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _equipTypeText;
        [SerializeField] private TMP_Text _currentStatText;
        [SerializeField] private TMP_Text _nextStatText;

        public void UpdateEquipmentStatText(OptionValue optionValue, int level)
        {
            if (_equipTypeText != null)
            {
                _equipTypeText.text = optionValue.Type;
            }

            double currentValue = optionValue.Base + (Mathf.Max(0, level - 1) * optionValue.Up);
            double nextValue = currentValue + optionValue.Up;

            if (_currentStatText != null)
            {
                _currentStatText.text = currentValue.ToString("F2");
            }

            if (_nextStatText != null)
            {
                _nextStatText.text = nextValue.ToString("F2");
            }
        }
    }
}
