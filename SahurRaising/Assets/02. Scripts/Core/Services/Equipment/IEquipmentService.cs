using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    public interface IEquipmentService
    {
        /// <summary>
        /// 서비스 초기화 (EquipmentTableSO 로드)
        /// </summary>
        UniTask InitializeAsync();

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 타입별 장비 목록을 가져옵니다.
        /// </summary>
        IReadOnlyList<EquipmentRow> GetByType(EquipmentType type);

        /// <summary>
        /// 코드로 장비를 가져옵니다.
        /// </summary>
        bool TryGetByCode(string code, out EquipmentRow equipment);

        /// <summary>
        /// 모든 장비 목록을 가져옵니다.
        /// </summary>
        IReadOnlyList<EquipmentRow> GetAll();

        /// <summary>
        /// 장비 타입별로 장착된 장비 코드를 반환합니다.
        /// </summary>
        string GetEquippedCode(EquipmentType type);

        /// <summary>
        /// 장비를 장착합니다. 기존 장착 장비는 해제됩니다.
        /// </summary>
        bool Equip(EquipmentType type, string equipmentCode);

        /// <summary>
        /// 장비를 해제합니다.
        /// </summary>
        bool Unequip(EquipmentType type);

        /// <summary>
        /// 장비 뽑기/보상 등으로 인벤토리에 장비를 추가합니다.
        /// </summary>
        bool AddToInventory(string equipmentCode, int count = 1);

        /// <summary>
        /// 장비의 소지 개수를 반환합니다. (0이면 보유하지 않음)
        /// </summary>
        int GetInventoryCount(string equipmentCode);

        /// <summary>
        /// 인벤토리에서 해당 장비의 정보(레벨, 개수)를 가져옵니다. 없으면 (0,0) 반환.
        /// </summary>
        EquipmentInventoryInfo GetInventoryInfo(string equipmentCode);

        /// <summary>
        /// 장비 1회 강화를 위해 필요한 동일 장비 개수를 반환합니다.
        /// </summary>
        int GetRequiredCountForUpgrade();

        /// <summary>
        /// 해당 장비 코드가 '새로 획득했지만 아직 UI에서 한 번도 본 적 없는 상태'인지 여부를 반환합니다.
        /// 인벤토리 내에 존재하고, Seen 목록에 포함되지 않은 경우 true를 반환합니다.
        /// </summary>
        bool IsNewEquipment(string equipmentCode);

        /// <summary>
        /// 해당 장비 코드를 '이미 한 번 본 장비'로 기록합니다.
        /// 이후부터는 IsNewEquipment 호출 시 NEW 대상으로 판단되지 않습니다.
        /// </summary>
        void MarkAsSeen(string equipmentCode);

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
