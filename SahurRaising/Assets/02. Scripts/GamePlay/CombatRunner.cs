using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 레전드 오브 슬라임 스타일의 전투 흐름을 관리하는 컴포넌트 (오케스트레이터)
    /// 
    /// 다수 몬스터 동시 전투 지원:
    /// - MaxTargetCount에 따라 동시에 여러 몬스터 공격
    /// - 몬스터 간 간격 유지 (겹침 방지)
    /// - 간격 대기 중인 몬스터는 Idle_A 애니메이션
    /// 
    /// 전투 흐름:
    /// 1. 이동 중: 배경 스크롤, 플레이어 전진, 몬스터 스폰
    /// 2. 전투 중: 배경 멈춤, 플레이어 원위치, 다수 타겟 공격
    /// 3. 처치 후: 다시 이동 모드로 전환
    /// </summary>
    public class CombatRunner : MonoBehaviour
    {
        private enum CombatPhase
        {
            WaitingForInit,
            Moving,
            Fighting,
            TransitionToMove
        }

        [Header("=== 필수 레퍼런스 ===")]
        [SerializeField] private CombatSettings _settings;
        [SerializeField] private BackgroundScroller _backgroundScroller;
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform _monsterSpawnPoint;
        [SerializeField] private PlayerUnitView _playerPrefab;

        [Header("=== 하위 컴포넌트 ===")]
        [SerializeField] private MonsterSpawner _monsterSpawner;
        [SerializeField] private CombatInputHandler _inputHandler;

        [Header("=== 디버그 ===")]
        [SerializeField] private bool _showDebugLogs = true;

        // 서비스
        private CombatService _combatService;
        private IEventBus _eventBus;

        // 인스턴스
        private PlayerUnitView _playerInstance;

        // 상태
        private CombatPhase _phase = CombatPhase.WaitingForInit;
        private Vector3 _playerIdlePosition;
        private Vector3 _playerAdvancedPosition;
        private int _monstersKilledThisWave = 0;
        private int _currentWaveIndex = 1;
        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            switch (_phase)
            {
                case CombatPhase.Moving:
                    UpdateMovingPhase();
                    break;

                case CombatPhase.Fighting:
                    UpdateFightingPhase();
                    break;

                case CombatPhase.TransitionToMove:
                    UpdateTransitionPhase();
                    break;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region Initialization

        private async UniTaskVoid InitializeAsync()
        {
            LogDebug("초기화 시작 - GameManager 대기 중...");

            await UniTask.WaitUntil(() =>
                GameManager.Instance != null &&
                GameManager.Instance.IsServicesInitialized &&
                GameManager.Instance.IsGameStarted);

            LogDebug("GameManager 초기화 완료 - 서비스 연결 중...");

            _combatService = ServiceLocator.Get<ICombatService>() as CombatService;
            _eventBus = ServiceLocator.Get<IEventBus>();

            if (_settings == null)
            {
                Debug.LogError("[CombatRunner] CombatSettings가 할당되지 않았습니다!");
                return;
            }

            // 플레이어 스폰
            SpawnPlayer();

            // 플레이어 위치 설정
            _playerIdlePosition = _playerInstance.transform.position;
            _playerAdvancedPosition = _playerIdlePosition + Vector3.right * _settings.PlayerAdvanceDistance;

            // 하위 컴포넌트 초기화
            InitializeSubComponents();

            // 이벤트 구독
            SubscribeEvents();

            // CombatSettings의 웨이브 수를 CombatService에 주입
            _combatService.SetWavesPerStage(_settings.WavesPerStage);

            // 스테이지 시작 TODO 데이터에서 불러오는거로 변경 예정
            await _combatService.StartStageAsync(1);

            _isInitialized = true;

            // 이동 모드로 시작
            EnterMovingPhase();

            LogDebug("초기화 완료 - 전투 시작!");
        }

        private void InitializeSubComponents()
        {
            // MonsterSpawner 초기화
            if (_monsterSpawner == null)
            {
                _monsterSpawner = GetComponent<MonsterSpawner>();
                if (_monsterSpawner == null)
                {
                    _monsterSpawner = gameObject.AddComponent<MonsterSpawner>();
                }
            }
            _monsterSpawner.Initialize(_settings, _monsterSpawnPoint, _playerInstance.transform, _combatService);

            // 몬스터 이벤트 구독
            _monsterSpawner.OnMonsterKilled += OnMonsterKilledHandler;

            // InputHandler 초기화
            if (_inputHandler == null)
            {
                _inputHandler = GetComponent<CombatInputHandler>();
                if (_inputHandler == null)
                {
                    _inputHandler = gameObject.AddComponent<CombatInputHandler>();
                }
            }
            _inputHandler.Initialize(_combatService);
        }

        private void SubscribeEvents()
        {
            if (_eventBus != null)
            {
                _eventBus.Subscribe<StageResultEvent>(OnStageResult);
            }

            if (_combatService != null)
            {
                _combatService.OnAttack += OnAttack;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<StageResultEvent>(OnStageResult);
            }

            if (_combatService != null)
            {
                _combatService.OnAttack -= OnAttack;
            }

            if (_monsterSpawner != null)
            {
                _monsterSpawner.OnMonsterKilled -= OnMonsterKilledHandler;
            }
        }

        #endregion

        #region Phase Updates

        /// <summary>
        /// 이동 중 상태 업데이트
        /// </summary>
        private void UpdateMovingPhase()
        {
            if (_playerInstance != null)
            {
                Vector3 currentPos = _playerInstance.transform.position;
                if (Vector3.Distance(currentPos, _playerAdvancedPosition) > 0.01f)
                {
                    _playerInstance.transform.position = Vector3.MoveTowards(
                        currentPos,
                        _playerAdvancedPosition,
                        _settings.PlayerMoveSpeed * Time.deltaTime
                    );
                }
                _playerInstance.PlayMove(true);
            }

            UpdateMonstersLogic();

            // 전투 중인 몬스터가 있으면 전투 모드로 전환
            if (_monsterSpawner.EngagedMonsterCount > 0)
            {
                EnterFightingPhase();
            }
        }

        /// <summary>
        /// 전투 중 상태 업데이트
        /// </summary>
        private void UpdateFightingPhase()
        {
            // 전투 중인 몬스터 수를 전달하여 몬스터 공격 여부 결정
            _combatService?.Tick(Time.deltaTime, _monsterSpawner.EngagedMonsterCount);

            if (_playerInstance != null)
            {
                Vector3 currentPos = _playerInstance.transform.position;
                if (Vector3.Distance(currentPos, _playerIdlePosition) > 0.01f)
                {
                    float returnSpeed = _settings.PlayerMoveSpeed * _settings.PlayerReturnSpeedMultiplier;
                    _playerInstance.transform.position = Vector3.MoveTowards(
                        currentPos,
                        _playerIdlePosition,
                        returnSpeed * Time.deltaTime
                    );

                    if (!_playerInstance.IsAttacking)
                        _playerInstance.PlayMove(false);
                }
                else
                {
                    _playerInstance.transform.position = _playerIdlePosition;

                    if (!_playerInstance.IsAttacking)
                        _playerInstance.PlayMove(false);
                }
            }

            // 몬스터 이동 업데이트
            UpdateMonstersLogic();

            // 전투 중인 몬스터가 없으면 이동 모드로 전환
            if (_monsterSpawner.EngagedMonsterCount == 0 && _monsterSpawner.ActiveMonsterCount == 0)
            {
                EnterTransitionPhase();
            }
        }

        /// <summary>
        /// 전환 상태
        /// </summary>
        private void UpdateTransitionPhase()
        {
            EnterMovingPhase();
        }

        #endregion

        #region Monster Management

        private void UpdateMonstersLogic()
        {
            var activeMonsters = _monsterSpawner.ActiveMonsters;
            if (activeMonsters.Count == 0) return;

            float playerX = _playerInstance.transform.position.x;
            float playerY = _playerInstance.transform.position.y;

            // 1. 거리순 정렬 (플레이어에게 가까운 순서)
            // 매 프레임 정렬 비용이 있지만, 몬스터 수가 적으므로(최대 30마리 미만) 안정성을 위해 수행
            // ToList()로 복사본을 생성하여 순회 중 리스트 변경 문제 방지
            var sortedMonsters = activeMonsters
                .OrderBy(m => Vector3.Distance(m.transform.position, _playerInstance.transform.position))
                .ToList();

            for (int i = 0; i < sortedMonsters.Count; i++)
            {
                var monster = sortedMonsters[i];
                if (monster == null || monster.IsDead) continue;

                Vector3 currentPos = monster.transform.position;
                float distToPlayer = Vector3.Distance(currentPos, _playerInstance.transform.position);

                // 1. 전투 진입 판정
                float engageRange = _settings.AttackRange;

                // 이미 전투 중인 경우
                if (_monsterSpawner.EngagedMonsters.Contains(monster))
                {
                    monster.PlayMove(false);
                    continue;
                }

                // 2. 전투 진입 시도
                if (distToPlayer <= engageRange)
                {
                    if (_monsterSpawner.TryEngageMonster(monster))
                    {
                        monster.PlayMove(false);
                        monster.MarkEnteredCombat();
                        continue;
                    }
                    else
                    {
                        // 자리가 없어서 대기
                        monster.SetWaitingForSpace(true);
                        monster.PlayMove(false);
                        continue;
                    }
                }

                // 3. 이동 로직 (전투 중이 아님)
                bool shouldWaitForSpacing = false;

                // 내 앞에 몬스터가 있는가? (sortedMonsters[i-1])
                if (i > 0)
                {
                    var frontMonster = sortedMonsters[i - 1];
                    // 앞 몬스터와의 X 거리 계산 (절대값)
                    float distToFront = Mathf.Abs(currentPos.x - frontMonster.transform.position.x);

                    // [히스테리시스 적용]
                    // 이미 대기 중이라면, 해제하기 위해 더 넓은 간격(Spacing + Margin)이 필요함
                    // 대기 중이 아니라면, 멈추기 위해 Spacing보다 가까워져야 함
                    float spacingThreshold = _settings.MonsterXSpacing;

                    // 떨림 방지용 여유폭 (0.5f)
                    if (monster.IsWaitingForSpace)
                    {
                        spacingThreshold += 0.5f;
                    }

                    if (distToFront < spacingThreshold)
                    {
                        shouldWaitForSpacing = true;
                    }
                }

                if (shouldWaitForSpacing)
                {
                    monster.SetWaitingForSpace(true);
                    monster.PlayMove(false);
                }
                else
                {
                    // 이동
                    monster.SetWaitingForSpace(false);

                    // 목표 Y 계산
                    float targetY = playerY + monster.TargetYOffset;
                    targetY = _settings.ClampY(targetY);

                    Vector3 targetPos = new Vector3(playerX, targetY, currentPos.z);
                    Vector3 direction = (targetPos - currentPos).normalized;

                    // 이동
                    currentPos += direction * _settings.MonsterMoveSpeed * Time.deltaTime;
                    currentPos.y = _settings.ClampY(currentPos.y);

                    monster.transform.position = currentPos;
                    monster.PlayMove(true);
                }
            }
        }
        private void SpawnPlayer()
        {
            if (_playerInstance == null && _playerPrefab != null && _playerSpawnPoint != null)
            {
                _playerInstance = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity, _playerSpawnPoint);
                _playerInstance.Initialize();
                _playerInstance.Flip(false);
            }
        }

        #endregion

        #region Phase Transitions

        private void EnterMovingPhase()
        {
            _phase = CombatPhase.Moving;

            _backgroundScroller?.StartScrolling(_settings.BackgroundScrollSpeed);
            _monsterSpawner.StartSpawning(_currentWaveIndex);
            _inputHandler?.SetEnabled(true);

            LogDebug(">>> 이동 모드 진입");
        }

        private void EnterFightingPhase()
        {
            _phase = CombatPhase.Fighting;

            _backgroundScroller?.StopScrolling();
            // 전투 중에도 스폰은 계속 (MaxMonstersOnScreen까지)

            _playerInstance?.PlayMove(false);

            LogDebug(">>> 전투 모드 진입");
        }

        private void EnterTransitionPhase()
        {
            _phase = CombatPhase.TransitionToMove;
            LogDebug(">>> 전환 모드 진입");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 공격 이벤트 핸들러 - 다수 타겟 공격 처리
        /// </summary>
        private void OnAttack(AttackEvent evt)
        {
            if (evt.IsPlayerAttack)
            {
                _playerInstance?.PlayAttack();

                // 전투 중인 모든 몬스터에게 데미지
                var targets = _monsterSpawner.GetEngagedTargets();
                int maxTargets = _combatService.GetMaxTargetCount();
                int targetCount = Mathf.Min(targets.Count, maxTargets);

                for (int i = 0; i < targetCount; i++)
                {
                    var monster = targets[i];
                    if (monster == null || monster.IsDead) continue;

                    // 데미지 적용
                    var defenseIgnore = _combatService.GetDefenseIgnoreRate();
                    var actualDamage = monster.TakeDamage(evt.Damage, defenseIgnore);

                    LogDebug($"몬스터 {i} 에게 {actualDamage} 데미지! (HP: {monster.CurrentHp}/{monster.MaxHp})");

                    // 몬스터 사망 체크
                    if (monster.CurrentHp <= 0)
                    {
                        HandleMonsterDeath(monster);
                    }
                    else
                    {
                        // 피격 애니메이션 (있다면)
                        // monster.PlayHit();
                    }
                }

                if (evt.IsCritical)
                {
                    LogDebug($"크리티컬 히트! 데미지: {evt.Damage}");
                }
            }
            else
            {
                // 몬스터 공격 - 현재 전투 중인 몬스터가 플레이어 공격
                var currentTarget = _monsterSpawner.GetCurrentTarget();
                if (currentTarget != null)
                {
                    currentTarget.PlayAttack();

                    // 플레이어에게 데미지
                    var spawnInfo = _combatService.GetMonsterSpawnInfo();
                    _combatService.DealDamageToPlayer(spawnInfo.Attack);
                }
            }
        }

        /// <summary>
        /// 몬스터 사망 처리
        /// </summary>
        private void HandleMonsterDeath(MonsterUnitView monster)
        {
            LogDebug($"몬스터 처치! Level: {monster.MonsterLevel}, Kind: {monster.MonsterKind}");

            monster.PlayDie();

            // 보상 지급 (CombatService에서 처리)
            var spawnInfo = _combatService.GetMonsterSpawnInfo();
            _combatService.OnMonsterKilled(monster.MonsterKind, spawnInfo.GoldReward);

            _monstersKilledThisWave++;

            // 풀 반환 처리
            _monsterSpawner.HandleMonsterDeath(monster);

            // 현재 웨이브의 몬스터 수 확인
            int monstersPerWave = _settings.GetMonstersPerWave(_currentWaveIndex);

            // 웨이브 클리어 체크
            _combatService.CheckWaveComplete(monstersPerWave);

            if (_monstersKilledThisWave >= monstersPerWave)
            {
                LogDebug($"웨이브 {_currentWaveIndex} 클리어!");
                _currentWaveIndex++;
                _monsterSpawner.ResetWave();
                _monstersKilledThisWave = 0;
            }
        }

        private void OnMonsterKilledHandler(MonsterUnitView monster)
        {
            // MonsterSpawner에서 호출되는 이벤트 (추가 처리 필요 시)
        }

        private void OnStageResult(StageResultEvent evt)
        {
            if (evt.IsClear)
            {
                LogDebug($"스테이지 {evt.StageIndex} 클리어!");
            }
            else
            {
                LogDebug($"스테이지 {evt.StageIndex} 실패!");
            }
        }

        #endregion

        #region Debug

        private void LogDebug(string message)
        {
            if (_showDebugLogs)
            {
                Debug.Log($"[CombatRunner] {message}");
            }
        }

        #endregion
    }
}
