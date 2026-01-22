using System;
using System.Collections.Generic;
using System.IO;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 재화 잔액 관리 및 저장/로드, 미접속 보상 계산을 담당
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        private const string SaveFileName = "currency.json";
        private const double BaseGoldPerSecond = 5.0; // 밸런스 확정 시 조정
        private const double DefaultOfflineMinutes = 360d; // 테이블 미적용 시 안전 기본값(분)

        private readonly Dictionary<CurrencyType, BigDouble> _balances = new();
        private readonly IEventBus _eventBus;
        private readonly IStatService _statService;
        private readonly IResourceService _resourceService;

        private long _lastSavedUnix;
        private CurrencyTable _currencyTable;

        public CurrencyService(IEventBus eventBus, IStatService statService, IResourceService resourceService)
        {
            _eventBus = eventBus;
            _statService = statService;
            _resourceService = resourceService;
        }

        public async UniTask InitializeAsync()
        {
            _currencyTable = await _resourceService.LoadTableAsync<CurrencyTable>("CurrencyTable");
            if (_currencyTable == null)
            {
                Debug.LogError("[CurrencyService] CurrencyTable load failed.");
            }

            InitializeDefaults();
            await LoadAsync();
        }

        public CurrencyData GetCurrencyData(CurrencyType type)
        {
            if (_currencyTable != null && _currencyTable.Index.TryGetValue(type, out var data))
            {
                return data;
            }
            return default;
        }

        public BigDouble Get(CurrencyType type)
        {
            return _balances.TryGetValue(type, out var value) ? value : BigDouble.Zero;
        }

        public bool TryConsume(CurrencyType type, BigDouble amount, string reason = null)
        {
            if (amount <= 0)
                return true;

            if (!_balances.TryGetValue(type, out var current) || current < amount)
                return false;

            _balances[type] = current - amount;

            _eventBus?.Publish(new CurrencyConsumedEvent
            {
                CurrencyType = type,
                Amount = amount,
                Reason = reason ?? "Unknown"
            });

            return true;
        }

        public void Add(CurrencyType type, BigDouble amount, string reason = null)
        {
            if (amount <= 0)
                return;

            if (_balances.TryGetValue(type, out var current))
                _balances[type] = current + amount;
            else
                _balances[type] = amount;

            _eventBus?.Publish(new RewardGrantedEvent
            {
                CurrencyType = type,
                Amount = amount,
                Source = reason ?? "Unknown"
            });
        }

        public OfflineRewardInfo? GetOfflineRewardInfo()
        {
            if (_lastSavedUnix <= 0)
                return null;

            var elapsedSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _lastSavedUnix;
            if (elapsedSeconds <= 0)
                return null;

            var snapshot = _statService?.GetSnapshot() ?? default;

            // OFFT는 스탯 테이블에 이미 합산된 누적 분 단위 값으로 가정한다.
            var cappedMinutes = snapshot.OfflineTimeMinutes > 0
                ? snapshot.OfflineTimeMinutes
                : DefaultOfflineMinutes;
            var maxSeconds = (long)(cappedMinutes * 60d);
            var clampedSeconds = Math.Min(elapsedSeconds, maxSeconds);

            if (clampedSeconds <= 0)
                return null;

            var reward = new BigDouble(BaseGoldPerSecond * clampedSeconds);
            var bonus = 1 + snapshot.OfflineAmountRate;
            var goldRate = 1 + snapshot.GoldBonusRate;

            reward *= bonus;
            reward *= goldRate;

            Debug.Log($"[CurrencyService] 미접속 보상 계산: 경과 {elapsedSeconds}s → {clampedSeconds}s, base {BaseGoldPerSecond}/s, bonus {bonus}, goldRate {goldRate}, 결과 {reward}");

            return new OfflineRewardInfo
            {
                RewardAmount = reward,
                ElapsedSeconds = elapsedSeconds,
                ClampedSeconds = clampedSeconds,
                MaxSeconds = maxSeconds
            };
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new CurrencySaveData
                {
                    Gold = Get(CurrencyType.Gold).ToString(),
                    Emerald = Get(CurrencyType.Emerald).ToString(),
                    Diamond = Get(CurrencyType.Diamond).ToString(),
                    Ticket = Get(CurrencyType.Ticket).ToString(),
                    Ruby = Get(CurrencyType.Ruby).ToString(),
                    LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                _lastSavedUnix = data.LastSavedUnix;
                Debug.Log($"[CurrencyService] 저장 완료: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurrencyService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[CurrencyService] 저장 파일이 없어 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<CurrencySaveData>(json);

                _balances[CurrencyType.Gold] = ParseBigDouble(data.Gold);
                _balances[CurrencyType.Emerald] = ParseBigDouble(data.Emerald);
                _balances[CurrencyType.Diamond] = ParseBigDouble(data.Diamond);
                _balances[CurrencyType.Ticket] = ParseBigDouble(data.Ticket);
                _balances[CurrencyType.Ruby] = ParseBigDouble(data.Ruby);
                _lastSavedUnix = data.LastSavedUnix;

                //var offlineRewardInfo = GetOfflineRewardInfo();
                //if (offlineRewardInfo.HasValue)
                //{
                //    var offlineReward = offlineRewardInfo.Value.RewardAmount;
                //    Add(CurrencyType.Gold, offlineReward, "OfflineReward");
                //    Debug.Log($"[CurrencyService] 미접속 보상 지급: {offlineReward}");
                //}
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurrencyService] 로드 실패: {ex.Message}");
                InitializeDefaults();
            }
        }

        private void InitializeDefaults()
        {
            _balances[CurrencyType.Gold] = BigDouble.Zero;
            _balances[CurrencyType.Emerald] = BigDouble.Zero;
            _balances[CurrencyType.Diamond] = BigDouble.Zero;
            _balances[CurrencyType.Ticket] = BigDouble.Zero;
            _balances[CurrencyType.Ruby] = BigDouble.Zero;
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        private BigDouble ParseBigDouble(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return BigDouble.Zero;

            try
            {
                return BigDouble.Parse(raw);
            }
            catch
            {
                return BigDouble.Zero;
            }
        }
    }
}


