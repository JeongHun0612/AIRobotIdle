using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    /// <summary>
    /// 아이템 슬롯의 공통 기능을 제공하는 추상 부모 클래스
    /// </summary>
    public abstract class ItemSlotBase<TData, TInventoryInfo, IService> : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] protected Image _bgImage;
        [SerializeField] protected Image _iconImage;
        [SerializeField] protected TMP_Text _rankText;
        [SerializeField] protected TMP_Text _levelText;

        [Header("강화 진행도 UI")]
        [SerializeField] protected Slider _progressSlider;
        [SerializeField] protected TMP_Text _progressText;
        [SerializeField] protected Image _upgradeArrowIcon;

        protected IConfigService _configService;
        protected IService _service;
        protected TData _data;

        public TData Data => _data;

        public virtual void Initialize()
        {
        }

        public virtual void SetData(TData data)
        {
            _data = data;
            UpdateUI();
        }

        protected virtual void UpdateUI()
        {
            if (!IsValidData())
                return;

            if (!TryBindService())
                return;

            var info = GetInventoryInfo();

            string gradeString = GetGradeString();
            string itemName = GetItemName();
            GachaType gachaType = GetGachaType();

            // 배경 색깔 설정
            UpdateBackgroundColor(gachaType, gradeString);

            // 아이콘 설정
            UpdateIcon();

            // 랭크 텍스트 설정
            UpdateRankText(gradeString);

            // 레벨 텍스트 설정
            UpdateLevelText(info);

            // 잼 아이콘 업데이트 (Equipment만 사용)
            UpdateGemIcons(gradeString);

            // 아이템 이름 설정
            UpdateItemName(itemName);

            // 프로그래스바 UI 업데이트
            int ownedCount = GetCount(info);
            int requiredCount = GetRequiredCountForAdvance();
            UpdateProgressUI(ownedCount, requiredCount);
        }

        protected virtual bool IsValidData()
        {
            return _data != null && !string.IsNullOrEmpty(GetItemID());
        }

        protected virtual bool TryBindService()
        {
            if (_configService == null && ServiceLocator.HasService<IConfigService>())
            {
                _configService = ServiceLocator.Get<IConfigService>();
            }

            if (_service == null && ServiceLocator.HasService<IService>())
            {
                _service = ServiceLocator.Get<IService>();
            }

            return _configService != null && _service != null;
        }

        protected virtual void UpdateBackgroundColor(GachaType gachaType, string gradeString)
        {
            if (_bgImage != null)
            {
                _bgImage.color = _configService.GetColorForGrade(gachaType, gradeString);
            }
        }

        protected virtual void UpdateIcon()
        {
            if (_iconImage != null)
            {
                var icon = GetIcon();
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }
        }

        protected virtual void UpdateRankText(string gradeString)
        {
            if (_rankText != null)
            {
                _rankText.text = GetRankText(gradeString);
            }
        }

        protected virtual void UpdateLevelText(TInventoryInfo info)
        {
            if (_levelText != null)
            {
                int level = GetLevel(info);
                _levelText.text = $"Lv. {level}";
            }
        }

        protected virtual void UpdateGemIcons(string gradeString)
        {
        }

        protected virtual void UpdateItemName(string itemName)
        {
        }

        protected void UpdateProgressUI(int ownedCount, int requiredCount)
        {
            if (_progressText != null)
            {
                _progressText.text = $"{ownedCount}/{requiredCount}";
            }

            if (_progressSlider != null)
            {
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = requiredCount;
                _progressSlider.value = Mathf.Clamp(ownedCount, 0, requiredCount);
            }

            // 업그레이드 가능 여부 확인 및 화살표 아이콘 업데이트
            if (_upgradeArrowIcon != null)
            {
                bool canUpgrade = ownedCount >= requiredCount;
                _upgradeArrowIcon.gameObject.SetActive(canUpgrade);
            }
        }

        protected abstract string GetItemID();
        protected abstract string GetItemName();
        protected abstract Sprite GetIcon();
        protected abstract string GetGradeString();
        protected abstract GachaType GetGachaType();
        protected abstract string GetRankText(string gradeString);
        protected abstract TInventoryInfo GetInventoryInfo();
        protected abstract int GetLevel(TInventoryInfo info);
        protected abstract int GetCount(TInventoryInfo info);
        protected abstract int GetRequiredCountForAdvance();
    }
}
