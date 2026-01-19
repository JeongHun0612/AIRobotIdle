using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    /// <summary>
    /// 초기 로딩 진행도를 표시하는 씬 UI.
    /// UIManager가 씬으로 관리하므로 Addressable 프리팹에 이 스크립트를 붙인다.
    /// </summary>
    public class UILoadingScene : UI_Scene
    {
        [Header("Progress")]
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TextMeshProUGUI _progressText;

        [Header("Touch To Start")]
        [SerializeField] private GameObject _touchToStartObject;
        [SerializeField] private GameObject _loadingIconObject;

        public void ShowTouchToStart()
        {
            if (_loadingIconObject != null)
                _loadingIconObject.SetActive(false);

            if (_touchToStartObject != null)
                _touchToStartObject.SetActive(true);
        }

        /// <summary>
        /// 0~1 값으로 진행도를 표시한다.
        /// </summary>
        public void SetProgress(float value)
        {
            float clamped = Mathf.Clamp01(value);

            if (_progressSlider != null)
                _progressSlider.value = clamped;

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
            
            // 로딩 중에는 터치 UI 숨김, 로딩 아이콘 표시
            if (clamped < 1f)
            {
                if (_touchToStartObject != null) _touchToStartObject.SetActive(false);
                if (_loadingIconObject != null) _loadingIconObject.SetActive(true);
            }
        }
    }
}

