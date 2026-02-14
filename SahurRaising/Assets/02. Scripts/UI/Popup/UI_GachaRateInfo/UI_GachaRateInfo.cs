using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_GachaRateInfo : UI_Popup
    {
        [Header("UI 요소")]
        [SerializeField] private ScrollRect _scrollView;
        [SerializeField] private Transform _contentParent;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("풀링 설정")]
        [SerializeField] private GachaRatePanel _panelPrefab;

        private readonly List<GachaRatePanel> _panels = new List<GachaRatePanel>();

        private GachaType _currentGachaType;
        private int _currentLevel;
        private int _maxLevel;

        private IGachaService _gachaService;
        private IConfigService _configService;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            // 프리팹에 미리 배치된 패널들을 찾아서 리스트에 추가
            _panels.Clear();
            if (_contentParent != null)
            {
                foreach (Transform child in _contentParent)
                {
                    var panel = child.GetComponent<GachaRatePanel>();
                    if (panel != null)
                    {
                        _panels.Add(panel);
                        panel.gameObject.SetActive(false);
                    }
                }
            }

            // 버튼 이벤트 등록
            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveAllListeners();
                _prevButton.onClick.AddListener(OnClickPrevLevel);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(OnClickNextLevel);
            }

            await UniTask.Yield();
        }

        public override void OnHide()
        {
            base.OnHide();

            // 모든 패널 비활성화
            foreach (var panel in _panels)
            {
                if (panel != null)
                    panel.gameObject.SetActive(false);
            }
        }

        public void SetGachaType(GachaType gachaType)
        {
            if (!TryBindService())
                return;

            _currentGachaType = gachaType;

            // 현재 레벨 및 최대 레벨 가져오기
            _currentLevel = _gachaService.GetGachaLevel(gachaType);
            _maxLevel = _gachaService.GetMaxLevel(gachaType);

            // UI 업데이트
            RefreshUI();

            // 스크롤을 맨 위로 초기화
            ResetScrollPosition();
        }

        private bool TryBindService()
        {
            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_configService == null && ServiceLocator.HasService<IConfigService>())
                _configService = ServiceLocator.Get<IConfigService>();

            return _gachaService != null && _configService != null;
        }

        private void RefreshUI()
        {
            // 레벨 텍스트 업데이트
            UpdateLevelText();

            // 버튼 상태 업데이트
            UpdateButtonState();

            // 확률 데이터 표시
            UpdateGachaRatePanels();
        }

        private void UpdateLevelText()
        {
            if (_levelText != null)
            {
                _levelText.text = $"Lv {_currentLevel}";
            }
        }

        private void UpdateButtonState()
        {
            if (_prevButton != null)
                _prevButton.interactable = _currentLevel > 1;

            if (_nextButton != null)
                _nextButton.interactable = _currentLevel < _maxLevel;
        }

        private void UpdateGachaRatePanels()
        {
            if (_contentParent == null || _panelPrefab == null)
                return;

            // GachaService를 통해 확률 정보 가져오기
            var probabilities = _gachaService.GetProbabilitiesForLevel(_currentGachaType, _currentLevel);

            int needCount = probabilities.Count;

            // 필요한 패널 수만큼 보장 (부족하면 새로 생성하여 풀에 추가)
            while (_panels.Count < needCount)
            {
                var newPanel = Instantiate(_panelPrefab, _contentParent);
                newPanel.gameObject.SetActive(false);
                _panels.Add(newPanel);
            }

            // 패널 채우기
            for (int i = 0; i < _panels.Count; i++)
            {
                if (i < needCount)
                {
                    var panel = _panels[i];
                    var probability = probabilities[i];

                    // UI에서 필요한 값들을 미리 계산
                    Color frameColor = _configService.GetColorForGrade(_currentGachaType, probability.GradeKey);
                    string gradeText = probability.GradeKey;
                    float probabilityValue = probability.Probability;

                    panel.gameObject.SetActive(true);
                    panel.SetData(frameColor, gradeText, probabilityValue);
                }
                else
                {
                    // 남는 패널은 비활성화 (풀 안에서 대기)
                    _panels[i].gameObject.SetActive(false);
                }
            }
        }

        private void ResetScrollPosition()
        {
            if (_scrollView != null)
            {
                _scrollView.verticalNormalizedPosition = 1f;
            }
        }

        private void OnClickPrevLevel()
        {
            if (_currentLevel > 1)
            {
                _currentLevel--;
                RefreshUI();
            }
        }

        private void OnClickNextLevel()
        {
            if (_currentLevel < _maxLevel)
            {
                _currentLevel++;
                RefreshUI();
            }
        }
    }
}
