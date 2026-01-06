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
        /// 현재 가챠 레벨을 가져옵니다
        /// </summary>
        int GetGachaLevel(GachaType type);

        /// <summary>
        /// 누적 뽑기 개수를 가져옵니다
        /// </summary>
        int GetTotalCount(GachaType type);

        /// <summary>
        /// 다음 레벨업까지 필요한 누적 개수를 반환합니다
        /// </summary>
        int GetRequiredCountForNextLevel(GachaType type);

        /// <summary>
        /// 특정 레벨에 필요한 누적 개수를 반환합니다
        /// </summary>
        int GetRequiredCountForLevel(GachaType type, int level);

        /// <summary>
        /// 뽑기를 실행합니다 (1회, 10회, 35회)
        /// </summary>
        /// <param name="type">가챠 타입 (Equipment 또는 Drone)</param>
        /// <param name="count">뽑기 횟수 (1, 10, 35)</param>
        /// <param name="cost">소비할 비용</param>
        /// <param name="currencyType">사용할 재화 타입</param>
        /// <returns>뽑기 결과 리스트</returns>
        UniTask<List<GachaResult>> DrawAsync(GachaType type, int count, BigDouble cost, CurrencyType currencyType);

        /// <summary>
        /// 뽑기 비용을 가져옵니다
        /// </summary>
        BigDouble GetCost(GachaType type, int count);

        UniTask SaveAsync();
        UniTask LoadAsync();
    }

    /// <summary>
    /// 가챠 뽑기 결과
    /// </summary>
    public struct GachaResult
    {
        public GachaType Type;
        public string ItemCode;  // EquipmentCode 또는 DroneID
        public EquipmentGrade? Grade;  // 장비인 경우 등급
    }
}