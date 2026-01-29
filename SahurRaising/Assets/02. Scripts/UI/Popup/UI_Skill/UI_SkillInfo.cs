using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    /// <summary>
    /// 스킬 정보 팝업
    /// 스킬의 상세 정보를 표시하고 학습/취소 기능 제공
    /// </summary>
    public class UI_SkillInfo : UI_Popup
    {
        [Header("정보 표시 UI")]
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _skillDescText;
        [SerializeField] private TextMeshProUGUI _skillTimeText;
        [SerializeField] private TextMeshProUGUI _skillCostText;
        [SerializeField] private Image _skillIconImage;
        [SerializeField] private Image _currencyIconImage;

        [Header("버튼")]
        [SerializeField] private Button _learnButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;

        [Header("배경 딤")]
        [SerializeField] private Button _dimButton;

        [Header("상태별 UI 그룹")]
        [SerializeField] private GameObject _costInfoGroup;  // 재화 아이콘 + 가격 영역
        [SerializeField] private GameObject _skillIconGroup; // 스킬 아이콘 표시 영역

        private SkillRow _currentSkillData;
        private System.Action<SkillRow> _onLearnConfirmed;
        private bool _isUnlocked;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();

            // 버튼 이벤트 연결
            if (_learnButton != null)
            {
                _learnButton.onClick.AddListener(OnLearnButtonClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            if (_dimButton != null)
            {
                _dimButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        /// <summary>
        /// 팝업 표시 시 UI 초기화 (이전 데이터 잔상 방지)
        /// </summary>
        public override void OnShow()
        {
            base.OnShow();
            
            // 데이터가 설정되기 전에 모든 동적 UI 요소를 초기 상태로 리셋
            ResetUIState();
        }

        /// <summary>
        /// UI 상태 초기화 (잔상 방지)
        /// </summary>
        private void ResetUIState()
        {
            // 텍스트 초기화
            if (_skillNameText != null) _skillNameText.text = "";
            if (_skillDescText != null) _skillDescText.text = "";
            if (_skillTimeText != null) _skillTimeText.text = "";
            if (_skillCostText != null) _skillCostText.text = "";
            
            // 아이콘 초기화
            if (_skillIconImage != null) _skillIconImage.sprite = null;
            
            // 모든 그룹 비활성화 (데이터 설정 시 다시 활성화됨)
            if (_costInfoGroup != null) _costInfoGroup.SetActive(false);
            if (_skillIconGroup != null) _skillIconGroup.SetActive(false);
            if (_learnButton != null) _learnButton.gameObject.SetActive(false);
            if (_skillTimeText != null) _skillTimeText.gameObject.SetActive(false);
            if (_skillCostText != null) _skillCostText.gameObject.SetActive(false);
            if (_currencyIconImage != null) _currencyIconImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// 스킬 데이터 설정
        /// </summary>
        /// <param name="skillData">표시할 스킬 데이터</param>
        /// <param name="onLearnConfirmed">학습 확인 콜백</param>
        /// <param name="isUnlocked">이미 해금된 스킬인지 여부</param>
        public void SetSkillData(SkillRow skillData, System.Action<SkillRow> onLearnConfirmed, bool isUnlocked = false)
        {
            _currentSkillData = skillData;
            _onLearnConfirmed = onLearnConfirmed;
            _isUnlocked = isUnlocked;

            UpdateUI();
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            Debug.Log($"[UI_SkillInfo] UpdateUI 호출 - 스킬: {_currentSkillData.Name}, 해금완료: {_isUnlocked}");
            
            // 스킬 이름
            if (_skillNameText != null)
            {
                _skillNameText.text = _currentSkillData.Name;
            }

            // 스킬 설명
            if (_skillDescText != null)
            {
                _skillDescText.text = string.IsNullOrEmpty(_currentSkillData.Desc) 
                    ? "스킬 설명이 없습니다." 
                    : _currentSkillData.Desc;
            }

            // 스킬 아이콘 (항상 표시)
            if (_skillIconImage != null && _currentSkillData.Icon != null)
            {
                _skillIconImage.sprite = _currentSkillData.Icon;
                _skillIconImage.gameObject.SetActive(true);
            }

            // 해금 완료 상태에 따라 UI 전환
            if (_isUnlocked)
            {
                Debug.Log("[UI_SkillInfo] 해금 완료 상태 UI 적용 - 비용/학습 버튼 숨김");
                
                // 해금 완료: 재화/가격 영역 숨기고 스킬 아이콘 영역 표시
                if (_costInfoGroup != null) _costInfoGroup.SetActive(false);
                if (_skillIconGroup != null) _skillIconGroup.SetActive(true);
                
                // 학습 버튼 숨김
                if (_learnButton != null) 
                {
                    _learnButton.gameObject.SetActive(false);
                    Debug.Log("[UI_SkillInfo] 학습 버튼 숨김 처리됨");
                }
                
                // 취소 버튼 텍스트를 "확인"으로 변경
                if (_cancelButtonText != null)
                {
                    _cancelButtonText.text = "확인";
                }
                
                // 연구 시간/비용 관련 UI 숨김
                if (_skillTimeText != null) _skillTimeText.gameObject.SetActive(false);
                if (_skillCostText != null) _skillCostText.gameObject.SetActive(false);
                if (_currencyIconImage != null) _currencyIconImage.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("[UI_SkillInfo] 해금 가능 상태 UI 적용 - 비용/학습 버튼 표시");
                
                // 해금 가능: 재화/가격 영역 표시, 스킬 아이콘 영역 숨김
                if (_costInfoGroup != null) _costInfoGroup.SetActive(true);
                if (_skillIconGroup != null) _skillIconGroup.SetActive(false);
                
                // 학습 버튼 표시
                if (_learnButton != null) 
                {
                    _learnButton.gameObject.SetActive(true);
                    Debug.Log("[UI_SkillInfo] 학습 버튼 표시 처리됨");
                }
                
                // 취소 버튼 텍스트를 "취소"로 복원
                if (_cancelButtonText != null)
                {
                    _cancelButtonText.text = "취소";
                }
                
                // 연구 시간
                if (_skillTimeText != null)
                {
                    _skillTimeText.gameObject.SetActive(true);
                    _skillTimeText.text = $"연구시간 {FormatTime(_currentSkillData.Time)}";
                }

                // 비용
                if (_skillCostText != null)
                {
                    _skillCostText.gameObject.SetActive(true);
                    _skillCostText.text = _currentSkillData.Cost.ToString("N0");
                }
                
                if (_currencyIconImage != null) _currencyIconImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 시간 포맷팅
        /// </summary>
        private string FormatTime(int totalSeconds)
        {
            if (totalSeconds <= 0)
            {
                return "즉시";
            }

            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(totalSeconds);

            if (timeSpan.TotalHours >= 1)
            {
                return string.Format("{0}시간 {1}분", (int)timeSpan.TotalHours, timeSpan.Minutes);
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return string.Format("{0}분 {1}초", timeSpan.Minutes, timeSpan.Seconds);
            }
            else
            {
                return string.Format("{0}초", timeSpan.Seconds);
            }
        }

        /// <summary>
        /// 학습 버튼 클릭
        /// </summary>
        private void OnLearnButtonClicked()
        {
            Debug.Log($"[UI_SkillInfo] 스킬 학습 확인: {_currentSkillData.Name}");
            
            _onLearnConfirmed?.Invoke(_currentSkillData);
            
            ClosePopup();
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelButtonClicked()
        {
            ClosePopup();
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        private void ClosePopup()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePopup(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 정리
            if (_learnButton != null)
            {
                _learnButton.onClick.RemoveListener(OnLearnButtonClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }

            if (_dimButton != null)
            {
                _dimButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
        }
    }
}
