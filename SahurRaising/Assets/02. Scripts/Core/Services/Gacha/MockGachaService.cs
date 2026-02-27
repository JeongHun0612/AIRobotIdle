using System;
using System.Collections.Generic;
using System.IO;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    public class MockGachaService : MonoBehaviour
    {
        private const string SaveFileName = "gacha.json";

        private readonly IResourceService _resourceService;
        private readonly ICurrencyService _currencyService;
        private readonly IEquipmentService _equipmentService;
        private readonly IDroneService _droneService;
        private readonly IEventBus _eventBus;

        private GachaLevelConfig _levelConfig;

        // 타입별 핸들러 관리
        private readonly Dictionary<GachaType, IGachaHandler> _handlers = new();

        // 타입별 가챠 데이터 관리
        private readonly Dictionary<GachaType, GachaTypeSaveData> _gachaData = new();

        // 타입별 UI 전략 관리
        private readonly Dictionary<GachaType, IGachaResultStrategy> _resultStrategies = new();

        public bool IsInitialized { get; private set; }
        public GachaLevelConfig LevelConfig => _levelConfig;
    }
}
