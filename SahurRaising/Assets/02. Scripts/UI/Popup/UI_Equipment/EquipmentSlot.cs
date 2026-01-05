using SahurRaising.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class EquipmentSlot : MonoBehaviour
    {
        [SerializeField] private EquipmentType _type;

        [SerializeField] private Image _emptyIcon;
        [SerializeField] private Image _equipmentIcon;

        public EquipmentType Type => _type;

        public void SetEquipped(EquipmentRow data)
        {
            if (_equipmentIcon == null || _emptyIcon == null)
                return;

            bool hasEquip = !string.IsNullOrEmpty(data.Code);

            if (hasEquip)
            {
                _equipmentIcon.sprite = data.Icon;
            }

            _equipmentIcon.gameObject.SetActive(hasEquip);
            _emptyIcon.gameObject.SetActive(!hasEquip);
        }

        public void Clear()
        {
            if (_equipmentIcon == null || _emptyIcon == null)
                return;

            _equipmentIcon.gameObject.SetActive(false);
            _emptyIcon.gameObject.SetActive(true);
        }
    }
}
