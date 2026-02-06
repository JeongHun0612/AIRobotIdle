using System.Collections.Generic;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    /// <summary>
    /// 메인 씬에 상시 표시되는 업그레이드 패널.
    /// - 슬롯(아이콘/레벨)은 UpgradeTable(SO)에서 조회
    /// - 잠금 조건(티어)은 캐릭터 레벨 기준
    /// </summary>
    public class UIUpgradePanel : UI_Base
    {
        [Header("Slots Roots")]
        [SerializeField] private UIUpgradeSlot _slotPrefab;
        [SerializeField] private Transform _normalSlotsRoot;
        [SerializeField] private Transform _superSlotsRoot;
        [SerializeField] private Transform _ultraSlotsRoot;
        [SerializeField] private Transform _superUltraSlotsRoot;

        [Header("Assets")]
        [SerializeField] private Sprite _fallbackIcon;
        [SerializeField] private Sprite _lockIcon;

        [Header("Settings")]
        [SerializeField] private string _upgradeTableKey = nameof(UpgradeTable);
        [SerializeField] private int _levelsPerClick = 1;

        [Header("Category Locks (Optional)")]
        [SerializeField] private GameObject _superLockedRoot;
        [SerializeField] private TMP_Text _superLockedReasonText;
        [SerializeField] private GameObject _ultraLockedRoot;
        [SerializeField] private TMP_Text _ultraLockedReasonText;
        [SerializeField] private GameObject _superUltraLockedRoot;
        [SerializeField] private TMP_Text _superUltraLockedReasonText;

        [Header("Unlock Thresholds")]
        [SerializeField] private int _superUnlockLevel = 5000;
        [SerializeField] private int _ultraUnlockLevel = 15000;
        [SerializeField] private int _superUltraUnlockLevel = 30000;

        private IUpgradeService _upgradeService;
        private IStatService _statService;
        private IResourceService _resourceService;

        private UpgradeTable _upgradeTable;
        private bool _isTableLoading;
        private readonly List<UIUpgradeSlot> _slots = new();

        public override void Initialize()
        {
            base.Initialize();
        }

        public override async UniTask InitializeAsync()
        {
            Initialize();
            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();
            TryBindServicesIfNeeded();
            
            if (_upgradeService != null)
            {
                _upgradeService.OnUpgradeChanged -= Refresh;
                _upgradeService.OnUpgradeChanged += Refresh;
            }

            EnsureTableLoadedIfNeeded().Forget();
            Refresh();
        }

        public override void OnHide()
        {
            base.OnHide();
            if (_upgradeService != null)
            {
                _upgradeService.OnUpgradeChanged -= Refresh;
            }
        }

        private void OnDestroy()
        {
            if (_upgradeService != null)
            {
                _upgradeService.OnUpgradeChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled) return;

            int characterLevel = GetCharacterLevelSafe();
            RefreshCategoryLocks(characterLevel);
            RefreshSlots(characterLevel);
        }

        private void HandleSlotUpgrade(string code)
        {
            if (!TryBindServicesIfNeeded()) return;

            if (_upgradeService.TryUpgrade(code, _levelsPerClick, out var applied, out var cost))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[UIUpgradePanel] Upgrade applied: Code='{code}', +{applied}, Cost={cost}");
#endif
                Refresh();
            }
        }

        private void RefreshSlots(int characterLevel)
        {
            if (_upgradeService == null || _slots.Count == 0) return;

            foreach (var slot in _slots)
            {
                if (slot == null) continue;

                bool isLocked = IsTierLocked(slot.Tier, characterLevel);
                string reason = GetTierLockReason(slot.Tier, characterLevel);

                int level = _upgradeService.GetLevel(slot.Code);
                BigDouble cost = _upgradeService.GetNextCost(slot.Code);

                // 현재 값과 다음 레벨 값 계산
                BigDouble currentVal = _statService != null ? _statService.GetStatValue(slot.Code, level) : 0;
                BigDouble nextVal = _statService != null ? _statService.GetStatValue(slot.Code, level + _levelsPerClick) : 0;

                // UpgradeService를 통해 구매 가능 여부 확인
                bool hasEnoughCurrency = _upgradeService.CanAfford(slot.Code);

                slot.Refresh(level, currentVal, nextVal, cost, isLocked, reason, _fallbackIcon, hasEnoughCurrency);
            }
        }

        private async UniTaskVoid EnsureTableLoadedIfNeeded()
        {
            if (_upgradeTable != null || _isTableLoading)
                return;

            if (!TryBindServicesIfNeeded())
                return;

            if (_resourceService == null)
                return;

            _isTableLoading = true;
            try
            {
                _upgradeTable = await _resourceService.LoadTableAsync<UpgradeTable>(_upgradeTableKey);
                BuildSlotsFromTable();
                Refresh();
            }
            finally
            {
                _isTableLoading = false;
            }
        }

        private void BuildSlotsFromTable()
        {
            if (_slotPrefab == null || _upgradeTable == null)
                return;

            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                var slot = _slots[i];
                if (slot == null) continue;

                if (Application.isPlaying)
                    Destroy(slot.gameObject);
                else
                    DestroyImmediate(slot.gameObject);
            }
            _slots.Clear();

            foreach (var row in _upgradeTable.Rows)
            {
                Transform parent = GetTierRoot(row.Tier);
                if (parent == null) continue;

                var slot = Instantiate(_slotPrefab, parent);
                slot.Initialize(row, HandleSlotUpgrade);


                if (_lockIcon != null)
                { }

                _slots.Add(slot);
            }
        }

        private Transform GetTierRoot(UpgradeTier tier)
        {
            return tier switch
            {
                UpgradeTier.Normal => _normalSlotsRoot,
                UpgradeTier.Super => _superSlotsRoot,
                UpgradeTier.Ultra => _ultraSlotsRoot,
                UpgradeTier.SuperUltra => _superUltraSlotsRoot,
                _ => _normalSlotsRoot
            };
        }

        private bool IsTierLocked(UpgradeTier tier, int characterLevel)
        {
            return tier switch
            {
                UpgradeTier.Super => characterLevel < _superUnlockLevel,
                UpgradeTier.Ultra => characterLevel < _ultraUnlockLevel,
                UpgradeTier.SuperUltra => characterLevel < _superUltraUnlockLevel,
                _ => false
            };
        }

        private string GetTierLockReason(UpgradeTier tier, int characterLevel)
        {
            if (!IsTierLocked(tier, characterLevel))
                return string.Empty;

            return tier switch
            {
                UpgradeTier.Super => $"레벨 {_superUnlockLevel}에 개방됩니다.",
                UpgradeTier.Ultra => $"레벨 {_ultraUnlockLevel}에 개방됩니다.",
                UpgradeTier.SuperUltra => $"레벨 {_superUltraUnlockLevel}에 개방됩니다.",
                _ => string.Empty
            };
        }

        private void RefreshCategoryLocks(int characterLevel)
        {
            // Normal is always open
            if (_normalSlotsRoot != null) _normalSlotsRoot.gameObject.SetActive(true);

            UpdateTierVisibility(_superSlotsRoot, _superLockedRoot, _superLockedReasonText, characterLevel, _superUnlockLevel, "슈퍼 강화");
            UpdateTierVisibility(_ultraSlotsRoot, _ultraLockedRoot, _ultraLockedReasonText, characterLevel, _ultraUnlockLevel, "울트라 강화");
            UpdateTierVisibility(_superUltraSlotsRoot, _superUltraLockedRoot, _superUltraLockedReasonText, characterLevel, _superUltraUnlockLevel, "슈퍼울트라 강화");
        }

        private void UpdateTierVisibility(Transform slotsRoot, GameObject lockedRoot, TMP_Text reasonText, int characterLevel, int requiredLevel, string categoryName)
        {
            bool isLocked = characterLevel < requiredLevel;

            // 슬롯 목록 표시 여부: 잠금 해제되었을 때만 표시
            if (slotsRoot != null)
                slotsRoot.gameObject.SetActive(!isLocked);

            // 잠금 패널 표시 여부: 잠겨있을 때만 표시
            if (lockedRoot != null)
                lockedRoot.SetActive(isLocked);

            if (reasonText != null && isLocked)
                reasonText.text = $"[{categoryName}]\n레벨 {requiredLevel}에 개방됩니다.";
        }

        private bool TryBindServicesIfNeeded()
        {
            if (_upgradeService == null && ServiceLocator.HasService<IUpgradeService>())
                _upgradeService = ServiceLocator.Get<IUpgradeService>();

            if (_statService == null && ServiceLocator.HasService<IStatService>())
                _statService = ServiceLocator.Get<IStatService>();

            if (_resourceService == null && ServiceLocator.HasService<IResourceService>())
                _resourceService = ServiceLocator.Get<IResourceService>();

            return _upgradeService != null;
        }

        private int GetCharacterLevelSafe()
        {
            if (_statService == null)
                return 0;

            return _statService.GetSnapshot().CharacterLevel;
        }
    }
}
