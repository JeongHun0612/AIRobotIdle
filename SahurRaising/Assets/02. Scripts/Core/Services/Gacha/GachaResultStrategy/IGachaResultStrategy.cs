using Cysharp.Threading.Tasks;
using BreakInfinity;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 가챠 결과 UI 전략 인터페이스
    /// 타입별로 다른 UI 동작을 정의합니다
    /// </summary>
    public interface IGachaResultStrategy
    {
        GachaType Type { get; }

        /// <summary>
        /// 표시할 재화 타입을 반환합니다
        /// </summary>
        CurrencyType GetCurrencyType();

        /// <summary>
        /// 슬롯이 고등급인지 확인합니다
        /// </summary>
        bool IsHighGrade(string gradeKey);

        /// <summary>
        /// 아이템이 신규인지 확인합니다
        /// </summary>
        bool IsNewItem(string itemCode);

        /// <summary>
        /// 등급 텍스트를 포맷팅합니다
        /// Equipment: "D3" → "D", Drone: "5" → "V"
        /// </summary>
        string FormatGradeText(string gradeKey);

        /// <summary>
        /// GemCount를 반환합니다
        /// </summary>
        int GetGemCount(string gradeKey);
    }
}