using SahurRaising.Core;
using TMPro;
using UnityEngine;

namespace SahurRaising
{
    public class EquipmentStatPanel : MonoBehaviour
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

            if (_currentStatText != null)
            {
                double currentValue = optionValue.Base * level;
                _currentStatText.text = currentValue.ToString("F2");
            }

            if (_nextStatText != null)
            {
                double currentValue = optionValue.Base * level;
                double nextValue = currentValue + optionValue.Up;
                _nextStatText.text = nextValue.ToString("F2");
            }
        }
    }
}
