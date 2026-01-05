using SahurRaising.Core;

namespace SahurRaising
{
    public class EquipmentInfoItemSlot : ItemSlot
    {
        public override void SetData(EquipmentRow data)
        {
            base.SetData(data);

            _slotButton.interactable = false;
            _equipIcon.SetActive(false);
        }
    }
}
