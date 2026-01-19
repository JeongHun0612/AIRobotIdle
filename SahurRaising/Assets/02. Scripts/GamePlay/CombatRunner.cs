using UnityEngine;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    public class CombatRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform _monsterSpawnPoint;
        
        [Header("Prefabs")]
        [SerializeField] private UnitView _playerPrefab;
        [SerializeField] private UnitView _monsterPrefab; 
        
        [Header("Settings")]
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _attackRange = 1.5f;

        private ICombatService _combatService;
        private IEventBus _eventBus;

        private UnitView _playerInstance;
        private UnitView _monsterInstance;
        
        private bool _isInCombatRange = false;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // GameManager가 서비스를 등록할 때까지 대기
            await UniTask.WaitUntil(() => ServiceLocator.HasService<ICombatService>());

            _combatService = ServiceLocator.Get<ICombatService>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            if (_eventBus != null)
            {
                _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                _eventBus.Subscribe<StageResultEvent>(OnStageResult);
            }
            
            if (_combatService != null)
            {
                _combatService.OnPlayerAttack += OnPlayerAttack;
            }

            SpawnPlayer();
            SpawnMonster(); 

            await _combatService.StartStageAsync(1); 
        }

        private void Update()
        {
            if (_combatService == null || _monsterInstance == null || _playerInstance == null)
                return;

            // 1. 거리 체크 및 이동 처리
            float distance = Vector3.Distance(_playerInstance.transform.position, _monsterInstance.transform.position);
            
            if (distance > _attackRange)
            {
                // 이동 중
                _isInCombatRange = false;
                
                // 서로를 향해 이동 (양방향 돌격)
                Vector3 playerToMonster = (_monsterInstance.transform.position - _playerInstance.transform.position).normalized;
                Vector3 monsterToPlayer = (_playerInstance.transform.position - _monsterInstance.transform.position).normalized;

                // 플레이어 이동
                _playerInstance.transform.position += playerToMonster * _moveSpeed * Time.deltaTime;
                
                // 몬스터 이동
                _monsterInstance.transform.position += monsterToPlayer * _moveSpeed * Time.deltaTime;
                
                // 애니메이션: 이동 (Idle을 Move로 사용 중)
                _monsterInstance.SetMove(true);
                _playerInstance.SetMove(true); 
            }
            else
            {
                // 전투 범위 도달
                _isInCombatRange = true;
                // _monsterInstance.SetMove(false); // 공격 애니메이션 방해 금지
                // _playerInstance.SetMove(false);
                
                // 2. 전투 로직 실행 (사거리 안에서만 딜 계산)
                _combatService.Tick(Time.deltaTime);
            }

            // 3. 터치 공격 처리
            HandleTouchInput();
        }
        
        private void HandleTouchInput()
        {
           if (Input.GetMouseButtonDown(0))
           {
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                        return;

                    // 터치 공격은 사거리 무관하게 가능하다고 가정 (원거리 마법?) 
                    // 혹은 사거리 체크를 넣어도 됨. 여기선 즉시 발동.
                    _combatService.ApplyTouchAttack();
                }
           }
        }
        
        private void OnPlayerAttack()
        {
            // 서비스에서 공격이 발생했을 때 뷰에 애니메이션 요청
            Debug.Log("[CombatRunner] OnPlayerAttack Received - Calling PlayAttack on PlayerInstance");
            _playerInstance?.PlayAttack();
        }

        private void SpawnPlayer()
        {
            if (_playerInstance == null && _playerPrefab != null && _playerSpawnPoint != null)
            {
                _playerInstance = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity, _playerSpawnPoint);
                _playerInstance.Initialize();
                _playerInstance.Flip(false); // 플레이어는 오른쪽 봄
            }
        }

        private void SpawnMonster()
        {
            if (_monsterInstance != null)
            {
                Destroy(_monsterInstance.gameObject);
            }

            if (_monsterPrefab != null && _monsterSpawnPoint != null)
            {
                _monsterInstance = Instantiate(_monsterPrefab, _monsterSpawnPoint.position, Quaternion.identity, _monsterSpawnPoint);
                _monsterInstance.Initialize();
                _monsterInstance.Flip(true); // 몬스터는 왼쪽 봄
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            _monsterInstance?.PlayDie();
            SpawnNextMonsterDelay().Forget();
        }

        private async UniTaskVoid SpawnNextMonsterDelay()
        {
            await UniTask.Delay(1000); 
            SpawnMonster();
        }

        private void OnStageResult(StageResultEvent evt)
        {
            if (!evt.IsClear)
            {
                Debug.Log("Stage Failed");
            }
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
            }
        }
    }
}
