using System.Collections.Generic;
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

        public override void Initialize()
        {
            base.Initialize();

            // 하단 메뉴 초기화 (필요하다면)
            // _bottomBarMenu가 MonoBehaviour의 Awake/Start에서 스스로 초기화할 수도 있지만,
            // 여기서 명시적으로 제어할 수도 있음.

        }

        public override void OnShow()
        {
            base.OnShow();

            // 씬이 보여질 때 필요한 로직
            // 예: BGM 재생, 카메라 세팅 등

        }
    }
}

