using SahurRaising.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// 가챠 타입별 뽑기 로직을 처리하는 핸들러 인터페이스
    /// </summary>
    public interface IGachaHandler
    {
        GachaType Type { get; }

        /// <summary>
        /// 가챠를 뽑습니다
        /// </summary>
        List<GachaResult> Pull(int level, int count);

        /// <summary>
        /// 결과를 인벤토리에 추가합니다
        /// </summary>
        void AddToInventory(GachaResult result);

        /// <summary>
        /// 특정 레벨의 확률 정보를 가져옵니다
        /// </summary>
        List<GachaProbability> GetProbabilitiesForLevel(int level);

        /// <summary>
        /// 테이블에 있는 최대 레벨을 반환합니다
        /// </summary>
        int GetMaxLevel();
    }
}
