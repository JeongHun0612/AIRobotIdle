using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SahurRaising.Core;
using SahurRaising.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_Equipment : UI_Popup
    {
        [Header("장비 탭 버튼")]
        [SerializeField] private List<EquipmentTabButton> _tabButtons = new();

        [Header("인벤토리 슬롯")]
        [SerializeField] private Transform _inventoryContentRoot;          // Content
        [SerializeField] private EquipmentItemSlot _itemSlotPrefab;        // ItemSlot 프리팹
        [SerializeField] private ScrollRect _scrollRect;

        [Header("장비 정보 영역")]
        [SerializeField] private EquipmentInfo _equipmentInfo;

        [Header("뽑기 버튼")]
        [SerializeField] private Button _gachaButton;

        [Header("합성 버튼")]
        [SerializeField] private Button _advanceButton;
        [SerializeField] private GameObject _advanceButtonDisabled;

        private readonly List<EquipmentItemSlot> _itemSlots = new();

        private EquipmentType _currentType = EquipmentType.Processor;
        private EquipmentItemSlot _selectedSlot;

        private IEquipmentService _equipmentService;
        private IEventBus _eventBus;

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
                    var slot = child.GetComponent<EquipmentItemSlot>();
                    if (slot == null)
                        continue;

                    // 초기화 및 클릭이벤트 등록
                    slot.Initialize();
                    slot.RegisterClickHandler(OnClickItemSlot);
                    _itemSlots.Add(slot);
                    slot.gameObject.SetActive(false); // 초기에는 비활성화
                }
            }

            // 장비 정보 패널 초기화
            if (_equipmentInfo != null)
            {
                _equipmentInfo.Initialize(RefreshInventoryByCurrentType);
            }

            // 버튼 이벤트 등록
            if (_gachaButton != null)
            {
                _gachaButton.onClick.RemoveAllListeners();
                _gachaButton.onClick.AddListener(OnClickGacha);
            }

            if (_advanceButton != null)
            {
                _advanceButton.onClick.RemoveAllListeners();
                _advanceButton.onClick.AddListener(OnClickAdvanceAll);
            }

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            if (_eventBus != null)
            {
                _eventBus.Subscribe<EquipmentInventoryChangedEvent>(OnEquipmentInventoryChanged);
                _eventBus.Subscribe<EquipmentEquippedEvent>(OnEquipmentEquipped);
            }

            OnClickTabButton(_currentType);
        }

        public override void OnHide()
        {
            base.OnHide();

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<EquipmentInventoryChangedEvent>(OnEquipmentInventoryChanged);
                _eventBus.Unsubscribe<EquipmentEquippedEvent>(OnEquipmentEquipped);
            }

            _selectedSlot = null;
        }

        private bool TryBindService()
        {
            if (_equipmentService == null && ServiceLocator.HasService<IEquipmentService>())
            {
                _equipmentService = ServiceLocator.Get<IEquipmentService>();
            }

            if (_eventBus == null && ServiceLocator.HasService<IEventBus>())
            {
                _eventBus = ServiceLocator.Get<IEventBus>();
            }

            return _equipmentService != null;
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
                EquipmentItemSlot newSlot = Instantiate(_itemSlotPrefab, _inventoryContentRoot);
                newSlot.Initialize();
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

            if (_selectedSlot != null)
            {
                _selectedSlot.SetFocus(true);
            }
        }

        private void ShowEquippedEquipmentInfo()
        {
            if (_equipmentService == null || _equipmentInfo == null)
                return;

            // 현재 탭 타입에 장착된 장비 코드 가져오기
            string equippedCode = _equipmentService.GetEquippedCode(_currentType);

            if (string.IsNullOrEmpty(equippedCode))
            {
                // 장착된 장비가 없으면 현재 탭의 인벤토리 중 첫 번째 장비를 타겟으로 설정
                IReadOnlyList<EquipmentRow> equipmentList = _equipmentService.GetByType(_currentType);

                if (equipmentList == null || equipmentList.Count == 0)
                    return;

                // 첫 번째 장비를 타겟으로 설정
                EquipmentRow firstEquipment = equipmentList[0];
                _equipmentInfo.UpdateItemInfo(firstEquipment);

                // 해당 ItemSlot 찾아서 선택 상태로 표시
                EquipmentItemSlot firstSlot = FindItemSlotByCode(firstEquipment.Code);
                if (firstSlot != null)
                {
                    SetSelectedSlot(firstSlot);
                    ScrollToItemSlot(firstSlot);
                }
                return;
            }

            // 장비 데이터 가져오기
            if (!_equipmentService.TryGetByCode(equippedCode, out EquipmentRow equippedData))
                return;

            // EquipmentInfo에 표시
            _equipmentInfo.UpdateItemInfo(equippedData);

            if (_selectedSlot == null)
            {
                // 해당 ItemSlot 찾아서 선택 상태로 표시
                EquipmentItemSlot equippedSlot = FindItemSlotByCode(equippedCode);
                if (equippedSlot != null)
                {
                    SetSelectedSlot(equippedSlot);
                    ScrollToItemSlot(equippedSlot);
                }
            }
        }

        private EquipmentItemSlot FindItemSlotByCode(string code)
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

        private void ScrollToItemSlot(EquipmentItemSlot itemSlot)
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

        private void SetSelectedSlot(EquipmentItemSlot itemSlot)
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

            // 인벤토리 현재 타입에 맞춰 갱신
            RefreshInventoryByCurrentType();

            // 현재 탭 타입에 장착된 장비 정보 표시
            ShowEquippedEquipmentInfo();

            // 일괄합성 버튼 상태 업데이트
            UpdateAdvanceButtonState();
        }

        public void OnClickItemSlot(EquipmentItemSlot itemSlot)
        {
            // 이미 선택된 슬롯을 다시 클릭한 경우 return
            if (_selectedSlot != null && ReferenceEquals(_selectedSlot, itemSlot))
                return;

            SetSelectedSlot(itemSlot);

            if (_equipmentInfo != null && itemSlot.Data.Code != null)
            {
                itemSlot.HideNewIfActive();
                _equipmentInfo.UpdateItemInfo(itemSlot.Data);
            }
        }

        public void OnClickGacha()
        {
            var mainScene = UIManager.Instance.GetCurrentScene<UIMainRootScene>();
            if (mainScene == null || mainScene.BottomBarMenu == null)
                return;

            // Shop 팝업 열기
            mainScene.BottomBarMenu.SetCurrent(EPopupUIType.Shop);

            UIManager.Instance.CloseAllPopups();
            var shopPopup = UIManager.Instance.ShowPopup<UI_ShopPopup>(EPopupUIType.Shop);
            if (shopPopup != null)
            {
                shopPopup.OnClickTabButton(EPopupUIType.Gacha);
            }
        }

        private void UpdateAdvanceButtonState()
        {
            // 현재 선택된 탭 타입의 장비중 강화할 장비가 있는지 여부
            bool hasAdvance = _equipmentService.HasAdvanceableEquipment(_currentType);

            if (_advanceButton != null)
            {
                _advanceButton.interactable = hasAdvance;
            }

            if (_advanceButtonDisabled != null)
            {
                _advanceButtonDisabled.SetActive(!hasAdvance);
            }
        }

        public void OnClickAdvanceAll()
        {
            // 현재 선택된 탭 타입의 장비만 일괄 강화
            var advanceResult = _equipmentService.AdvanceAllAvailable(_currentType);

            if (advanceResult == null)
            {
                // TODO 이후 팝업 창 출력
                Debug.Log($"[UI_Equipment] {_currentType} 타입의 승급 가능한 장비가 없습니다.");
                return;
            }

            // 승급 결과 팝업 표시
            var advanceResultPopup = UIManager.Instance.ShowPopup<UI_AdvanceResult>(EPopupUIType.AdvanceResult);
            if (advanceResultPopup != null)
            {
                advanceResultPopup.SetAdvanceResult(advanceResult);
            }

            // 인벤토리 갱신
            RefreshInventoryByCurrentType();

            // 현재 탭 타입에 장착된 장비 정보 표시
            ShowEquippedEquipmentInfo();

            // 일괄합성 버튼 상태 업데이트
            UpdateAdvanceButtonState();
        }

        private void OnEquipmentInventoryChanged(EquipmentInventoryChangedEvent eventData)
        {
            // 현재 탭 타입과 일치하는 경우에만 갱신
            if (eventData.EquipmentType == _currentType)
            {
                RefreshInventoryByCurrentType();

                // 현재 탭 타입에 장착된 장비 정보 표시
                ShowEquippedEquipmentInfo();

                // 일괄합성 버튼 상태 업데이트
                UpdateAdvanceButtonState();
            }
        }

        private void OnEquipmentEquipped(EquipmentEquippedEvent eventData)
        {
            // 현재 탭 타입과 일치하는 경우에만 갱신
            if (eventData.EquipmentType == _currentType)
            {
                RefreshInventoryByCurrentType();
                ShowEquippedEquipmentInfo();
            }
        }
    }
}
