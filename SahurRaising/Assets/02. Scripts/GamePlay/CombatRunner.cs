using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 레전드 오브 슬라임 스타일의 전투 흐름을 관리하는 컴포넌트
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
        
        [Header("=== 디버그 ===")]
        [SerializeField] private bool _showDebugLogs = false;

        // 서비스
        private ICombatService _combatService;
        private IEventBus _eventBus;

        // 인스턴스
        private PlayerUnitView _playerInstance;
        private readonly List<UnitView> _activeMonsters = new();
        
        // 상태
        private CombatPhase _phase = CombatPhase.WaitingForInit;
        private Vector3 _playerIdlePosition;
        private Vector3 _playerAdvancedPosition;
        private int _monstersSpawnedThisWave = 0;
        private int _monstersKilledThisWave = 0;
        private float _spawnTimer = 0f;
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

            HandleTouchInput();
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                _eventBus.Unsubscribe<StageResultEvent>(OnStageResult);
            }
            
            if (_combatService != null)
            {
                _combatService.OnPlayerAttack -= OnPlayerAttack;
                _combatService.OnMonsterAttack -= OnMonsterAttack;
            }
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

            // 이벤트 구독
            if (_eventBus != null)
            {
                _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                _eventBus.Subscribe<StageResultEvent>(OnStageResult);
            }
            
            if (_combatService != null)
            {
                _combatService.OnPlayerAttack += OnPlayerAttack;
                _combatService.OnMonsterAttack += OnMonsterAttack;
            }

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

            UpdateMonsterSpawning();
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
                    _playerInstance.transform.position = Vector3.MoveTowards(
                        currentPos, 
                        _playerIdlePosition, 
                        _settings.PlayerMoveSpeed * 1.5f * Time.deltaTime 
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

        private void UpdateMonsterSpawning()
        {
            // 이번 웨이브에 스폰할 몬스터가 남아있고, 화면에 여유가 있으면 스폰
            if (_monstersSpawnedThisWave >= _settings.MonstersPerWave) return;
            if (_activeMonsters.Count >= _settings.MaxMonstersOnScreen) return;
            
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _settings.SpawnInterval)
            {
                _spawnTimer = 0f;
                SpawnMonster();
            }
        }

        private void UpdateMonsterMovement()
        {
            bool anyMonsterInRange = false;
            
            for (int i = _activeMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _activeMonsters[i];
                if (monster == null)
                {
                    _activeMonsters.RemoveAt(i);
                    continue;
                }

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
                    // 사거리 도달
                    monster.PlayMove(false); // 바퀴 회전 OFF
                    anyMonsterInRange = true;
                }
            }

            // 사거리 내 몬스터가 있으면 전투 모드로 전환
            if (anyMonsterInRange && _phase == CombatPhase.Moving)
            {
                EnterFightingPhase();
            }
        }

        private void SpawnMonster()
        {
            MonsterUnitView prefab = _settings.GetRandomMonsterPrefab();
            if (prefab == null)
            {
                Debug.LogError("[CombatRunner] 몬스터 프리팹이 없습니다! CombatSettings를 확인하세요.");
                return;
            }

            // 스폰 위치에 약간의 랜덤 오프셋 추가
            Vector3 spawnPos = _monsterSpawnPoint.position;
            spawnPos.y += Random.Range(-0.3f, 0.3f);
            
            var monster = Instantiate(prefab, spawnPos, Quaternion.identity, _monsterSpawnPoint);
            monster.Initialize();
            monster.Flip(true); // 몬스터는 왼쪽을 봄
            
            _activeMonsters.Add(monster);
            _monstersSpawnedThisWave++;
            
            LogDebug($"몬스터 스폰! ({_monstersSpawnedThisWave}/{_settings.MonstersPerWave})");
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
            _spawnTimer = _settings.SpawnInterval; // 즉시 첫 몬스터 스폰
            
            // 배경 스크롤 시작
            _backgroundScroller?.StartScrolling(_settings.BackgroundScrollSpeed);
            
            LogDebug(">>> 이동 모드 진입");
        }

        private void EnterFightingPhase()
        {
            _phase = CombatPhase.Fighting;
            
            // 배경 스크롤 정지
            _backgroundScroller?.StopScrolling();
            
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

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            LogDebug($"몬스터 처치! Stage: {evt.StageIndex}, Wave: {evt.WaveIndex}");
            
            _monstersKilledThisWave++;
            
            // 첫 번째 활성 몬스터 제거 (CombatService는 단일 타겟)
            if (_activeMonsters.Count > 0)
            {
                var deadMonster = _activeMonsters[0];
                _activeMonsters.RemoveAt(0);
                
                deadMonster?.PlayDie();
                
                // 지연 후 오브젝트 파괴
                DestroyMonsterDelayed(deadMonster).Forget();
            }
            
            // 웨이브 클리어 체크
            if (_monstersKilledThisWave >= _settings.MonstersPerWave)
            {
                LogDebug("웨이브 클리어!");
                _monstersSpawnedThisWave = 0;
                _monstersKilledThisWave = 0;
            }
            
            // 모든 몬스터가 사라지면 이동 모드로 전환
            if (_activeMonsters.Count == 0)
            {
                EnterTransitionPhase();
            }
        }

        private async UniTaskVoid DestroyMonsterDelayed(UnitView monster)
        {
            await UniTask.Delay((int)(_settings.DeathToSpawnDelay * 1000));
            
            if (monster != null)
            {
                Destroy(monster.gameObject);
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

        private void OnPlayerAttack()
        {
            _playerInstance?.PlayAttack();
        }

        private void OnMonsterAttack()
        {
            // 가장 가까운 몬스터가 공격
            if (_activeMonsters.Count > 0)
            {
                _activeMonsters[0]?.PlayAttack();
            }
        }

        #endregion

        #region Input

        private void HandleTouchInput()
        {
            if (_phase != CombatPhase.Fighting && _phase != CombatPhase.Moving) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current == null) return;
                
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    if (Input.touchCount > 0 && 
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                        return;

                    _combatService?.ApplyTouchAttack();
                }
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
