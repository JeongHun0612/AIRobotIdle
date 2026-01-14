using SahurRaising.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaPanel : MonoBehaviour
    {
        [SerializeField] private GachaType _gachaType;

        [Header("뽑기 레벨 UI")]
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TMP_Text _progressText;

        [Header("봅기 버튼")]
        [SerializeField] private List<GachaButton> _gachaButtons;


        private IGachaService _gachaService;

        public GachaType GachaType => _gachaType;

        public void Initialize()
        {            
            // 각 GachaButton 초기화
            foreach (var button in _gachaButtons)
            {
                button?.Initialize();
            }
        }

        public void Refresh()
        {
            if (_gachaService == null)
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_gachaService == null || !_gachaService.IsInitialized)
                return;

            // 레벨 정보 가져오기
            int currentLevel = _gachaService.GetGachaLevel(_gachaType);
            int totalCount = _gachaService.GetGachaCount(_gachaType);

            // 레벨 텍스트 업데이트
            if (_levelText != null)
            {
                _levelText.text = $"Lv.{currentLevel}";
            }

            // 프로그래스바 업데이트
            UpdateProgressBar(currentLevel, totalCount);

            // 각 GachaButton 업데이트
            foreach (var button in _gachaButtons)
            {
                button?.Refresh(_gachaType);
            }
        }

        private void UpdateProgressBar(int currentLevel, int totalCount)
        {
            // 다음 레벨의 필요 누적 개수 가져오기
            int nextLevelRequired = _gachaService.GetRequiredCountForNextLevel(_gachaType);

            // 최대 레벨인 경우
            int maxLevel = _gachaService.LevelConfig.GetMaxLevel(_gachaType);
            if (currentLevel >= maxLevel)
            {
                if (_progressSlider != null)
                    _progressSlider.value = 1f;

                if (_progressText != null)
                    _progressText.text = "MAX";

                return;
            }

            // 프로그래스 계산: 현재 누적 개수 / 다음 레벨 필요 누적 개수
            float progress = Mathf.Clamp01((float)totalCount / nextLevelRequired);

            if (_progressSlider != null)
                _progressSlider.value = progress;

            if (_progressText != null)
                _progressText.text = $"{totalCount}/{nextLevelRequired}";
        }
    }
}
