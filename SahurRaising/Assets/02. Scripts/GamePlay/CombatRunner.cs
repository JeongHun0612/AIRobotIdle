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
        [SerializeField] private SpriteRenderer _backgroundRenderer; // 맵 경계 자동 계산용
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
        
        // 웨이브 전환 상태
        private bool _isWaveTransitioning = false;
        private float _waveTransitionTimer = 0f;
        private int _nextWaveIndex = 1;

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

            // 배경 렌더러를 CombatSettings에 연결 (자동 맵 경계 계산용)
            if (_backgroundRenderer != null)
            {
                _settings.SetBackgroundRenderer(_backgroundRenderer);
                LogDebug($"맵 경계 자동 계산 활성화: Y범위 = {_settings.MapBoundsYMin:F1} ~ {_settings.MapBoundsYMax:F1}");
            }
            else
            {
                LogDebug("배경 렌더러 미설정 - 수동 맵 경계 사용");
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
            // InputHandler에 전투 중인 몬스터 수 전달
            _inputHandler?.UpdateEngagedMonsterCount(_monsterSpawner.EngagedMonsterCount);
            
            // 웨이브 전환 중이면 타이머 처리
            if (_isWaveTransitioning)
            {
                _waveTransitionTimer -= Time.deltaTime;
                
                if (_waveTransitionTimer <= 0f)
                {
                    // 전환 완료 - 다음 웨이브 스폰 시작
                    _isWaveTransitioning = false;
                    _currentWaveIndex = _nextWaveIndex;
                    _monsterSpawner.ResetWave();
                    _monsterSpawner.StartSpawning(_currentWaveIndex);
                    LogDebug($">>> 웨이브 {_currentWaveIndex} 스폰 시작!");
                }
            }

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
                
                // 공격 중이 아닐 때만 이동 애니메이션 재생
                if (!_playerInstance.IsAttacking)
                {
                    _playerInstance.PlayMove(true);
                }
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

            // InputHandler에 전투 중인 몬스터 수 전달 (터치 공격 조건 검사용)
            _inputHandler?.UpdateEngagedMonsterCount(_monsterSpawner.EngagedMonsterCount);

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

                    // 공격 중이 아닐 때만 이동 애니메이션 재생
                    if (!_playerInstance.IsAttacking)
                        _playerInstance.PlayMove(false);
                }
                else
                {
                    _playerInstance.transform.position = _playerIdlePosition;

                    // 공격 중이 아닐 때만 Idle 애니메이션 재생
                    if (!_playerInstance.IsAttacking)
                        _playerInstance.PlayMove(false);
                }
            }

            // 몬스터 이동 업데이트
            UpdateMonstersLogic();

            // 전투 중인 몬스터가 없으면 이동 모드로 전환
            // 단, 공격 애니메이션이 재생 중이면 대기
            if (_monsterSpawner.EngagedMonsterCount == 0 && _monsterSpawner.ActiveMonsterCount == 0)
            {
                // 공격 중이면 전환 보류
                if (_playerInstance != null && _playerInstance.IsAttacking)
                    return;
                
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
            var sortedMonsters = activeMonsters
                .OrderBy(m => Vector3.Distance(m.transform.position, _playerInstance.transform.position))
                .ToList();

            for (int i = 0; i < sortedMonsters.Count; i++)
            {
                var monster = sortedMonsters[i];
                if (monster == null || monster.IsDead) continue;

                Vector3 currentPos = monster.transform.position;
                
                // X축 거리만으로 전투 진입 판정 (Y축 차이로 인해 전투에 못 들어가는 문제 해결)
                float xDistToPlayer = Mathf.Abs(currentPos.x - playerX);
                float engageRange = _settings.AttackRange;

                // 이미 전투 중인 경우: Y축 고정, X 이동만
                if (_monsterSpawner.EngagedMonsters.Contains(monster))
                {
                    monster.PlayMove(false);
                    continue;
                }

                // 2. Y축 수렴 체크
                bool useYConvergence = _settings.EnableYConvergence;
                bool isYWithinCombatRange = _settings.IsWithinCombatYRange(currentPos.y);

                // 3. 전투 진입 시도
                // 조건: X거리가 사거리 내 (Y축 수렴이 켜져있다면 Y범위 체크도 수행, 꺼져있다면 무시)
                bool canEngage = xDistToPlayer <= engageRange;
                if (useYConvergence && !isYWithinCombatRange)
                {
                    // Y축 수렴이 켜져있는데 아직 범위 밖이라면 전투 진입 보류
                    // 단, X거리가 매우 가까우면(0.5f) Y조건 무시하고 강제 진입 (무한 루프 방지)
                    if (xDistToPlayer < 0.5f)
                    {
                        // 강제 진입 허용 (로그는 남김)
                        // if (i == 0 && _showDebugLogs && Time.frameCount % 60 == 0) Debug.Log("[CombatRunner] Force Engaging due to close X distance");
                    }
                    else
                    {
                        canEngage = false; 
                    }
                }

                // 디버그: 가장 가까운 몬스터(i==0)의 상태 로깅
                if (i == 0 && _showDebugLogs)
                {
                    // 너무 자주 로그가 남지 않도록 60프레임마다 한번만
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[CombatRunner] Closest Monster: Dist={xDistToPlayer:F2}, Range={engageRange:F2}, " +
                                  $"Y={currentPos.y:F2}, InYRange={isYWithinCombatRange}, UseY={useYConvergence}, " +
                                  $"CanEngage={canEngage}, EngagedCount={_monsterSpawner.EngagedMonsterCount}");
                    }
                }

                if (canEngage)
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

                // 4. 이동 로직 (전투 중이 아님)
                bool shouldWaitForSpacing = false;

                // 내 앞에 몬스터가 있는가? (sortedMonsters[i-1])
                if (i > 0)
                {
                    var frontMonster = sortedMonsters[i - 1];
                    float distToFront = Mathf.Abs(currentPos.x - frontMonster.transform.position.x);
                    float spacingThreshold = _settings.MonsterXSpacing;

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
                    
                    // 대기 중일 때도 Y축 수렴은 진행 (옵션이 켜져있다면)
                    if (useYConvergence && !isYWithinCombatRange)
                    {
                        float targetY = _settings.ClampToCombatY(currentPos.y);
                        float yMoveSpeed = _settings.MonsterMoveSpeed * _settings.YConvergeSpeedMultiplier;
                        currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, yMoveSpeed * Time.deltaTime);
                        currentPos.y = _settings.ClampY(currentPos.y);
                        monster.transform.position = currentPos;
                    }
                    
                    monster.PlayMove(false);
                }
                else
                {
                    // 이동
                    monster.SetWaitingForSpace(false);

                    // 목표 X는 플레이어 방향
                    float xDirection = Mathf.Sign(playerX - currentPos.x);
                    float newX = currentPos.x + xDirection * _settings.MonsterMoveSpeed * Time.deltaTime;
                    float newY = currentPos.y;

                    // Y축 이동 (옵션이 켜져있을 때만)
                    if (useYConvergence)
                    {
                        // 목표 Y 계산: 전투 범위 내로 수렴
                        float targetY = playerY + monster.TargetYOffset;
                        targetY = _settings.ClampToCombatY(targetY);
                        targetY = _settings.ClampY(targetY);

                        float yMoveSpeed = _settings.MonsterMoveSpeed * _settings.YConvergeSpeedMultiplier;
                        newY = Mathf.MoveTowards(currentPos.y, targetY, yMoveSpeed * Time.deltaTime);
                        newY = _settings.ClampY(newY);
                    }

                    currentPos = new Vector3(newX, newY, currentPos.z);
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
                // 전투 중인 몬스터 목록을 스냅샷으로 복사 (반복 중 리스트 변경 방지)
                var originalTargets = _monsterSpawner.GetEngagedTargets();
                int maxTargets = _combatService.GetMaxTargetCount();
                int targetCount = Mathf.Min(originalTargets.Count, maxTargets);
                
                // 안전한 복사본 생성 (인덱스 범위 오류 방지)
                var safeTargets = new List<MonsterUnitView>(targetCount);
                for (int i = 0; i < targetCount && i < originalTargets.Count; i++)
                {
                    if (originalTargets[i] != null && !originalTargets[i].IsDead)
                    {
                        safeTargets.Add(originalTargets[i]);
                    }
                }
                
                // 공격 대상이 없으면 공격 애니메이션도 재생하지 않음 (허공 공격 방지)
                if (safeTargets.Count == 0)
                {
                    return;
                }
                
                // 공격 대상이 있을 때만 공격 애니메이션 재생
                _playerInstance?.PlayAttack();

                foreach (var monster in safeTargets)
                {
                    // 데미지 적용
                    var defenseIgnore = _combatService.GetDefenseIgnoreRate();
                    var actualDamage = monster.TakeDamage(evt.Damage, defenseIgnore);

                    LogDebug($"몬스터에게 {actualDamage} 데미지! (HP: {monster.CurrentHp}/{monster.MaxHp})");

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

            // 현재 웨이브의 패턴 수
            int patternsPerWave = _settings.GetPatternsPerWave(_currentWaveIndex);

            // 웨이브 클리어 체크: 패턴이 모두 완료되고 활성 몬스터가 없으면 클리어
            bool allPatternsCompleted = _monsterSpawner.PatternsCompletedThisWave >= patternsPerWave;
            bool noActiveMonsters = _monsterSpawner.ActiveMonsterCount == 0;

            LogDebug($"웨이브 상태 체크: 완료 패턴={_monsterSpawner.PatternsCompletedThisWave}/{patternsPerWave}, " +
                     $"활성 몬스터={_monsterSpawner.ActiveMonsterCount}, 전투 중={_monsterSpawner.EngagedMonsterCount}");

            if (allPatternsCompleted && noActiveMonsters)
            {
                LogDebug($">>> 웨이브 {_currentWaveIndex} 클리어! (패턴 {patternsPerWave}개 완료, 처치 몬스터: {_monstersKilledThisWave})");
                
                // CombatService에 웨이브 완료 알림
                _combatService.NotifyWaveComplete();
                
                // 다음 웨이브 준비 (이동 페이즈를 거치므로 즉시 시작하지 않음)
                _nextWaveIndex = _currentWaveIndex + 1;
                _monstersKilledThisWave = 0;
                
                // 웨이브 전환 타이머 설정
                _isWaveTransitioning = true;
                _waveTransitionTimer = _settings.MinWaveTransitionTime;
                
                // 이동 페이즈로 전환 (스폰은 타이머 완료 후)
                _monsterSpawner.StopSpawning();
                
                LogDebug($">>> 웨이브 전환 시작 - {_waveTransitionTimer}초 후 웨이브 {_nextWaveIndex} 시작");
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
