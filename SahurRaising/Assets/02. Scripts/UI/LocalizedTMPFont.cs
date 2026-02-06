using SahurRaising.Core;
using TMPro;
using UnityEngine;

namespace SahurRaising.UI
{
    /// <summary>
    /// 현재 로캘 코드에 따라 TMP_Text 폰트를 교체해 주는 컴포넌트
    /// - "Korean (South Korea)"  : ko-KR  -> 한국어/영어 폰트(DNFBitBitv2 등)
    /// - "English"               : en     -> 한국어/영어 폰트(DNFBitBitv2 등)
    /// - "Chinese (Traditional)" : zh-Hant -> CJK 폰트(BoutiqueBitmap9x9_Bold_2 등)
    /// - "Chinese (Simplified)"  : zh-Hans -> CJK 폰트
    /// - "Japanese"              : ja-JP   -> CJK 폰트
    /// 필요 시 인스펙터에서 폰트만 교체하여 사용한다.
    /// 
    /// [중요] 커스텀 머티리얼(아웃라인 등)을 사용하는 경우:
    /// - useCustomMaterial을 체크하고 각 폰트에 맞는 머티리얼을 설정하세요.
    /// - 폰트 변경 시 해당 머티리얼이 자동으로 적용됩니다.
    /// </summary>
    public class LocalizedTMPFont : MonoBehaviour
    {
        [Header("폰트 설정")]
        [SerializeField] private TMP_FontAsset koreanEnglishFont;    // 예: DNFBitBitv2 SDF
        [SerializeField] private TMP_FontAsset cjkFont;              // 예: BoutiqueBitmap9x9_Bold_2

        [Header("커스텀 머티리얼 설정 (아웃라인 등)")]
        [SerializeField] private bool useCustomMaterial;
        [Tooltip("한국어/영어 폰트에 사용할 커스텀 머티리얼 (예: Outline 머티리얼)")]
        [SerializeField] private Material koreanEnglishMaterial;
        [Tooltip("CJK 폰트에 사용할 커스텀 머티리얼 (예: Outline 머티리얼)")]
        [SerializeField] private Material cjkMaterial;

        private TMP_Text _text;
        private ILocalizationService _localizationService;
        
        // 원본 머티리얼 백업 (커스텀 머티리얼이 설정되지 않은 경우 원본 유지용)
        private Material _originalMaterial;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            
            // 원본 머티리얼 백업 (커스텀 머티리얼이 설정되어 있으면 이걸 사용)
            if (_text != null)
            {
                _originalMaterial = _text.fontSharedMaterial;
            }

            if (!ServiceLocator.HasService<ILocalizationService>())
            {
                Debug.LogWarning("[LocalizedTMPFont] ILocalizationService가 등록되어 있지 않습니다.");
                return;
            }

            _localizationService = ServiceLocator.Get<ILocalizationService>();
            if (_localizationService == null)
            {
                Debug.LogWarning("[LocalizedTMPFont] ILocalizationService 인스턴스를 가져오지 못했습니다.");
                return;
            }

            // 초기 로캘에 맞춰 폰트 적용
            ApplyCurrentLocaleFont();

            // 로캘 변경 이벤트 구독
            _localizationService.OnLocaleChanged += HandleLocaleChanged;
        }

        private void OnDestroy()
        {
            if (_localizationService != null)
            {
                _localizationService.OnLocaleChanged -= HandleLocaleChanged;
            }
        }

        private void HandleLocaleChanged()
        {
            ApplyCurrentLocaleFont();
        }

        /// <summary>
        /// 현재 로캘 코드에 따라 적절한 폰트와 머티리얼을 적용한다.
        /// </summary>
        private void ApplyCurrentLocaleFont()
        {
            if (_localizationService == null || _text == null)
                return;

            string code = _localizationService.GetCurrentLocaleCode();
            if (string.IsNullOrEmpty(code))
                return;

            TMP_FontAsset targetFont = GetFontForLocale(code);
            Material targetMaterial = GetMaterialForLocale(code);
            
            if (targetFont != null && _text.font != targetFont)
            {
                _text.font = targetFont;
            }
            
            // 커스텀 머티리얼 적용 (폰트 변경 후 머티리얼이 리셋되므로 항상 적용)
            if (targetMaterial != null)
            {
                _text.fontSharedMaterial = targetMaterial;
            }
            else if (_originalMaterial != null && useCustomMaterial)
            {
                // 커스텀 머티리얼 사용 설정되었지만 해당 로캘용 머티리얼이 없으면 원본 유지
                _text.fontSharedMaterial = _originalMaterial;
            }
        }
        
        /// <summary>
        /// 로캘 코드에 따라 사용할 머티리얼을 결정한다.
        /// </summary>
        private Material GetMaterialForLocale(string localeCode)
        {
            if (!useCustomMaterial)
                return null;
                
            if (string.IsNullOrEmpty(localeCode))
                return _originalMaterial;
            
            switch (localeCode)
            {
                case "ko-KR":
                case "en":
                    return koreanEnglishMaterial;

                case "zh-Hant":
                case "zh-Hans":
                case "ja-JP":
                    return cjkMaterial;

                default:
                    var lower = localeCode.ToLowerInvariant();
                    if (lower.StartsWith("ko") || lower.StartsWith("en"))
                        return koreanEnglishMaterial;
                    if (lower.StartsWith("zh") || lower.StartsWith("ja"))
                        return cjkMaterial;
                    return _originalMaterial;
            }
        }

        /// <summary>
        /// 로캘 코드에 따라 사용할 폰트를 결정한다.
        /// - ko-KR, en          => koreanEnglishFont
        /// - zh-Hant, zh-Hans,
        ///   ja-JP              => cjkFont
        /// 필요하면 조건을 프로젝트 로캘 코드에 맞게 확장/수정한다.
        /// </summary>
        private TMP_FontAsset GetFontForLocale(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode))
                return _text.font;

            // Unity Localization에서 넘어오는 코드 형식:
            // - Korean (South Korea) : ko-KR
            // - English              : en
            // - Chinese (Traditional): zh-Hant
            // - Chinese (Simplified) : zh-Hans
            // - Japanese             : ja-JP

            switch (localeCode)
            {
                case "ko-KR":
                case "en":
                    return koreanEnglishFont != null ? koreanEnglishFont : _text.font;

                case "zh-Hant":
                case "zh-Hans":
                case "ja-JP":
                    return cjkFont != null ? cjkFont : _text.font;

                default:
                    // 예상치 못한 코드(예: en-US 등)는 앞 2글자로 한 번 더 폴백
                    var lower = localeCode.ToLowerInvariant();
                    if (lower.StartsWith("ko") || lower.StartsWith("en"))
                        return koreanEnglishFont != null ? koreanEnglishFont : _text.font;
                    if (lower.StartsWith("zh") || lower.StartsWith("ja"))
                        return cjkFont != null ? cjkFont : _text.font;
                    return _text.font;
            }
        }
    }
}
