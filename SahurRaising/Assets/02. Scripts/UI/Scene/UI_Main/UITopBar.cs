using System;
using System.Collections.Generic;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    public class UITopBar : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform _layoutRoot; // 레이아웃 그룹이 있는 부모 객체 (시작점)
        [SerializeField] private List<UICurrencySlot> _predefinedSlots = new();

        [Header("Layer (Keep clickable over popups)")]
        [SerializeField] private bool _overrideSorting = true;
        [SerializeField] private int _sortingOrder = 10000;

        private ICurrencyService _currencyService;
        private IEventBus _eventBus;

        // 현재 활성화된 재화 타입들 (이벤트 수신 시 갱신용)
        private HashSet<CurrencyType> _activeCurrencyTypes = new();
        // 현재 매핑된 슬롯들 (재화 타입 -> 슬롯 인스턴스)
        private Dictionary<CurrencyType, UICurrencySlot> _activeSlotMap = new();

        private bool _isInitialized = false;

        private void Awake()
        {
            EnsureTopLayerIfNeeded();
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeEvents();
                UpdateDisplay();
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void EnsureTopLayerIfNeeded()
        {
            if (!_overrideSorting)
                return;

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = _sortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // Wait for required services
            await UniTask.WaitUntil(() =>
                ServiceLocator.HasService<ICurrencyService>() &&
                ServiceLocator.HasService<IEventBus>());

            _currencyService = ServiceLocator.Get<ICurrencyService>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            if (_currencyService == null || _eventBus == null)
            {
                Debug.LogError("[UITopBar] Failed to get services.");
                return;
            }

            _isInitialized = true;

            // If active, subscribe and update immediately
            if (gameObject.activeInHierarchy)
            {
                SubscribeEvents();
                UpdateDisplay();
            }
        }

        private void SubscribeEvents()
        {
            if (_eventBus != null)
            {
                _eventBus.Subscribe<RewardGrantedEvent>(OnRewardGranted);
                _eventBus.Subscribe<CurrencyConsumedEvent>(OnCurrencyConsumed);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PopupShown += OnPopupShown;
                UIManager.Instance.PopupHidden += OnPopupHidden;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<RewardGrantedEvent>(OnRewardGranted);
                _eventBus.Unsubscribe<CurrencyConsumedEvent>(OnCurrencyConsumed);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PopupShown -= OnPopupShown;
                UIManager.Instance.PopupHidden -= OnPopupHidden;
            }
        }

        private void UpdateDisplay()
        {
            UpdateDisplayForPopup(UIManager.Instance != null ? UIManager.Instance.GetCurrentPopupType() : EPopupUIType.None);
        }

        private void OnPopupShown(EPopupUIType popupType)
        {
            UpdateDisplayForPopup(popupType);
        }

        private void OnPopupHidden(EPopupUIType popupType)
        {
            if (UIManager.Instance != null)
            {
                var current = UIManager.Instance.GetCurrentPopupType();
                UpdateDisplayForPopup(current);
            }
        }

        private void UpdateDisplayForPopup(EPopupUIType popupType)
        {
            var targetCurrencies = GetCurrenciesForPopup(popupType);
            RefreshSlots(targetCurrencies);
        }

        [Serializable]
        public struct PopupCurrencyConfig
        {
            public EPopupUIType PopupType;
            public List<CurrencyType> Currencies;
        }

        [Header("Settings")]
        [SerializeField] private List<CurrencyType> _defaultCurrencies = new() { CurrencyType.Gold, CurrencyType.Emerald, CurrencyType.Diamond };
        [SerializeField] private List<PopupCurrencyConfig> _popupCurrencyConfigs = new();

        private List<CurrencyType> GetCurrenciesForPopup(EPopupUIType popupType)
        {
            // Inspector에서 설정된 팝업별 재화 목록 검색
            foreach (var config in _popupCurrencyConfigs)
            {
                if (config.PopupType == popupType)
                {
                    return config.Currencies;
                }
            }

            // 설정이 없으면 기본값 반환
            return _defaultCurrencies;
        }

        private void RefreshSlots(List<CurrencyType> types)
        {
            _activeCurrencyTypes.Clear();
            _activeSlotMap.Clear();

            // 미리 배치된 슬롯들을 순회하며 설정
            for (int i = 0; i < _predefinedSlots.Count; i++)
            {
                var slot = _predefinedSlots[i];
                if (slot == null) continue;

                if (i < types.Count)
                {
                    // 표시할 재화가 있는 경우: 활성화 및 데이터 설정
                    var type = types[i];
                    var icon = GetIcon(type);
                    var amount = _currencyService.Get(type);

                    slot.Initialize(type, icon, amount);

                    _activeCurrencyTypes.Add(type);
                    _activeSlotMap[type] = slot;
                }
                else
                {
                    // 표시할 재화가 없는 경우: 비활성화
                    slot.gameObject.SetActive(false);
                }
            }

            // 레이아웃 강제 갱신 (비활성화된 객체가 레이아웃에서 즉시 제외되도록)
            if (_layoutRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRoot);
            }
        }

        private Sprite GetIcon(CurrencyType type)
        {
            if (_currencyService != null)
            {
                var data = _currencyService.GetCurrencyData(type);
                return data.Icon;
            }
            return null;
        }

        private void OnRewardGranted(RewardGrantedEvent evt)
        {
            if (_activeCurrencyTypes.Contains(evt.CurrencyType))
            {
                if (_activeSlotMap.TryGetValue(evt.CurrencyType, out var slot))
                {
                    slot.Refresh(_currencyService.Get(evt.CurrencyType));
                }
            }
        }

        private void OnCurrencyConsumed(CurrencyConsumedEvent evt)
        {
            if (_activeCurrencyTypes.Contains(evt.CurrencyType))
            {
                if (_activeSlotMap.TryGetValue(evt.CurrencyType, out var slot))
                {
                    slot.Refresh(_currencyService.Get(evt.CurrencyType));
                }
            }
        }
    }
}
