using Cysharp.Threading.Tasks;
using DG.Tweening;
using SahurRaising.Core;
using SahurRaising.UI;
using SahurRaising.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_GachaResult : UI_Popup
    {
        [Header("재화 슬롯")]
        [SerializeField] private TMP_Text _currenyAmountText;

        [Header("가챠 슬롯")]
        [SerializeField] private RectTransform _slotContainer;
        [SerializeField] private GachaSlot _gachaSlotPrefab;

        [Header("뽑기 레벨 경험치 슬라이더")]
        [SerializeField] private Slider _backProgressSlider;
        [SerializeField] private Slider _frontProgressSlider;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _progressText;

        [Header("연출 설정")]
        [SerializeField, Tooltip("각 슬롯 사이의 딜레이")]
        private float _slotAnimationDelay = 0.025f;

        [SerializeField, Tooltip("특별 슬롯 추가 딜레이 (슬로우 모션 효과)")]
        private float _specialSlotDelay = 0.5f;

        [SerializeField, Tooltip("특별 슬롯 이후 다음 슬롯까지의 추가 딜레이")]
        private float _specialSlotNextDelay = 0.2f;

        [SerializeField, Tooltip("슬라이더 애니메이션 시간")]
        private float _progressAnimationDuration = 1f;

        [Header("쉐이킹 설정")]
        [SerializeField, Tooltip("쉐이킹 지속 시간")]
        private float _shakeDuration = 0.5f;
        [SerializeField, Tooltip("쉐이킹 강도")]
        private float _shakeStrength = 20f;
        [SerializeField, Tooltip("쉐이킹 진동 횟수")]
        private int _shakeVibrato = 20;

        [Header("봅기 버튼")]
        [SerializeField] private List<GachaButton> _gachaButtons;

        [Header("클릭 블로커")]
        [SerializeField] private GameObject _clickBlocker;

        private List<GachaSlot> _slotPool = new List<GachaSlot>();
        private GachaPullEvent _currentEvent;

        private Tween _progressTween;   // 슬라이더 애니메이션 트윈
        private Tween _shakeTween;      // 쉐이킹 트윈

        private IGachaResultStrategy _currentStrategy;

        private IEventBus _eventBus;
        private IGachaService _gachaService;
        private ICurrencyService _currencyService;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            InitializeSlotPool();

            if (_clickBlocker != null)
            {
                _clickBlocker.SetActive(false);
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
                _eventBus.Subscribe<RewardGrantedEvent>(OnRewardGranted);
            }

            // 클릭 블로커 초기 상태
            if (_clickBlocker != null)
            {
                _clickBlocker.SetActive(false);
            }
        }

        public override void OnHide()
        {
            base.OnHide();

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<RewardGrantedEvent>(OnRewardGranted);
            }

            // 애니메이션 정리
            ClearActiveTweens();

            // 클릭 블로커 비활성화
            if (_clickBlocker != null)
            {
                _clickBlocker.SetActive(false);
            }

            foreach (var slot in _slotPool)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                    slot.OnReset();
                }
            }
        }

        private bool TryBindService()
        {
            if (_eventBus == null && ServiceLocator.HasService<IEventBus>())
            {
                _eventBus = ServiceLocator.Get<IEventBus>();
            }

            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            if (_currencyService == null && ServiceLocator.HasService<ICurrencyService>())
            {
                _currencyService = ServiceLocator.Get<ICurrencyService>();
            }

            return _eventBus != null && _gachaService != null && _currencyService != null;
        }
        
        private void UpdateGachaButton(GachaType type = GachaType.None)
        {
            // 각 GachaButton 업데이트
            foreach (var button in _gachaButtons)
            {
                button?.Refresh(type);
            }
        }

        private void UpdateCurrenyAmountText()
        {
            if (_currencyService == null)
                return;

            var amount = _currencyService.Get(CurrencyType.Diamond);

            if (_currenyAmountText != null)
            {
                _currenyAmountText.text = amount.ToString("F0");
                //_currenyAmountText.text = NumberFormatUtil.FormatBigDouble(amount);
            }
        }

        private void InitializeSlotPool()
        {
            if (_slotContainer == null)
            {
                Debug.LogError("[UI_GachaResult] SlotContainer가 할당되지 않았습니다.");
                return;
            }

            // 이미 하위에 있는 GachaSlot들을 먼저 할당
            var existingSlots = _slotContainer.GetComponentsInChildren<GachaSlot>(true).ToList();
            foreach (var slot in existingSlots)
            {
                slot.OnReset();
                slot.gameObject.SetActive(false);
                _slotPool.Add(slot);
            }
        }

        public void SetGachaResult(GachaPullEvent evt)
        {
            if (!TryBindService())
                return;

            _currentEvent = evt;

            // GachaService를 통해 전략 가져오기
            _currentStrategy = _gachaService.GetResultStrategy(evt.Type);

            if (_currentStrategy == null)
            {
                Debug.LogError($"[UI_GachaResult] {evt.Type}에 대한 전략을 찾을 수 없습니다.");
                return;
            }

            // 각 GachaButton 업데이트
            UpdateGachaButton(evt.Type);

            UpdateCurrenyAmountText();

            ShowGachaResultsAsync().Forget();
        }

        private async UniTask ShowGachaResultsAsync()
        {
            if (_currentEvent.Results == null || _currentEvent.Results.Count == 0)
            {
                Debug.LogWarning("[UI_GachaResult] 가챠 결과가 비어있습니다.");
                return;
            }

            if (_gachaService == null)
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_gachaService == null || !_gachaService.IsInitialized)
            {
                Debug.LogWarning("[UI_GachaResult] GachaService를 찾을 수 없거나 초기화되지 않았습니다.");
                return;
            }

            // 뽑기 전 상태 저장
            GachaType gachaType = _currentEvent.Type;

            int resultCount = _currentEvent.Results.Count;

            // 필요한 슬롯이 풀보다 많으면 추가 생성
            if (resultCount > _slotPool.Count)
            {
                int needCount = resultCount - _slotPool.Count;
                CreateAdditionalSlots(needCount);
            }

            // 슬롯 비활성화
            foreach (var slot in _slotPool)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                    slot.OnReset();
                }
            }

            // 기존 트윈 정리
            ClearActiveTweens();

            // 클릭 블로커 활성화 (애니메이션 시작)
            if (_clickBlocker != null)
            {
                _clickBlocker.SetActive(true);
            }

            // 슬라이더 초기 상태 설정
            InitializeProgressBar(gachaType);

            // 슬라이더 애니메이션 시작 (슬롯 애니메이션과 동시에)
            AnimateProgressBar(gachaType);

            // 필요한 만큼만 슬롯 활성화 및 데이터 설정
            for (int i = 0; i < resultCount && i < _slotPool.Count; i++)
            {
                var slot = _slotPool[i];
                var result = _currentEvent.Results[i];

                slot.SetData(result);
                slot.gameObject.SetActive(true);

                float delay = _slotAnimationDelay;
                if (slot.IsHighGradeNewItemSlot)
                {
                    delay += _specialSlotNextDelay;

                    await UniTask.WaitForSeconds(_specialSlotDelay);
                    StartShakeAnimation();
                }

                // 슬롯 간 딜레이만 전달하고, 나머지 연출은 슬롯이 자체 관리
                var tween = slot.ShowWithAnimation();

                if (tween != null)
                {
                    // 마지막 슬롯의 애니메이션이 끝나면 플래그 해제
                    if (i == resultCount - 1)
                    {
                        tween.OnComplete(() =>
                        {
                            if (_clickBlocker != null)
                            {
                                _clickBlocker.SetActive(false);
                            }

                            // 연출 완료 후 인벤토리에 추가
                            if (_gachaService != null)
                            {
                                _gachaService.AddResultsToInventory(_currentEvent.Type, _currentEvent.Results);
                            }
                        });
                    }
                }

                await UniTask.WaitForSeconds(delay);
            }
        }

        /// <summary>
        /// 슬롯 컨테이너 쉐이킹 애니메이션 시작
        /// </summary>
        private void StartShakeAnimation()
        {
            if (_slotContainer == null)
                return;

            // 이미 쉐이킹 중이면 중복 실행 방지
            if (_shakeTween != null && _shakeTween.IsActive())
                return;

            // 초기 위치 저장
            Vector3 originalPosition = _slotContainer.anchoredPosition;

            // 쉐이킹 애니메이션
            _shakeTween = _slotContainer.DOShakeAnchorPos(_shakeDuration, _shakeStrength, _shakeVibrato, 90f, false, true)
                .OnComplete(() =>
                {
                    // 쉐이킹 완료 후 원래 위치로 복귀
                    if (_slotContainer != null)
                    {
                        _slotContainer.anchoredPosition = originalPosition;
                        _shakeTween = null;
                    }
                });
        }

        private void InitializeProgressBar(GachaType gachaType)
        {
            if (_gachaService == null || _gachaService.LevelConfig == null)
                return;

            int beforeCount = _gachaService.GetGachaCount(gachaType) - _currentEvent.Count; // 뽑기 전 개수

            int gachaCount = _gachaService.GetGachaCount(gachaType); // 뽑기 후 개수
            int gachaLevel = _gachaService.GetGachaLevel(gachaType);

            int maxLevel = _gachaService.LevelConfig.GetMaxLevel(gachaType);

            // 레벨 텍스트 설정
            if (_levelText != null)
            {
                _levelText.text = $"Lv.{gachaLevel}";
            }

            // 최대 레벨인 경우
            if (gachaLevel >= maxLevel)
            {
                if (_backProgressSlider != null)
                    _backProgressSlider.value = 1f;
                if (_frontProgressSlider != null)
                    _frontProgressSlider.value = 1f;

                if (_progressText != null)
                    _progressText.text = "MAX";

                return;
            }

            // 최종 목표 진행도 계산
            int nextLevelRequiredCount = _gachaService.GetRequiredCountForLevel(gachaType, gachaLevel + 1);

            float finalProgress = 0f;
            if (nextLevelRequiredCount > 0)
            {
                finalProgress = Mathf.Clamp01((float)gachaCount / nextLevelRequiredCount);
            }

            // 뒤쪽 슬라이더: 최종 목표 위치로 즉시 설정
            if (_backProgressSlider != null)
            {
                _backProgressSlider.value = finalProgress;
            }

            // 앞쪽 슬라이더: 시작 위치 설정 (뽑기 전 위치)
            float startProgress = 0f;

            if (beforeCount >= 0)
            {
                // 같은 레벨 내에서 진행
                if (nextLevelRequiredCount > 0)
                {
                    startProgress = Mathf.Clamp01((float)beforeCount / nextLevelRequiredCount);
                }
            }
            else
            {
                // 레벨업이 발생한 경우, 시작은 0으로
                startProgress = 0f;
            }

            if (_frontProgressSlider != null)
            {
                _frontProgressSlider.value = startProgress;
            }

            // 최종 텍스트 바로 할당
            if (_progressText != null)
            {
                _progressText.text = $"{gachaCount}/{nextLevelRequiredCount}";
            }
        }

        private void AnimateProgressBar(GachaType gachaType)
        {
            if (_frontProgressSlider == null || _gachaService == null || _gachaService.LevelConfig == null)
                return;

            // 기존 트윈 정리
            if (_progressTween != null && _progressTween.IsActive())
            {
                _progressTween.Kill();
            }

            int afterLevel = _gachaService.GetGachaLevel(gachaType);
            int maxLevel = _gachaService.LevelConfig.GetMaxLevel(gachaType);

            // 최대 레벨인 경우
            if (afterLevel >= maxLevel)
                return;

            // 뒤쪽 슬라이더의 값(최종 목표 진행도)을 가져와서 앞쪽 슬라이더가 그 값까지 차오르도록
            float targetProgress = _backProgressSlider.value;
            float startValue = _frontProgressSlider.value;

            // 앞쪽 슬라이더가 뒤쪽 슬라이더의 값까지 차오르는 애니메이션
            _progressTween = _frontProgressSlider.DOValue(targetProgress, _progressAnimationDuration)
                .SetEase(Ease.OutQuad);
        }

        private void CreateAdditionalSlots(int count)
        {
            if (_gachaSlotPrefab == null)
            {
                Debug.LogError("[UI_GachaResult] GachaSlot 프리팹이 할당되지 않아 추가 슬롯을 생성할 수 없습니다.");
                return;
            }

            if (_slotContainer == null)
            {
                Debug.LogError("[UI_GachaResult] SlotContainer가 할당되지 않았습니다.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(_gachaSlotPrefab, _slotContainer);
                slot.OnReset();
                slot.gameObject.SetActive(false);
                _slotPool.Add(slot);
            }
        }

        private void ClearActiveTweens()
        {
            if (_progressTween != null && _progressTween.IsActive())
            {
                _progressTween.Kill();
                _progressTween = null;
            }

            // 쉐이킹 애니메이션 정리
            if (_shakeTween != null && _shakeTween.IsActive())
            {
                _shakeTween.Kill();
                _shakeTween = null;
            }

            // 쉐이킹 후 원래 위치로 복귀
            if (_slotContainer != null)
            {
                _slotContainer.DOKill();
            }
        }

        private void OnRewardGranted(RewardGrantedEvent evt)
        {
            if (evt.CurrencyType == CurrencyType.Diamond)
            {
                UpdateGachaButton();
                UpdateCurrenyAmountText();
            }
        }
    }
}