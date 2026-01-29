using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Debug용 스킬 컨트롤 ScriptableObject
    /// 에디터에서 스킬 해금 상태를 관리할 수 있는 디버그 도구
    /// 
    /// 모든 기능은 에디터 Inspector를 통해 제공되며,
    /// 스킬 목록은 SkillTable에서 자동으로 로드됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugSkillSettings", menuName = "SahurRaising/Debug/DebugSkillSettings")]
    public class DebugSkillSettings : ScriptableObject
    {
        // 이 클래스는 에디터에서 ScriptableObject로 선택할 수 있게 하는
        // 컨테이너 역할만 합니다.
        // 
        // 실제 기능은 DebugSkillSettingsEditor에서 제공됩니다:
        // - 전체 초기화 (학습 전 상태로)
        // - 전체 해금
        // - 개별 스킬 해금/잠금
        // - 스킬 검색 및 필터링
        // - 상태 저장
    }
}
