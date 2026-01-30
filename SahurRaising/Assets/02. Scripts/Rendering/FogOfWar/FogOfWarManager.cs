using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.Rendering
{
    /// <summary>
    /// UI 기반 전장의 안개(Fog of War) 매니저.
    /// Unity 6.0 URP 환경에서 동작하도록 Texture2D 기반으로 구현.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        public static FogOfWarManager Instance { get; private set; }

        [Header("맵 설정")]
        [Tooltip("안개가 덮일 UI 영역의 RectTransform (기본: Content)")]
        [SerializeField] private RectTransform _targetArea;
        
        [Header("품질 설정")]
        public Vector2Int TextureResolution = new Vector2Int(256, 256);
        public Color FogColor = new Color(0, 0, 0, 0.95f);
        [Tooltip("매 프레임 안개를 다시 덮을지 여부. True면 시야 밖은 다시 어두워짐.")]
        public bool ClearFogEveryFrame = false;
        [Tooltip("맵의 가로세로 비율에 맞춰 텍스처 해상도 Y를 자동 조절합니다.")]
        public bool AutoAspect = true;
        [Tooltip("안개 가장자리 부드러움 (0~1)")]
        [Range(0f, 1f)]
        public float EdgeSoftness = 0.3f;

        [Header("연결")]
        [SerializeField] private RawImage _fogOverlayImage;

        // 내부 상태
        private Texture2D _fogTexture;
        private Color[] _fogPixels;
        private Color[] _clearPixels; // 완전 안개 상태
        private Material _overlayMaterial;
        private List<FogRevealer> _revealers = new List<FogRevealer>();
        private bool _needsUpdate = true;
        private bool _isInitialized = false;
        private Canvas _parentCanvas;
        private RectTransform _fogOverlayRect;

        private void Awake()
        {
            if (Instance == null) 
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // 재활성화 시 텍스처 연결 복구 및 업데이트 요청
            if (_isInitialized && _fogOverlayImage != null && _fogTexture != null)
            {
                _fogOverlayImage.texture = _fogTexture;
                _needsUpdate = true;
            }
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // RawImage 자동 탐색
            if (_fogOverlayImage == null)
            {
                _fogOverlayImage = GetComponent<RawImage>();
                if (_fogOverlayImage == null)
                {
                    Debug.LogError("[FogOfWar] RawImage 컴포넌트가 없습니다.");
                    return;
                }
            }

            _fogOverlayRect = _fogOverlayImage.rectTransform;
            
            // 부모 캔버스 찾기
            _parentCanvas = _fogOverlayImage.GetComponentInParent<Canvas>();
            if (_parentCanvas == null)
            {
                Debug.LogError("[FogOfWar] 부모 Canvas를 찾을 수 없습니다.");
                return;
            }

            // Target Area 자동 탐색 (설정 안했으면 부모의 Content 탐색)
            if (_targetArea == null)
            {
                var scrollRect = GetComponentInParent<ScrollRect>();
                if (scrollRect != null && scrollRect.content != null)
                {
                    _targetArea = scrollRect.content;
                }
                else
                {
                    // Viewport 또는 부모 사용
                    _targetArea = transform.parent as RectTransform;
                }
            }

            // 텍스처 생성
            CreateFogTexture();
            
            // 머티리얼 설정
            SetupMaterial();

            _isInitialized = true;
            _needsUpdate = true;
            
            Debug.Log($"[FogOfWar] 초기화 완료. 해상도: {TextureResolution}, TargetArea: {_targetArea?.name}");
        }

        private void CreateFogTexture()
        {
            if (_fogTexture != null)
            {
                DestroyAdaptive(_fogTexture);
            }

            int width = TextureResolution.x;
            int height = TextureResolution.y;

            // 비율 자동 보정
            if (AutoAspect && _targetArea != null)
            {
                Rect rect = _targetArea.rect;
                if (rect.width > 0 && rect.height > 0)
                {
                    float ratio = rect.height / rect.width;
                    height = Mathf.RoundToInt(width * ratio);
                    // 인스펙터 값도 갱신하여 보여줌 (선택사항)
                    TextureResolution.y = height; 
                }
            }

            _fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _fogTexture.filterMode = FilterMode.Bilinear;
            _fogTexture.wrapMode = TextureWrapMode.Clamp;
            _fogTexture.hideFlags = HideFlags.DontSave;

            int pixelCount = width * height;
            _fogPixels = new Color[pixelCount];
            _clearPixels = new Color[pixelCount];

            // 초기 안개 상태로 채우기
            for (int i = 0; i < pixelCount; i++)
            {
                _clearPixels[i] = FogColor;
                _fogPixels[i] = FogColor;
            }
            
            _fogTexture.SetPixels(_fogPixels);
            _fogTexture.Apply();
            
            Debug.Log($"[FogOfWar] 텍스처 생성: {width}x{height} (AutoAspect: {AutoAspect})");
        }

        private void SetupMaterial()
        {
            // UI 기본 머티리얼 사용 (셰이더 대신 RawImage의 color로 처리)
            // 텍스처 자체에 알파값을 포함시켜 투명도 처리
            _fogOverlayImage.texture = _fogTexture;
            _fogOverlayImage.color = Color.white;
            _fogOverlayImage.raycastTarget = false;
        }

        public void RegisterRevealer(FogRevealer revealer)
        {
            if (revealer == null) return;
            if (!_revealers.Contains(revealer))
            {
                _revealers.Add(revealer);
                _needsUpdate = true;
            }
        }

        public void UnregisterRevealer(FogRevealer revealer)
        {
            if (revealer == null) return;
            if (_revealers.Contains(revealer))
            {
                _revealers.Remove(revealer);
                _needsUpdate = true;
            }
        }

        /// <summary>
        /// 외부에서 강제 업데이트 요청
        /// </summary>
        public void RequestUpdate()
        {
            _needsUpdate = true;
        }

        private void LateUpdate()
        {
            if (!_isInitialized || _fogTexture == null) return;

            // ClearFogEveryFrame이 true거나 업데이트 필요 시 
            if (ClearFogEveryFrame || _needsUpdate)
            {
                UpdateFogTexture();
                _needsUpdate = false;
            }
        }

        private void UpdateFogTexture()
        {
            if (_targetArea == null || _fogOverlayRect == null) return;

            // 안개로 초기화
            System.Array.Copy(_clearPixels, _fogPixels, _fogPixels.Length);

            // FogOverlay의 World Corners 계산
            Vector3[] overlayCorners = new Vector3[4];
            _fogOverlayRect.GetWorldCorners(overlayCorners);
            
            Vector2 overlayMin = overlayCorners[0]; // 좌하단
            Vector2 overlayMax = overlayCorners[2]; // 우상단
            Vector2 overlaySize = overlayMax - overlayMin;

            if (overlaySize.x <= 0 || overlaySize.y <= 0) return;

            // 각 Revealer에 대해 안개 제거
            foreach (var rev in _revealers)
            {
                if (rev == null || !rev.enabled || !rev.gameObject.activeInHierarchy) continue;

                // Revealer의 월드 위치 가져오기
                Vector3 revealerWorldPos = rev.transform.position;
                
                // FogOverlay 영역 내 상대 좌표로 변환 (0~1)
                float u = (revealerWorldPos.x - overlayMin.x) / overlaySize.x;
                float v = (revealerWorldPos.y - overlayMin.y) / overlaySize.y;

                // 범위를 벗어나면 스킵 (약간의 여유 둠)
                if (u < -0.1f || u > 1.1f || v < -0.1f || v > 1.1f) continue;

                // 반경을 텍스처 픽셀로 변환
                // Revealer의 반경은 RectTransform 기준이므로 월드 스케일 고려
                float worldRadius = rev.Radius;
                
                // 월드 좌표 기준 반경을 UV 공간 반경으로 변환
                float radiusU = worldRadius / overlaySize.x;
                float radiusV = worldRadius / overlaySize.y;
                
                int pixelRadiusX = Mathf.CeilToInt(radiusU * _fogTexture.width);
                int pixelRadiusY = Mathf.CeilToInt(radiusV * _fogTexture.height);
                
                int centerX = Mathf.RoundToInt(u * _fogTexture.width);
                int centerY = Mathf.RoundToInt(v * _fogTexture.height);

                // 영향 범위 내 픽셀 업데이트
                DrawCircle(centerX, centerY, pixelRadiusX, pixelRadiusY, rev.Intensity);
            }

            // 텍스처 적용
            _fogTexture.SetPixels(_fogPixels);
            _fogTexture.Apply();
        }

        private void DrawCircle(int centerX, int centerY, int radiusX, int radiusY, float intensity)
        {
            if (_fogTexture == null) return;
            
            int texWidth = _fogTexture.width;
            int texHeight = _fogTexture.height;

            int minX = Mathf.Max(0, centerX - radiusX);
            int maxX = Mathf.Min(texWidth - 1, centerX + radiusX);
            int minY = Mathf.Max(0, centerY - radiusY);
            int maxY = Mathf.Min(texHeight - 1, centerY + radiusY);

            float radSqX = radiusX * radiusX;
            float radSqY = radiusY * radiusY;

            // 0으로 나누기 방지
            if (radSqX <= 0 || radSqY <= 0) return;

            for (int y = minY; y <= maxY; y++)
            {
                // 행의 시작 인덱스 미리 계산 (최적화)
                int rowOffset = y * texWidth;
                
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    
                    // 타원 방정식: (dx/rx)^2 + (dy/ry)^2 <= 1
                    float normalizedDistSq = (dx * dx) / radSqX + (dy * dy) / radSqY;

                    if (normalizedDistSq <= 1f)
                    {
                        int idx = rowOffset + x;
                        
                        // 정규화된 거리 (0 = 중심, 1 = 가장자리)
                        float dist = Mathf.Sqrt(normalizedDistSq);
                        float edgeStart = 1f - EdgeSoftness;
                        float targetAlpha;
                        
                        if (dist <= edgeStart)
                        {
                            // 완전히 밝음 (투명)
                            targetAlpha = 0f;
                        }
                        else
                        {
                            // 가장자리: edgeStart ~ 1 사이에서 부드럽게 페이드
                            float t = (dist - edgeStart) / EdgeSoftness;
                            targetAlpha = Mathf.SmoothStep(0f, FogColor.a, t);
                        }

                        // intensity 적용 (intensity가 1이면 완전히 밝히고, 0이면 효과 없음)
                        targetAlpha = Mathf.Lerp(FogColor.a, targetAlpha, intensity);
                        
                        // 기존 값과 비교하여 더 밝은(투명한) 값 유지
                        _fogPixels[idx].a = Mathf.Min(_fogPixels[idx].a, targetAlpha);
                    }
                }
            }
        }

        private void DestroyAdaptive(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            if (_fogTexture != null)
            {
                DestroyAdaptive(_fogTexture);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값 변경 시 재생성
            if (_isInitialized && Application.isPlaying)
            {
                CreateFogTexture();
                SetupMaterial();
                _needsUpdate = true;
            }
        }
#endif
    }
}
