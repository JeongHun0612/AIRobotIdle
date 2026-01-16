using Cysharp.Threading.Tasks;
using DG.Tweening;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_Drone : UI_Popup
    {
        [Header("인벤토리 슬롯")]
        [SerializeField] private Transform _inventoryContentRoot;          // Content
        [SerializeField] private DroneItemSlot _itemSlotPrefab;            // ItemSlot 프리팹
        [SerializeField] private ScrollRect _scrollRect;

        [Header("드론 정보 영역")]
        [SerializeField] private DroneInfo _droneInfo;

        private readonly List<DroneItemSlot> _itemSlots = new();

        private DroneItemSlot _selectedSlot;

        private IDroneService _droneService;

        public async override UniTask InitializeAsync()
        {
            // 서비스 바인딩 시도 (실패 시 무시하고 진행)
            TryBindService();

            // 아이템 슬롯 초기화
            _itemSlots.Clear();
            if (_inventoryContentRoot != null)
            {
                foreach (Transform child in _inventoryContentRoot)
                {
                    var slot = child.GetComponent<DroneItemSlot>();
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
            _droneInfo.Initialize(RefreshInventory);

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            // 인벤토리 갱신
            RefreshInventory();

            // 장착된 드론 정보 표시
            ShowEquippedDroneInfo();
        }

        private bool TryBindService()
        {
            if (_droneService == null && ServiceLocator.HasService<IDroneService>())
            {
                _droneService = ServiceLocator.Get<IDroneService>();
            }

            return _droneService != null;
        }

        private void RefreshInventory()
        {
            if (_droneService == null || _inventoryContentRoot == null || _itemSlotPrefab == null)
                return;

            // 드론 리스트 가져오기
            IReadOnlyList<DroneRow> droneList = _droneService.GetAll();

            int needCount = droneList.Count;

            // 필요한 슬롯 수만큼 보장 (부족하면 새로 생성하여 풀에 추가)
            while (_itemSlots.Count < needCount)
            {
                DroneItemSlot newSlot = Instantiate(_itemSlotPrefab, _inventoryContentRoot);
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
                    var data = droneList[i];
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

        private void ShowEquippedDroneInfo()
        {
            if (_droneService == null || _droneInfo == null)
                return;

            // 현재 장착된 드론 ID가져오기
            string equippedID = _droneService.GetEquippedID();

            if (string.IsNullOrEmpty(equippedID))
            {
                // 장착된 드론이 없으면 드론 정보 숨기기
                _droneInfo.HideEquipmentInfo();
                return;
            }

            // 드론 데이터 가져오기
            if (!_droneService.TryGetByID(equippedID, out DroneRow equippedData))
                return;

            // EquipmentInfo에 표시
            _droneInfo.RefreshDroneInfo(equippedData);

            // 해당 ItemSlot 찾아서 선택 상태로 표시
            DroneItemSlot equippedSlot = FindItemSlotByCode(equippedID);
            if (equippedSlot != null)
            {
                SetSelectedSlot(equippedSlot);
                ScrollToItemSlot(equippedSlot);
            }
        }

        private DroneItemSlot FindItemSlotByCode(string ID)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot != null && slot.Data.ID == ID)
                {
                    return slot;
                }
            }
            return null;
        }

        private void ScrollToItemSlot(DroneItemSlot itemSlot)
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

        private void SetSelectedSlot(DroneItemSlot itemSlot)
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

        public void OnClickItemSlot(DroneItemSlot itemSlot)
        {
            // 이미 선택된 슬롯을 다시 클릭한 경우 return
            if (_selectedSlot != null && ReferenceEquals(_selectedSlot, itemSlot))
                return;

            SetSelectedSlot(itemSlot);

            if (_droneInfo != null && itemSlot.Data.ID != null)
            {
                itemSlot.HideNewIfActive();
                _droneInfo.RefreshDroneInfo(itemSlot.Data);
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
                shopPopup.OnClickTabButton(ShopType.Gacha);
            }
        }

        public void OnClickAdvanceAll()
        {
            // 현재 선택된 탭 타입의 장비만 일괄 강화
            var advanceResult = _droneService.AdvanceAllAvailable();

            if (advanceResult == null || advanceResult.Count <= 0)
            {
                // TODO 이후 팝업 창 출력
                Debug.Log($"[UI_Drone] 승급 가능한 드론이 없습니다.");
                return;
            }

            // 승급 결과 팝업 표시
            var advanceResultPopup = UIManager.Instance.ShowPopup<UI_AdvanceResult>(EPopupUIType.AdvanceResult);
            if (advanceResultPopup != null)
            {
                advanceResultPopup.SetAdvanceResult(advanceResult);
            }

            // 인벤토리 갱신
            RefreshInventory();

            // 장착된 드론 정보 표시
            ShowEquippedDroneInfo();
        }
    }
}
