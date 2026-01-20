using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 레전드 오브 슬라임 스타일의 전투 흐름을 관리하는 컴포넌트 (오케스트레이터)
    /// 
    /// 책임:
    /// - 전투 페이즈 FSM 관리
    /// - 하위 컴포넌트(MonsterSpawner, InputHandler) 조율
    /// - 배경 스크롤 및 플레이어 이동 제어
    /// 
    /// 전투 흐름:
    /// 1. 이동 중: 배경 스크롤, 플레이어 살짝 전진, 몬스터 스폰되어 접근
    /// 2. 전투 중: 배경 멈춤, 플레이어 원위치, 전투 진행
    /// 3. 처치 후: 다시 이동 모드로 전환
    /// </summary>
    public class CombatRunner : MonoBehaviour
    {
        private enum CombatPhase
        {
            WaitingForInit,     // 초기화 대기
            Moving,             // 이동 중 (배경 스크롤, 몬스터 스폰)
            Fighting,           // 전투 중 (배경 멈춤)
            TransitionToMove    // 전투→이동 전환 중
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
        [SerializeField] private bool _showDebugLogs = false;

        // 서비스
        private ICombatService _combatService;
        private IEventBus _eventBus;

        // 인스턴스
        private PlayerUnitView _playerInstance;
        
        // 현재 타겟 (개선 사항: _activeMonsters[0] 직접 접근 대신 명시적 관리)
        private UnitView _currentTarget;
        
        // 상태
        private CombatPhase _phase = CombatPhase.WaitingForInit;
        private Vector3 _playerIdlePosition;
        private Vector3 _playerAdvancedPosition;
        private int _monstersKilledThisWave = 0;
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
            
            _combatService = ServiceLocator.Get<ICombatService>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            // 설정 검증
            if (_settings == null)
            {
                Debug.LogError("[CombatRunner] CombatSettings가 할당되지 않았습니다!");
                return;
            }

            // 하위 컴포넌트 초기화
            InitializeSubComponents();
            
            // 이벤트 구독
            SubscribeEvents();

            // 플레이어 스폰
            SpawnPlayer();
            
            // 플레이어 위치 설정
            _playerIdlePosition = _playerInstance.transform.position;
            _playerAdvancedPosition = _playerIdlePosition + Vector3.right * _settings.PlayerAdvanceDistance;

            // 스테이지 시작
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
            _monsterSpawner.Initialize(_settings, _monsterSpawnPoint);
            
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
                _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                _eventBus.Subscribe<StageResultEvent>(OnStageResult);
            }
            
            if (_combatService != null)
            {
                // 새로운 통합 이벤트 사용
                _combatService.OnAttack += OnAttack;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                _eventBus.Unsubscribe<StageResultEvent>(OnStageResult);
            }
            
            if (_combatService != null)
            {
                _combatService.OnAttack -= OnAttack;
            }
        }

        #endregion

        #region Phase Updates

        /// <summary>
        /// 이동 중 상태 업데이트
        /// - 배경 스크롤 ON
        /// - 플레이어: 전진 위치(AdvancedPosition)로 이동하여 달림 (추진력 표현)
        /// </summary>
        private void UpdateMovingPhase()
        {
            if (_playerInstance != null)
            {
                // 전진 위치로 이동
                Vector3 currentPos = _playerInstance.transform.position;
                if (Vector3.Distance(currentPos, _playerAdvancedPosition) > 0.01f)
                {
                    _playerInstance.transform.position = Vector3.MoveTowards(
                        currentPos, 
                        _playerAdvancedPosition, 
                        _settings.PlayerMoveSpeed * Time.deltaTime
                    );
                }
                // 이동 중 (바퀴 회전 ON)
                _playerInstance.PlayMove(true);
            }

            UpdateMonsterMovement();
        }

        /// <summary>
        /// 전투 중 상태 업데이트
        /// - 배경 스크롤 OFF
        /// - 플레이어: 원래 위치(IdlePosition)로 복귀 (브레이크 밟는 느낌)
        /// </summary>
        private void UpdateFightingPhase()
        {
            _combatService?.Tick(Time.deltaTime);
            
            if (_playerInstance != null)
            {
                // 전투 위치(IdlePosition)로 복귀
                Vector3 currentPos = _playerInstance.transform.position;
                if (Vector3.Distance(currentPos, _playerIdlePosition) > 0.01f)
                {
                    // 브레이크 잡으며 뒤로 밀리는 연출
                    float returnSpeed = _settings.PlayerMoveSpeed * _settings.PlayerReturnSpeedMultiplier;
                    _playerInstance.transform.position = Vector3.MoveTowards(
                        currentPos, 
                        _playerIdlePosition, 
                        returnSpeed * Time.deltaTime 
                    );
                    
                    // 밀리는 중에도 공격 중이 아니면 대기 상태 (바퀴 멈춤)
                    if (!_playerInstance.IsAttacking)
                        _playerInstance.PlayMove(false);
                }
                else
                {
                    // 위치 고정
                    _playerInstance.transform.position = _playerIdlePosition;
                    
                    if (!_playerInstance.IsAttacking)
                        _playerInstance.PlayMove(false);
                }
            }
        }

        /// <summary>
        /// 전투 종료 후 전환 상태
        /// - 몬스터가 없으면 즉시 이동 모드로 전환
        /// </summary>
        private void UpdateTransitionPhase()
        {
            // 별도 복귀 로직 없이 바로 이동 모드로 전환 (이동 모드에서 전진하므로 자연스럽게 가속됨)
            EnterMovingPhase();
        }

        #endregion

        #region Monster Management

        private void UpdateMonsterMovement()
        {
            bool anyMonsterInRange = false;
            var activeMonsters = _monsterSpawner.ActiveMonsters;
            
            for (int i = activeMonsters.Count - 1; i >= 0; i--)
            {
                var monster = activeMonsters[i];
                if (monster == null) continue;

                // 몬스터가 플레이어를 향해 이동
                float distance = Vector3.Distance(monster.transform.position, _playerInstance.transform.position);
                
                if (distance > _settings.AttackRange)
                {
                    // 이동 중
                    Vector3 dir = (_playerInstance.transform.position - monster.transform.position).normalized;
                    monster.transform.position += dir * _settings.MonsterMoveSpeed * Time.deltaTime;
                    monster.PlayMove(true); // 바퀴 회전 ON
                }
                else
                {
                    // 사거리 도달 - 현재 타겟으로 설정
                    monster.PlayMove(false); // 바퀴 회전 OFF
                    anyMonsterInRange = true;
                    
                    // 가장 먼저 도달한 몬스터를 현재 타겟으로
                    if (_currentTarget == null || _currentTarget.IsDead)
                    {
                        _currentTarget = monster;
                    }
                }
            }

            // 사거리 내 몬스터가 있으면 전투 모드로 전환
            if (anyMonsterInRange && _phase == CombatPhase.Moving)
            {
                EnterFightingPhase();
            }
        }

        private void SpawnPlayer()
        {
            if (_playerInstance == null && _playerPrefab != null && _playerSpawnPoint != null)
            {
                _playerInstance = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity, _playerSpawnPoint);
                _playerInstance.Initialize();
                _playerInstance.Flip(false); // 플레이어는 오른쪽을 봄
            }
        }

        #endregion

        #region Phase Transitions

        private void EnterMovingPhase()
        {
            _phase = CombatPhase.Moving;
            
            // 배경 스크롤 시작
            _backgroundScroller?.StartScrolling(_settings.BackgroundScrollSpeed);
            
            // 몬스터 스폰 시작
            _monsterSpawner.StartSpawning();
            
            // 입력 활성화
            _inputHandler?.SetEnabled(true);
            
            LogDebug(">>> 이동 모드 진입");
        }

        private void EnterFightingPhase()
        {
            _phase = CombatPhase.Fighting;
            
            // 배경 스크롤 정지
            _backgroundScroller?.StopScrolling();
            
            // 몬스터 스폰 정지 (전투 중에는 스폰 안 함)
            _monsterSpawner.StopSpawning();
            
            // 플레이어 제자리 대기 상태 (바퀴 멈춤, 애니메이션은 Move 유지)
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
        /// 공격 이벤트 핸들러
        /// AttackType으로 공격 유형 구분, 다중 공격 시 HitIndex/IsLastHit 활용
        /// </summary>
        private void OnAttack(AttackEvent evt)
        {
            if (evt.IsPlayerAttack)
            {
                _playerInstance?.PlayAttack();
                
                // 공격 유형별 분기 처리 (확장 가능)
                switch (evt.AttackType)
                {
                    case AttackType.Touch:
                        // 터치 공격: 추가 이펙트 가능
                        break;
                    case AttackType.Auto:
                        // 자동 공격
                        break;
                    case AttackType.Skill:
                        // 스킬 공격 (추후 구현)
                        break;
                }
                
                // 크리티컬 히트 시 추가 연출
                if (evt.IsCritical)
                {
                    LogDebug($"크리티컬 히트! 데미지: {evt.Damage}");
                    // TODO: 크리티컬 이펙트 추가 가능
                }
                
                // 다중 공격 시 마지막 히트 처리
                if (evt.IsLastHit && evt.HitIndex > 0)
                {
                    LogDebug($"다중 공격 완료! 총 {evt.HitIndex + 1}회 공격");
                }
            }
            else
            {
                // 몬스터 공격
                _currentTarget?.PlayAttack();
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            LogDebug($"몬스터 처치! Stage: {evt.StageIndex}, Wave: {evt.WaveIndex}");
            
            _monstersKilledThisWave++;
            
            // 현재 타겟 처리
            if (_currentTarget != null)
            {
                var deadMonster = _currentTarget;
                _currentTarget = null; // 타겟 해제
                
                deadMonster.PlayDie();
                
                // 지연 후 풀로 반환
                ReleaseMonsterDelayed(deadMonster).Forget();
            }
            
            // 웨이브 클리어 체크
            if (_monstersKilledThisWave >= _settings.MonstersPerWave)
            {
                LogDebug("웨이브 클리어!");
                _monsterSpawner.ResetWave();
                _monstersKilledThisWave = 0;
            }
            
            // 모든 몬스터가 사라지면 이동 모드로 전환
            if (_monsterSpawner.ActiveMonsterCount == 0)
            {
                EnterTransitionPhase();
            }
            else
            {
                // 다음 타겟 설정
                _currentTarget = _monsterSpawner.GetCurrentTarget();
            }
        }

        private async UniTaskVoid ReleaseMonsterDelayed(UnitView monster)
        {
            await UniTask.Delay((int)(_settings.DeathToSpawnDelay * 1000));
            
            if (monster != null)
            {
                _monsterSpawner.ReleaseMonster(monster);
            }
        }

        private void OnStageResult(StageResultEvent evt)
        {
            if (evt.IsClear)
            {
                LogDebug("스테이지 클리어!");
            }
            else
            {
                LogDebug("스테이지 실패!");
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
