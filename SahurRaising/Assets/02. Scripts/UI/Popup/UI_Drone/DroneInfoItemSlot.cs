using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class DroneInfoItemSlot : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _levelText;

        [Header("강화 진행도 UI")]
        [SerializeField] private Slider _progressSlider;   // 슬라이더 바
        [SerializeField] private TMP_Text _progressText;   // "보유/필요" 텍스트

        private IDroneService _droneService;
        private IConfigService _configService;

        private DroneRow _data;

        public DroneRow Data => _data;

        public void Initialize()
        {
        }

        public void SetData(DroneRow data)
        {
            _data = data;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(_data.ID))
                return;

            if (_configService == null)
                _configService = ServiceLocator.Get<IConfigService>();

            if (_droneService == null)
                _droneService = ServiceLocator.Get<IDroneService>();

            if (_configService == null || _configService.ItemVisualConfig == null)
            {
                Debug.LogWarning("[ItemSlot] ItemVisualConfig를 찾을 수 없습니다.");
                return;
            }

            if (_droneService == null || !_droneService.IsInitialized)
            {
                Debug.LogWarning("[ItemSlot] DroneService를 찾을 수 없거나 초기화되지 않았습니다.");
                return;
            }

            // 인벤토리 정보 조회
            var info = _droneService.GetInventoryInfo(_data.ID);

            string gradeString = _data.ID;

            // 배경 색깔 설정
            if (_bgImage != null)
            {
                _bgImage.color = _configService.GetColorForGrade(GachaType.Drone, gradeString);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                var icon = _data.Icon;
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }

            // 랭크 텍스트 설정
            if (_rankText != null)
            {
                _rankText.text = gradeString;
            }

            // 레벨 텍스트 설정
            if (_levelText != null)
            {
                _levelText.text = $"Lv. {info.Level}";
            }

            // 프로그래스바 UI 업데이트
            int ownedCount = info.Count;
            int requiredCount = _droneService.GetRequiredCountForAdvance();
            UpdateProgressUI(ownedCount, requiredCount);
        }

        private void UpdateProgressUI(int ownedCount, int requiredCount)
        {
            if (_progressText != null)
            {
                _progressText.text = $"{ownedCount}/{requiredCount}";
            }

            if (_progressSlider != null)
            {
                // 슬라이더가 0~필요개수 기준으로 채워지도록 설정
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = requiredCount;
                _progressSlider.value = Mathf.Clamp(ownedCount, 0, requiredCount);
            }
        }
    }
}
