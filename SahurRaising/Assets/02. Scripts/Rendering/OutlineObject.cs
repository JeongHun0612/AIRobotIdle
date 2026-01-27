using UnityEngine;

namespace SahurRaising.Rendering
{
    /// <summary>
    /// 아웃라인 스타일 타입
    /// </summary>
    public enum OutlineStyle
    {
        /// <summary>
        /// 실루엣 아웃라인 - 객체 외곽선만 표시 (ex. 오버쿡, 젤다)
        /// </summary>
        Silhouette = 0,
        
        /// <summary>
        /// 내부 엣지 포함 - 모델의 디테일 라인도 포함
        /// </summary>
        WithInnerEdge = 1,
        
        /// <summary>
        /// 선택적 하이라이트 - 특정 객체 강조용 글로우 느낌
        /// </summary>
        Highlight = 2
    }

    /// <summary>
    /// 아웃라인을 적용할 객체에 부착하는 컴포넌트
    /// Renderer가 있는 객체에 부착하면 해당 객체에 아웃라인이 적용됩니다.
    /// </summary>
    [ExecuteAlways] // 에디터 모드에서도 OnEnable/OnDisable 호출 필요
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class OutlineObject : MonoBehaviour
    {
        [Header("아웃라인 설정")]
        [Tooltip("아웃라인 스타일")]
        [SerializeField] private OutlineStyle _style = OutlineStyle.Silhouette;
        
        [Tooltip("아웃라인 색상")]
        [SerializeField] private Color _outlineColor = Color.white;
        
        [Tooltip("아웃라인 두께 (픽셀 단위)")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _outlineWidth = 2f;
        
        [Header("하이라이트 전용 설정")]
        [Tooltip("글로우 강도 (Highlight 스타일에서만 사용)")]
        [Range(0f, 5f)]
        [SerializeField] private float _glowIntensity = 1.5f;
        
        [Tooltip("글로우 퍼짐 정도")]
        [Range(0f, 20f)]
        [SerializeField] private float _glowSpread = 3f;

        // 프로퍼티
        public OutlineStyle Style => _style;
        public Color OutlineColor => _outlineColor;
        public float OutlineWidth => _outlineWidth;
        public float GlowIntensity => _glowIntensity;
        public float GlowSpread => _glowSpread;

        // 캐시된 Renderer
        private Renderer _renderer;
        public Renderer Renderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = GetComponent<Renderer>();
                return _renderer;
            }
        }

        // 전역 아웃라인 오브젝트 관리
        private static readonly System.Collections.Generic.HashSet<OutlineObject> _activeOutlines = new();
        public static System.Collections.Generic.IReadOnlyCollection<OutlineObject> ActiveOutlines => _activeOutlines;

        private void OnEnable()
        {
            _activeOutlines.Add(this);
        }

        private void OnDisable()
        {
            _activeOutlines.Remove(this);
        }

        /// <summary>
        /// 런타임에서 아웃라인 색상 변경
        /// </summary>
        public void SetColor(Color color)
        {
            _outlineColor = color;
        }

        /// <summary>
        /// 런타임에서 아웃라인 두께 변경
        /// </summary>
        public void SetWidth(float width)
        {
            _outlineWidth = Mathf.Clamp(width, 0.5f, 10f);
        }

        /// <summary>
        /// 런타임에서 아웃라인 스타일 변경
        /// </summary>
        public void SetStyle(OutlineStyle style)
        {
            _style = style;
        }

        /// <summary>
        /// 도메인 리로드 비활성화 대응: 게임 시작 시 목록 초기화
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetList()
        {
            _activeOutlines.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값 변경 시 즉시 반영되도록 처리
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }
        }

        private void Reset()
        {
            // 컴포넌트 추가 시 기본값 설정
            _outlineColor = Color.white;
            _outlineWidth = 2f;
            _style = OutlineStyle.Silhouette;
        }
#endif
    }
}
