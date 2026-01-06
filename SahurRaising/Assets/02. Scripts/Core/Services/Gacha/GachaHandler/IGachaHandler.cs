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
        /// <param name="level">현재 가챠 레벨</param>
        /// <param name="count">뽑기 횟수</param>
        /// <returns>뽑기 결과 리스트</returns>
        List<GachaResult> Pull(int level, int count);

        /// <summary>
        /// 결과를 인벤토리에 추가합니다
        /// </summary>
        void AddToInventory(GachaResult result);
    }
}
