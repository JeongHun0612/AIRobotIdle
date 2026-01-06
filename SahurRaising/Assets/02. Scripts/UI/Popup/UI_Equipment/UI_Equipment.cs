using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SahurRaising.Core;
using SahurRaising.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_Equipment : UI_Popup
    {
        [Header("장비 탭 버튼")]
        [SerializeField] private List<EquipmentTabButton> _tabButtons = new();

        [Header("인벤토리 슬롯")]
        [SerializeField] private Transform _inventoryContentRoot; // Content
        [SerializeField] private ItemSlot _itemSlotPrefab;        // ItemSlot 프리팹
        [SerializeField] private ScrollRect _scrollRect;

        [Header("장비 정보 영역")]
        [SerializeField] private EquipmentInfo _equipmentInfo;

        private readonly List<ItemSlot> _itemSlots = new();

        private ItemSlot _selectedSlot;

        private IEquipmentService _equipmentService;

        private EquipmentType _currentType = EquipmentType.Processor;
        private bool _isComponentsInitialized = false;

        public async override UniTask InitializeAsync()
        {
            // 서비스 바인딩 시도 (실패 시 무시하고 진행)
            TryBindService();

            // 탭 버튼 이벤트 등록
            RegisterTabButtons();

            // 아이템 슬롯 초기화
            _itemSlots.Clear();
            if (_inventoryContentRoot != null)
            {
                foreach (Transform child in _inventoryContentRoot)
                {
                    var slot = child.GetComponent<ItemSlot>();
                    if (slot == null)
                        continue;

                    // 서비스가 있으면 즉시 초기화, 없으면 OnShow 시점으로 지연
                    if (_equipmentService != null)
                        slot.Initialize(_equipmentService);

                    slot.RegisterClickHandler(OnClickItemSlot);

                    _itemSlots.Add(slot);
                    slot.gameObject.SetActive(false); // 초기에는 비활성화
                }
            }

            // 장비 정보 패널 초기화
            if (_equipmentService != null)
            {
                _equipmentInfo.Initialize(_equipmentService, RefreshInventoryByCurrentType);
                _isComponentsInitialized = true;
            }

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            EnsureComponentsInitialized();

            OnClickTabButton(_currentType);
        }

        private bool TryBindService()
        {
            if (_equipmentService == null && ServiceLocator.HasService<IEquipmentService>())
            {
                _equipmentService = ServiceLocator.Get<IEquipmentService>();
            }

            return _equipmentService != null;
        }

        private void EnsureComponentsInitialized()
        {
            if (_isComponentsInitialized || _equipmentService == null)
                return;

            // 기존 슬롯들 초기화 (InitializeAsync 시점에 서비스가 없어서 못한 경우)
            foreach (var slot in _itemSlots)
            {
                slot.Initialize(_equipmentService);
            }

            // 장비 정보 패널 초기화
            _equipmentInfo.Initialize(_equipmentService, RefreshInventoryByCurrentType);

            _isComponentsInitialized = true;
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
            if (_equipmentService == null || _inventoryContentRoot == null || _itemSlotPrefab == null)
                return;

            // 현재 탭 타입 기준으로 장비 리스트 가져오기
            IReadOnlyList<EquipmentRow> equipmentList = _equipmentService.GetByType(_currentType);

            int needCount = equipmentList.Count;

            // 필요한 슬롯 수만큼 보장 (부족하면 새로 생성하여 풀에 추가)
            while (_itemSlots.Count < needCount)
            {
                ItemSlot newSlot = Instantiate(_itemSlotPrefab, _inventoryContentRoot);
                newSlot.Initialize(_equipmentService);
                newSlot.RegisterClickHandler(OnClickItemSlot);
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

        private void ShowEquippedEquipmentInfo()
        {
            if (_equipmentService == null || _equipmentInfo == null)
                return;

            // 현재 탭 타입에 장착된 장비 코드 가져오기
            string equippedCode = _equipmentService.GetEquippedCode(_currentType);

            if (string.IsNullOrEmpty(equippedCode))
                return; // 장착된 장비가 없으면 표시하지 않음

            // 장비 데이터 가져오기
            if (!_equipmentService.TryGetByCode(equippedCode, out EquipmentRow equippedData))
                return;

            // EquipmentInfo에 표시
            _equipmentInfo.RefreshEquipmentInfo(equippedData);

            // 해당 ItemSlot 찾아서 선택 상태로 표시
            ItemSlot equippedSlot = FindItemSlotByCode(equippedCode);
            if (equippedSlot != null)
            {
                SetSelectedSlot(equippedSlot);
                ScrollToItemSlot(equippedSlot);
            }
        }

        private ItemSlot FindItemSlotByCode(string code)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot != null && slot.Data.Code == code)
                {
                    return slot;
                }
            }
            return null;
        }

        private void ScrollToItemSlot(ItemSlot itemSlot)
        {
            if (_scrollRect == null || itemSlot == null || _inventoryContentRoot == null)
                return;

            RectTransform contentRect = _inventoryContentRoot as RectTransform;
            RectTransform itemRect = itemSlot.transform as RectTransform;
            RectTransform viewportRect = _scrollRect.viewport;

            if (contentRect == null || itemRect == null || viewportRect == null)
                return;

            // Content의 전체 높이
            float contentHeight = contentRect.rect.height;
            float viewportHeight = viewportRect.rect.height;

            // 스크롤할 필요가 없으면 리턴
            if (contentHeight <= viewportHeight)
                return;

            // ItemSlot의 위치 계산 (Content 기준)
            float itemPosition = -itemRect.anchoredPosition.y;
            float itemHeight = itemRect.rect.height;

            // Viewport 중앙에 오도록 계산
            float targetPosition = itemPosition - (viewportHeight * 0.5f) + (itemHeight * 0.5f);

            // Normalized position 계산 (0~1 범위)
            float normalizedPosition = 1f - (targetPosition / (contentHeight - viewportHeight));
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // DOTween으로 부드럽게 스크롤
            DOTween.To(
                () => _scrollRect.verticalNormalizedPosition,
                x => _scrollRect.verticalNormalizedPosition = x,
                normalizedPosition,
                0.3f
            ).SetEase(Ease.OutCubic);
        }

        private void SetSelectedSlot(ItemSlot itemSlot)
        {
            // 이전 선택 해제
            if (_selectedSlot != null && _selectedSlot != itemSlot)
            {
                _selectedSlot.SetFocus(false);
            }

            // 새 선택 활성화
            _selectedSlot = itemSlot;
            if (_selectedSlot != null)
            {
                _selectedSlot.SetFocus(true);
            }
        }

        public void OnClickTabButton(EquipmentType type)
        {
            // 선택 해제
            if (_selectedSlot != null)
            {
                _selectedSlot.SetFocus(false);
                _selectedSlot = null;
            }

            foreach (var tabButton in _tabButtons)
            {
                tabButton.OnShow(tabButton.Type == type);
            }

            _currentType = type;

            RefreshInventoryByCurrentType();

            // 현재 탭 타입에 장착된 장비 정보 표시
            ShowEquippedEquipmentInfo();
        }

        public void OnClickItemSlot(ItemSlot itemSlot)
        {
            SetSelectedSlot(itemSlot);

            if (_equipmentInfo != null && itemSlot.Data.Code != null)
            {
                itemSlot.HideNewIfActive();
                _equipmentInfo.RefreshEquipmentInfo(itemSlot.Data);
            }
        }

        public void OnClickGacha()
        {
            //UIManager.Instance.ShowPopup(EPopupUIType.Gacha);

            _equipmentService.AddToInventory("W1");

            RefreshInventoryByCurrentType();
        }

        public void OnClickUpgrade()
        {
            Debug.Log("[UI_Equipment] OnClickUpgrade");
        }
    }
}
