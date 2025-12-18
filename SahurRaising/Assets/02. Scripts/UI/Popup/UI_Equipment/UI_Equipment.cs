using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    public class UI_Equipment : UI_Popup
    {
        [Header("장비 탭 버튼")]
        [SerializeField] private List<EquipmentTabButton> _tabButtons = new();

        [Header("장비 슬롯")]
        [SerializeField] private List<EquipmentSlot> _equipmentSlots = new();

        [Header("인벤토리 슬롯")]
        [SerializeField] private Transform _inventoryContentRoot; // Content
        [SerializeField] private ItemSlot _itemSlotPrefab;        // ItemSlot 프리팹

        [Header("장비 정보")]
        [SerializeField] private EquipmentInfo _equipmentInfo;

        [Header("디버깅")]
        [SerializeField] private string _equimentCode = "W1";


        private readonly List<ItemSlot> _itemSlots = new();

        private IEquipmentService _equipmentService;

        private EquipmentType _currentType = EquipmentType.Weapon;

        public async override UniTask InitializeAsync()
        {
            RegisterTabButtons();

            _itemSlots.Clear();
            if (_inventoryContentRoot != null)
            {
                foreach (Transform child in _inventoryContentRoot)
                {
                    var slot = child.GetComponent<ItemSlot>();
                    if (slot == null)
                        continue;

                    slot.Initialize(_equipmentService);
                    slot.RegisterEquipToggleCallback(OnClickEquipToggle);
                    slot.RegisterSlotCallback(OnClickItemSlot);

                    _itemSlots.Add(slot);
                    slot.gameObject.SetActive(false); // 초기에는 비활성화
                }
            }

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            _equipmentInfo.Hide();

            OnClickTabButton(_currentType);
            RefreshEquipmentSlots();
        }

        private void RegisterTabButtons()
        {
            foreach (var tabButton in _tabButtons)
            {
                tabButton.Register(OnClickTabButton);
            }
        }

        private void RefreshInventoryByCurrentType()
        {
            if (_inventoryContentRoot == null || _itemSlotPrefab == null)
                return;

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            // 현재 탭 타입 기준으로 장비 리스트 가져오기
            IReadOnlyList<EquipmentRow> equipmentList = _equipmentService.GetByType(_currentType);

            int needCount = equipmentList.Count;

            // 필요한 슬롯 수만큼 보장 (부족하면 새로 생성하여 풀에 추가)
            while (_itemSlots.Count < needCount)
            {
                ItemSlot newSlot = Instantiate(_itemSlotPrefab, _inventoryContentRoot);
                newSlot.Initialize(_equipmentService);
                newSlot.RegisterEquipToggleCallback(OnClickEquipToggle);
                newSlot.RegisterSlotCallback(OnClickItemSlot);
                newSlot.gameObject.SetActive(false);
                _itemSlots.Add(newSlot);
            }

            // 슬롯 채우기
            for (int i = 0; i < _itemSlots.Count; i++)
            {
                if (i < needCount)
                {
                    var data = equipmentList[i];
                    var slot = _itemSlots[i];

                    slot.gameObject.SetActive(true);
                    slot.SetData(data);
                }
                else
                {
                    // 남는 슬롯은 비활성화 (풀 안에서 대기)
                    _itemSlots[i].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshEquipmentSlots()
        {
            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            foreach (var slot in _equipmentSlots)
            {
                if (slot == null)
                    continue;

                var type = slot.Type;

                // 이 타입에 현재 장착된 코드
                string equippedCode = _equipmentService.GetEquippedCode(type);

                // 아무것도 장착 안 되어 있으면 슬롯 비우기
                if (string.IsNullOrEmpty(equippedCode))
                {
                    slot.Clear();
                    continue;
                }

                // 코드로 EquipmentRow 조회
                if (_equipmentService.TryGetByCode(equippedCode, out var row))
                {
                    slot.SetEquipped(row);
                }
                else
                {
                    // 저장 데이터는 있는데 테이블에는 없는 코드일 때 안전하게 Clear
                    slot.Clear();
                }
            }
        }

        private EquipmentSlot GetEquipmentSlot(EquipmentType type)
        {
            foreach (var equipSlot in _equipmentSlots)
            {
                if (equipSlot == null)
                    continue;
                if (equipSlot.Type == type)
                    return equipSlot;
            }
            return null;
        }

        public void OnClickTabButton(EquipmentType type)
        {
            foreach (var tabButton in _tabButtons)
            {
                tabButton.OnShow(tabButton.Type == type);
            }

            _currentType = type;

            RefreshInventoryByCurrentType();
        }

        public void OnClickItemSlot(ItemSlot slot)
        {
            if (slot == null)
                return;

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            var data = slot.Data;
            if (string.IsNullOrEmpty(data.Code))
                return;

            // 패널 활성화
            _equipmentInfo.Show();
            _equipmentInfo.RefreshEquipmentInfo(data);
        }

        public void OnClickEquipToggle(ItemSlot slot)
        {
            if (slot == null)
                return;

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();

            var data = slot.Data;
            if (string.IsNullOrEmpty(data.Code))
                return;

            EquipmentSlot targetSlot = GetEquipmentSlot(data.Type);
            if (targetSlot == null)
                return;

            // 현재 이 타입에 장착된 코드
            string currentCode = _equipmentService.GetEquippedCode(data.Type);

            // === 토글 OFF: 같은 아이템을 다시 누른 경우 ===
            if (!string.IsNullOrEmpty(currentCode) && currentCode == data.Code)
            {
                if (_equipmentService.Unequip(data.Type))
                {
                    targetSlot.Clear();
                }
            }
            // === 토글 ON/교체: 다른 아이템을 누른 경우 ===
            else
            {
                if (_equipmentService.Equip(data.Type, data.Code))
                {
                    slot.OnEquipped();
                    targetSlot.SetEquipped(data);
                }
            }

            // 인벤토리 전체 갱신
            RefreshInventoryByCurrentType();

            // 장비 슬롯 갱신
            RefreshEquipmentSlots();
        }

        public void OnClickGacha()
        {
            //UIManager.Instance.ShowPopup(EPopupUIType.Gacha);

            _equipmentService.AddToInventory(_equimentCode);

            RefreshInventoryByCurrentType();
        }

        public void OnClickUpgrade()
        {
            Debug.Log("[UI_Equipment] OnClickUpgrade");
        }

        public void OnClickBack()
        {
            UIManager.Instance.CloseCurrentPopup();
        }
    }
}
