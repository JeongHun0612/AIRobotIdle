using Cysharp.Threading.Tasks;
using DG.Tweening;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_GachaResult : UI_Popup
    {
        [Header("가챠 슬롯")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GachaSlot _gachaSlotPrefab;

        [Header("뽑기 레벨 경험치 슬라이더")]
        [SerializeField] private Slider _backProgressSlider;
        [SerializeField] private Slider _frontProgressSlider;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _progressText;

        [Header("연출 설정")]
        [SerializeField] private float _slotAnimationDuration = 0.3f;
        [SerializeField] private float _slotAnimationDelay = 0.1f; // 각 슬롯 사이의 딜레이
        [SerializeField] private float _progressAnimationDuration = 1.5f; // 슬라이더 애니메이션 시간

        [Header("봅기 버튼")]
        [SerializeField] private List<GachaButton> _gachaButtons;

        [Header("클릭 감지")]
        [SerializeField] private Button _skipButton;

        private List<GachaSlot> _slotPool = new List<GachaSlot>();
        private GachaPullEvent _currentEvent;

        private bool _isAnimating = false;
        private List<Tween> _activeTweens = new List<Tween>(); // GachaSlot 실행 중인 트윈 추적
        private Tween _progressTween; // 슬라이더 애니메이션 트윈

        private IGachaService _gachaService;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            InitializeSlotPool();

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(OnClickSkipAnimation);
            }

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;

            _isAnimating = false;

            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(false);
            }
        }

        public override void OnHide()
        {
            base.OnHide();

            // 애니메이션 중이면 정리
            SkipAnimation();

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
            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            return _gachaService != null;
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
            _currentEvent = evt;

            // 각 GachaButton 업데이트
            foreach (var button in _gachaButtons)
            {
                button?.Refresh(evt.Type);
            }

            ShowGachaResults();
        }

        private void ShowGachaResults()
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
            _isAnimating = true;
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(true);
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

                // 순차적으로 애니메이션 실행
                float delay = i * _slotAnimationDelay;
                var tween = slot.ShowWithAnimation(delay, _slotAnimationDuration);

                if (tween != null)
                {
                    _activeTweens.Add(tween);

                    // 마지막 슬롯의 애니메이션이 끝나면 플래그 해제
                    if (i == resultCount - 1)
                    {
                        tween.OnComplete(() =>
                        {
                            _isAnimating = false;
                            if (_skipButton != null)
                            {
                                _skipButton.gameObject.SetActive(false);
                            }
                        });
                    }
                }
            }
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

        private void SkipAnimation()
        {
            if (!_isAnimating)
                return;

            // 모든 활성 트윈 즉시 완료
            foreach (var tween in _activeTweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Complete();
                }
            }
            _activeTweens.Clear();

            // 슬라이더 애니메이션 즉시 완료
            if (_progressTween != null && _progressTween.IsActive())
            {
                _progressTween.Complete();
                _progressTween = null;
            }

            // 최종 상태로 슬라이더 업데이트
            if (_currentEvent.Results != null && _currentEvent.Results.Count > 0 && _gachaService != null)
            {
                // 앞쪽 슬라이더를 최종 값으로 즉시 설정
                if (_frontProgressSlider != null && _backProgressSlider != null)
                {
                    _frontProgressSlider.value = _backProgressSlider.value;
                }
            }

            // 모든 활성화된 슬롯을 최종 상태로 설정
            foreach (var slot in _slotPool)
            {
                if (slot != null && slot.gameObject.activeSelf)
                {
                    slot.SetFinalState();
                }
            }

            _isAnimating = false;
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(false);
            }
        }

        private void ClearActiveTweens()
        {
            foreach (var tween in _activeTweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }
            }
            _activeTweens.Clear();

            if (_progressTween != null && _progressTween.IsActive())
            {
                _progressTween.Kill();
                _progressTween = null;
            }
        }

        private void OnClickSkipAnimation()
        {
            if (!_isAnimating)
                return;

            SkipAnimation();
        }

        public override void OnClickBack()
        {
            if (_isAnimating)
                return;

            base.OnClickBack();
        }
    }
}