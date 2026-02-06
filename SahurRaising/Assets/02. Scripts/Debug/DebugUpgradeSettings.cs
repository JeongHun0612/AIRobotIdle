using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Debug용 업그레이드 컨트롤 ScriptableObject
    /// 에디터에서 업그레이드 레벨을 관리할 수 있는 디버그 도구
    /// 
    /// 모든 기능은 에디터 Inspector를 통해 제공되며,
    /// 업그레이드 목록은 UpgradeTable에서 자동으로 로드됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugUpgradeSettings", menuName = "SahurRaising/Debug/DebugUpgradeSettings")]
    public class DebugUpgradeSettings : ScriptableObject
    {
        // 이 클래스는 에디터에서 ScriptableObject로 선택할 수 있게 하는
        // 컨테이너 역할만 합니다.
        // 
        // 실제 기능은 DebugUpgradeSettingsEditor에서 제공됩니다:
        // - 전체 초기화 (레벨 0으로)
        // - 전체 최대 레벨 설정
        // - 개별 업그레이드 레벨 조절
        // - 업그레이드 검색 및 필터링
        // - 상태 저장
    }
}
