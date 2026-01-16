using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    public interface IDroneService
    {
        /// <summary>
        /// 서비스 초기화 (DroneTable 로드)
        /// </summary>
        UniTask InitializeAsync();

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 모든 드론 목록을 가져옵니다.
        /// </summary>
        IReadOnlyList<DroneRow> GetAll();

        /// <summary>
        /// ID로 드론을 가져옵니다.
        /// </summary>
        bool TryGetByID(string id, out DroneRow drone);

        /// <summary>
        /// 현재 장착된 드론 ID를 반환합니다.
        /// </summary>
        string GetEquippedID();

        /// <summary>
        /// 드론을 장착합니다. 기존 장착 드론은 해제됩니다.
        /// </summary>
        bool Equip(string droneID);

        /// <summary>
        /// 드론을 해제합니다.
        /// </summary>
        bool Unequip();

        /// <summary>
        /// 드론 뽑기/보상 등으로 인벤토리에 드론을 추가합니다.
        /// </summary>
        bool AddToInventory(string droneID, int count = 1);

        /// <summary>
        /// 인벤토리에서 해당 드론의 정보를 가져옵니다.
        /// </summary>
        DroneInventoryInfo GetInventoryInfo(string droneID);

        /// <summary>
        /// 드론의 레벨을 1 증가시킵니다.
        /// </summary>
        bool LevelUp(string droneID);

        /// <summary>
        /// 보유한 모든 드론을 일괄 승급합니다.
        /// </summary>
        /// <returns> 승급된 드론 결과 리스트 (승급 불가능하면 빈 리스트)</returns>
        List<AdvanceResult> AdvanceAllAvailable();

        /// <summary>
        /// 드론 1회 승급을 위해 필요한 동일 드론 개수를 반환합니다.
        /// </summary>
        int GetRequiredCountForAdvance();

        /// <summary>
        /// 해당 드론 ID가 '새로 획득했지만 아직 UI에서 한 번도 본 적 없는 상태'인지 여부를 반환합니다.
        /// 인벤토리 내에 존재하고, Seen 목록에 포함되지 않은 경우 true를 반환합니다.
        /// </summary>
        bool IsNewDrone(string droneID);

        /// <summary>
        /// 해당 드론 ID를 '이미 한 번 본 드론'으로 기록합니다.
        /// 이후부터는 IsNewDrone 호출 시 NEW 대상으로 판단되지 않습니다.
        /// </summary>
        void MarkAsSeen(string droneID);

        /// <summary>
        /// 데이터를 저장합니다.
        /// </summary>
        UniTask SaveAsync();

        /// <summary>
        /// 데이터를 로드합니다.
        /// </summary>
        UniTask LoadAsync();
    }
}
