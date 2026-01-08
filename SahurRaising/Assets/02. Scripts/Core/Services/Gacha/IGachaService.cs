using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using BreakInfinity;

namespace SahurRaising.Core
{
    public interface IGachaService
    {
        UniTask InitializeAsync();
        bool IsInitialized { get; }

        /// <summary>
        /// GachaLevelConfig에 직접 접근 (읽기 전용)
        /// </summary>
        GachaLevelConfig LevelConfig { get; }

        /// <summary>
        /// GachaGradeColorConfig 직접 접근 (읽기 전용)
        /// </summary>
        GachaGradeColorConfig GradeColorConfig { get; }

        /// <summary>
        /// 현재 가챠 레벨을 가져옵니다
        /// </summary>
        int GetGachaLevel(GachaType type);

        /// <summary>
        /// 누적 뽑기 개수를 가져옵니다
        /// </summary>
        int GetGachaCount(GachaType type);

        /// <summary>
        /// 다음 레벨업까지 필요한 누적 개수를 반환합니다
        /// </summary>
        int GetRequiredCountForNextLevel(GachaType type);

        /// <summary>
        /// 특정 레벨에 필요한 누적 개수를 반환합니다
        /// </summary>
        int GetRequiredCountForLevel(GachaType type, int level);

        /// <summary>
        /// 가챠 타입에 해당하는 재화 타입을 반환합니다.
        /// </summary>
        /// <param name="type">가챠 타입</param>
        /// <returns>해당 가챠 타입에 사용되는 재화 타입</returns>
        CurrencyType GetCurrencyType(GachaType type);

        /// <summary>
        /// 가챠를 뽑습니다
        /// </summary>
        /// <param name="type">가챠 타입</param>
        /// <param name="count">뽑기 횟수</param>
        /// <param name="cost">소비할 비용</param>
        /// <returns>뽑기 결과 리스트</returns>
        List<GachaResult> Pull(GachaType type, int count, BigDouble cost);

        UniTask SaveAsync();
        UniTask LoadAsync();
    }

    /// <summary>
    /// 가챠 뽑기 결과
    /// </summary>
    public struct GachaResult
    {
        public GachaType Type;
        public string ItemCode;
    }
}