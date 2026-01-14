using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_AdvanceResult : UI_Popup
    {
        [Header("강화 결과 정보")]
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _gradeText;
        [SerializeField] private TMP_Text _countText;

        private AdvanceResult? _advanceResult;

        private IConfigService _configService;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            await UniTask.Yield();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!TryBindService())
                return;
        }

        private bool TryBindService()
        {
            if (_configService == null && ServiceLocator.HasService<IConfigService>())
            {
                _configService = ServiceLocator.Get<IConfigService>();
            }

            return _configService != null;
        }

        public void SetAdvanceResult(AdvanceResult? result)
        {
            _advanceResult = result;

            if (result == null)
            {
                Debug.LogWarning("[UI_AdvanceResult] 강화 결과가 null입니다.");
                return;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (!_advanceResult.HasValue)
                return;

            var result = _advanceResult.Value;

            if (string.IsNullOrEmpty(result.ItemCode))
                return;

            if (_configService == null)
                _configService = ServiceLocator.Get<IConfigService>();

            if (_configService == null || _configService.ItemVisualConfig == null)
            {
                Debug.LogWarning("[UI_AdvanceResult] ItemVisualConfig를 찾을 수 없습니다.");
                return;
            }

            // 배경 색상 설정 (등급에 따라)
            if (_bgImage != null)
            {
                _bgImage.color = _configService.GetColorForGrade(result.Type, result.GradeKey);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                var icon = result.Icon;
                _iconImage.sprite = icon;
                _iconImage.color = (icon == null) ? Color.clear : Color.white;
            }

            // 등급 텍스트 설정
            if (_gradeText != null)
            {
                _gradeText.text = result.GradeKey;
            }

            // 수량 텍스트 설정
            if (_countText != null)
            {
                _countText.text = result.Count.ToString();
            }
        }

        public void OnClickBack()
        {
            UIManager.Instance.CloseCurrentPopup();
        }
    }
}
