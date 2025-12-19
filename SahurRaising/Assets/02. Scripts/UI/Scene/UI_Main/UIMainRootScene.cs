using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.UI
{
    /// <summary>
    /// 메인 화면 루트 씬. 
    /// - UIManager에 의해 로드되는 메인 배틀 씬 UI
    /// - 하단 탭 메뉴를 포함하고 초기화한다.
    /// </summary>
    public class UIMainRootScene : UI_Scene
    {
        [Header("Components")]
        [SerializeField] private UIBottomBarMenu _bottomBarMenu;

        [Header("Persistent UI (Upgrade Panel)")]
        [SerializeField] private Transform _upgradePanelRoot;
        [SerializeField] private UIUpgradePanel _upgradePanelPrefab;
        private UIUpgradePanel _upgradePanelInstance;

        // 왜: UIManager의 씬 캐시에서 꺼내졌을 때, 씬 단위로 필요한 연결/초기화를 한 곳에서 보장한다.
        public override void Initialize()
        {
            base.Initialize();

            // 하단 메뉴 초기화 (필요하다면)
            // _bottomBarMenu가 MonoBehaviour의 Awake/Start에서 스스로 초기화할 수도 있지만,
            // 여기서 명시적으로 제어할 수도 있음.

        }

        // 왜: 로딩 중 1프레임 노출/서비스 미등록 시점 접근을 피하기 위해,
        //     상시 UI 인스턴스는 초기화 단계에서 '생성만' 해 두고 실제 데이터 바인딩은 OnShow에서 한다.
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            EnsureUpgradePanelInstance();
            await UniTask.Yield();
        }

        // 왜: 메인 화면 진입 시 상시 패널은 항상 켜져 있어야 하므로, 씬 표시 이벤트에 맞춰 노출한다.
        public override void OnShow()
        {
            base.OnShow();

            // 씬이 보여질 때 필요한 로직
            // 예: BGM 재생, 카메라 세팅 등
            _upgradePanelInstance?.Show();

        }

        // 왜: 씬이 숨겨질 때 상시 패널도 함께 숨겨, 다른 씬 UI와 입력/레이어 충돌을 방지한다.
        public override void OnHide()
        {
            base.OnHide();
            _upgradePanelInstance?.Hide();
        }

        // 왜: 업그레이드 패널은 팝업이 아니라 '항시 존재'해야 하므로, 씬 내부에 안전하게 1회만 인스턴스화한다.
        private void EnsureUpgradePanelInstance()
        {
            if (_upgradePanelInstance != null)
                return;

            if (_upgradePanelRoot == null)
                return;

            // 왜: 프리팹이 없으면(제작 전/직접 씬 배치) 루트 하위에서 패널을 찾아 사용한다.
            if (_upgradePanelPrefab == null)
            {
                _upgradePanelInstance = _upgradePanelRoot.GetComponentInChildren<UIUpgradePanel>(includeInactive: true);
                if (_upgradePanelInstance == null)
                    return;

                _upgradePanelInstance.gameObject.SetActive(false);
                _upgradePanelInstance.Initialize();
                return;
            }

            // 왜: 업그레이드 패널은 팝업이 아니라 상시 UI이므로, 씬(UI_Scene) 내부에 1회만 인스턴스화한다.
            _upgradePanelInstance = Instantiate(_upgradePanelPrefab, _upgradePanelRoot);
            _upgradePanelInstance.gameObject.SetActive(false);
            _upgradePanelInstance.Initialize();
        }
    }
}

