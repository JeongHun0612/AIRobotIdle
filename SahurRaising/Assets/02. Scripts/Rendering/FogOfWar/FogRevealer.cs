using UnityEngine;

namespace SahurRaising.Rendering
{
    /// <summary>
    /// 전장의 안개를 걷어내는 컴포넌트.
    /// 스킬 슬롯 등에 붙여서 해금된 영역의 안개를 밝힙니다.
    /// </summary>
    public class FogRevealer : MonoBehaviour
    {
        [Tooltip("안개를 걷어낼 반경 (UI 픽셀 단위)")]
        public float Radius = 80f;
        
        [Tooltip("밝기 강도 (0~1)")]
        [Range(0, 1)]
        public float Intensity = 1f;

        private bool _isRegistered = false;

        private void OnEnable()
        {
            RegisterToManager();
        }

        private void OnDisable()
        {
            UnregisterFromManager();
        }

        private void Start()
        {
            // OnEnable보다 늦게 Manager가 초기화될 수 있으므로 재등록 시도
            if (!_isRegistered)
            {
                RegisterToManager();
            }
        }

        private void RegisterToManager()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RegisterRevealer(this);
                _isRegistered = true;
            }
        }

        private void UnregisterFromManager()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UnregisterRevealer(this);
                _isRegistered = false;
            }
        }

        /// <summary>
        /// 강제로 안개 업데이트 요청
        /// </summary>
        public void RequestFogUpdate()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RequestUpdate();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // UI 요소의 경우 Gizmo가 3D 공간에 그려지므로 근사값으로 표시
            Gizmos.color = Color.cyan;
            // RectTransform 스케일을 대략적으로 반영
            Gizmos.DrawWireSphere(transform.position, Radius * 0.01f);
        }
        
        private void OnValidate()
        {
            // 에디터에서 값 변경 시 업데이트 요청
            if (Application.isPlaying && FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RequestUpdate();
            }
        }
#endif
    }
}
