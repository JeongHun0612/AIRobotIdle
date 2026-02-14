using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaRatePanel : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private Image _frame;
        [SerializeField] private TMP_Text _gradeText;
        [SerializeField] private TMP_Text _rateText;

        public void SetData(Color frameColor, string gradeText, float probability)
        {
            // 등급에 따른 프레임 색상 설정
            if (_frame != null)
            {
                _frame.color = frameColor;
            }

            // 등급 텍스트 설정
            if (_gradeText != null)
            {
                _gradeText.text = gradeText;
            }

            // 확률 텍스트 설정 (퍼센트로 표시)
            if (_rateText != null)
            {
                _rateText.text = $"{probability:F3}%";
            }
        }
    }
}
