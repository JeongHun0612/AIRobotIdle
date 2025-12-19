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
        [Header("Slots (Optional)")]
        [SerializeField] private UIUpgradeSlot _slotPrefab;
        [SerializeField] private Transform _normalSlotsRoot;
        [SerializeField] private Transform _superSlotsRoot;
        [SerializeField] private Transform _ultraSlotsRoot;
        [SerializeField] private Transform _superUltraSlotsRoot;

        [Header("Slot Icons (Fallback/Lock)")]
        [SerializeField] private Sprite _fallbackIcon;
        [SerializeField] private Sprite _lockIcon;

        [Header("Data")]
        [SerializeField] private string _upgradeTableKey = nameof(UpgradeTable);
        [SerializeField] private bool _autoSelectFirstUnlocked = true;

        [Header("Selected Upgrade (Optional)")]
        [SerializeField] private string _selectedUpgradeCode = "";
        [SerializeField, Min(1)] private int _levelsPerClick = 1;

        [Header("Text (Optional)")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _costText;

        [Header("Actions (Optional)")]
        [SerializeField] private Button _upgradeButton;

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
            BindUpgradeButtonIfNeeded();
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
            EnsureTableLoadedIfNeeded().Forget();
            Refresh();
        }

        public void SetSelectedUpgradeCode(string upgradeCode)
        {
            if (string.IsNullOrEmpty(upgradeCode))
                return;

            _selectedUpgradeCode = upgradeCode;
            Refresh();
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled)
                return;

            int characterLevel = GetCharacterLevelSafe();
            RefreshCategoryLocks(characterLevel);
            RefreshSelectedUpgrade();
            RefreshSlots(characterLevel);
        }

        private void BindUpgradeButtonIfNeeded()
        {
            if (_upgradeButton == null)
                return;

            _upgradeButton.onClick.RemoveListener(HandleUpgradeButtonClicked);
            _upgradeButton.onClick.AddListener(HandleUpgradeButtonClicked);
        }

        private void HandleUpgradeButtonClicked()
        {
            if (!TryBindServicesIfNeeded())
                return;

            if (string.IsNullOrEmpty(_selectedUpgradeCode))
                return;

            if (!_upgradeService.TryUpgrade(_selectedUpgradeCode, _levelsPerClick, out var appliedLevels, out var totalCost))
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[UIUpgradePanel] Upgrade applied: Code='{_selectedUpgradeCode}', +{appliedLevels}, Cost={totalCost}");
#endif

            Refresh();
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

        private void RefreshCategoryLocks(int characterLevel)
        {
            SetCategoryLocked(_superLockedRoot, _superLockedReasonText, characterLevel, _superUnlockLevel, "슈퍼 강화");
            SetCategoryLocked(_ultraLockedRoot, _ultraLockedReasonText, characterLevel, _ultraUnlockLevel, "울트라 강화");
            SetCategoryLocked(_superUltraLockedRoot, _superUltraLockedReasonText, characterLevel, _superUltraUnlockLevel, "슈퍼울트라 강화");
        }

        private void SetCategoryLocked(GameObject lockedRoot, TMP_Text reasonText, int characterLevel, int requiredLevel, string categoryName)
        {
            if (lockedRoot == null && reasonText == null)
                return;

            bool isLocked = characterLevel < requiredLevel;

            if (lockedRoot != null)
                lockedRoot.SetActive(isLocked);

            if (reasonText != null && isLocked)
                reasonText.text = $"[{categoryName}]\n레벨 {requiredLevel}에 개방됩니다.";
        }

        private void RefreshSelectedUpgrade()
        {
            if (_upgradeTable != null && !string.IsNullOrEmpty(_selectedUpgradeCode) && _upgradeTable.Index != null && _upgradeTable.Index.TryGetValue(_selectedUpgradeCode, out var row))
            {
                if (_titleText != null)
                    _titleText.text = string.IsNullOrEmpty(row.Name) ? row.Code : row.Name;

                if (_descriptionText != null)
                    _descriptionText.text = row.Description ?? string.Empty;
            }
            else
            {
                if (_titleText != null)
                    _titleText.text = string.IsNullOrEmpty(_selectedUpgradeCode) ? "업그레이드" : _selectedUpgradeCode;

                if (_descriptionText != null)
                    _descriptionText.text = "";
            }

            if (_upgradeService == null || string.IsNullOrEmpty(_selectedUpgradeCode))
            {
                if (_levelText != null) _levelText.text = "";
                if (_costText != null) _costText.text = "";
                return;
            }

            int level = _upgradeService.GetLevel(_selectedUpgradeCode);
            BigDouble nextCost = _upgradeService.GetNextCost(_selectedUpgradeCode);

            if (_levelText != null)
                _levelText.text = $"LV {level}";

            if (_costText != null)
                _costText.text = nextCost > BigDouble.Zero ? nextCost.ToString() : "MAX";
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

                if (_autoSelectFirstUnlocked && string.IsNullOrEmpty(_selectedUpgradeCode))
                {
                    int characterLevel = GetCharacterLevelSafe();
                    string first = FindFirstUnlockedUpgradeCode(characterLevel);
                    if (!string.IsNullOrEmpty(first))
                        _selectedUpgradeCode = first;
                }

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
                if (parent == null)
                    continue;

                var slot = Instantiate(_slotPrefab, parent);
                slot.Initialize(row, SetSelectedUpgradeCode);
                if (_lockIcon != null)
                    slot.SetLockIcon(_lockIcon);

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

        private void RefreshSlots(int characterLevel)
        {
            if (_upgradeService == null || _slots.Count == 0)
                return;

            foreach (var slot in _slots)
            {
                if (slot == null)
                    continue;

                bool isLocked = IsTierLocked(slot.Tier, characterLevel);
                string reason = GetTierLockReason(slot.Tier, characterLevel);
                int level = _upgradeService.GetLevel(slot.Code);

                slot.SetSelected(!string.IsNullOrEmpty(_selectedUpgradeCode) && slot.Code == _selectedUpgradeCode);
                slot.Refresh(level, isLocked, reason, _fallbackIcon);
            }
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

        private string FindFirstUnlockedUpgradeCode(int characterLevel)
        {
            if (_upgradeTable == null)
                return string.Empty;

            foreach (var row in _upgradeTable.Rows)
            {
                if (!IsTierLocked(row.Tier, characterLevel))
                    return row.Code;
            }

            return _upgradeTable.Rows.Count > 0 ? _upgradeTable.Rows[0].Code : string.Empty;
        }
    }
}
