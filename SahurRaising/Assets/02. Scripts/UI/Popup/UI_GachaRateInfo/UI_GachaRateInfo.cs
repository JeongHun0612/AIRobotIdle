using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class UI_GachaRateInfo : UI_Popup
    {
        [Header("UI 요소")]
        [SerializeField] private ScrollRect _scrollView;
        [SerializeField] private Transform _contentParent;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("풀링 설정")]
        [SerializeField] private GachaRatePanel _panelPrefab;

        private readonly List<GachaRatePanel> _panels = new List<GachaRatePanel>();

        private GachaType _currentGachaType;
        private int _currentLevel;
        private int _maxLevel;

        private IGachaService _gachaService;
        private IConfigService _configService;
        private IRemoteConfigService _remoteConfigService;

        private RemoteGachaEquipmentTableData _remoteEquipmentData;
        private RemoteGachaDroneTableData _remoteDroneData;

        public async override UniTask InitializeAsync()
        {
            TryBindService();

            // 프리팹에 미리 배치된 패널들을 찾아서 리스트에 추가
            _panels.Clear();
            if (_contentParent != null)
            {
                foreach (Transform child in _contentParent)
                {
                    var panel = child.GetComponent<GachaRatePanel>();
                    if (panel != null)
                    {
                        _panels.Add(panel);
                        panel.gameObject.SetActive(false);
                    }
                }
            }

            // 버튼 이벤트 등록
            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveAllListeners();
                _prevButton.onClick.AddListener(OnClickPrevLevel);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(OnClickNextLevel);
            }

            await UniTask.Yield();
        }

        public override void OnHide()
        {
            base.OnHide();

            // 모든 패널 비활성화
            foreach (var panel in _panels)
            {
                if (panel != null)
                    panel.gameObject.SetActive(false);
            }
        }

        public void SetGachaType(GachaType gachaType)
        {
            if (!TryBindService())
                return;

            _currentGachaType = gachaType;

            // 현재 레벨 및 최대 레벨 가져오기
            _currentLevel = _gachaService.GetGachaLevel(gachaType);
            _maxLevel = _gachaService.GetMaxLevel(gachaType);

            // UI 업데이트
            RefreshUI();

            // 스크롤을 맨 위로 초기화
            ResetScrollPosition();
        }

        private bool TryBindService()
        {
            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_configService == null && ServiceLocator.HasService<IConfigService>())
                _configService = ServiceLocator.Get<IConfigService>();

            if (_remoteConfigService == null && ServiceLocator.HasService<IRemoteConfigService>())
                _remoteConfigService = ServiceLocator.Get<IRemoteConfigService>();

            return _gachaService != null && _configService != null && _remoteConfigService != null;
        }

        private async UniTask LoadRemoteConfigDataAsync()
        {
            if (_remoteConfigService == null || !_remoteConfigService.IsInitialized)
            {
                Debug.LogWarning("[UI_GachaRateInfo] RemoteConfig가 초기화되지 않았습니다.");
                return;
            }

            //try
            //{
            //    // Equipment 데이터 로드
            //    const string equipmentKey = "GachaEquipmentTable";
            //    string equipmentJson = _remoteConfigService.GetString(equipmentKey, "");

            //    if (!string.IsNullOrEmpty(equipmentJson))
            //    {
            //        try
            //        {
            //            var jsonData = JsonUtility.FromJson<RemoteGachaEquipmentTableData>(equipmentJson);
            //            _remoteEquipmentData = ConvertEquipmentJsonToData(jsonData);
            //            Debug.Log($"[UI_GachaRateInfo] RemoteConfig Equipment 데이터 로드 성공: {_remoteEquipmentData.Rows.Count}개 레벨");
            //        }
            //        catch (Exception ex)
            //        {
            //            Debug.LogError($"[UI_GachaRateInfo] Equipment JSON 역직렬화 실패: {ex.Message}");
            //        }
            //    }

            //    // Drone 데이터 로드
            //    const string droneKey = "GachaDroneTable";
            //    string droneJson = _remoteConfigService.GetString(droneKey, "");

            //    if (!string.IsNullOrEmpty(droneJson))
            //    {
            //        try
            //        {
            //            var jsonData = JsonUtility.FromJson<RemoteGachaDroneTableDataJson>(droneJson);
            //            _remoteDroneData = ConvertDroneJsonToData(jsonData);
            //            Debug.Log($"[UI_GachaRateInfo] RemoteConfig Drone 데이터 로드 성공: {_remoteDroneData.Rows.Count}개 레벨");
            //        }
            //        catch (Exception ex)
            //        {
            //            Debug.LogError($"[UI_GachaRateInfo] Drone JSON 역직렬화 실패: {ex.Message}");
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.LogError($"[UI_GachaRateInfo] RemoteConfig 데이터 로드 실패: {ex.Message}");
            //}
        }

        private void RefreshUI()
        {
            LoadAndLogRemoteConfigGachaEquipmentTableAsync().Forget();

            // 레벨 텍스트 업데이트
            UpdateLevelText();

            // 버튼 상태 업데이트
            UpdateButtonState();

            // 확률 데이터 표시
            UpdateGachaRatePanels();
        }

        private void UpdateLevelText()
        {
            if (_levelText != null)
            {
                _levelText.text = $"Lv {_currentLevel}";
            }
        }

        private void UpdateButtonState()
        {
            if (_prevButton != null)
                _prevButton.interactable = _currentLevel > 1;

            if (_nextButton != null)
                _nextButton.interactable = _currentLevel < _maxLevel;
        }

        private void UpdateGachaRatePanels()
        {
            if (_contentParent == null || _panelPrefab == null)
                return;

            // GachaService를 통해 확률 정보 가져오기
            var probabilities = _gachaService.GetProbabilitiesForLevel(_currentGachaType, _currentLevel);

            int needCount = probabilities.Count;

            // 필요한 패널 수만큼 보장 (부족하면 새로 생성하여 풀에 추가)
            while (_panels.Count < needCount)
            {
                var newPanel = Instantiate(_panelPrefab, _contentParent);
                newPanel.gameObject.SetActive(false);
                _panels.Add(newPanel);
            }

            // 패널 채우기
            for (int i = 0; i < _panels.Count; i++)
            {
                if (i < needCount)
                {
                    var panel = _panels[i];
                    var probability = probabilities[i];

                    // UI에서 필요한 값들을 미리 계산
                    Color frameColor = _configService.GetColorForGrade(_currentGachaType, probability.GradeKey);
                    string gradeText = probability.GradeKey;
                    float probabilityValue = probability.Probability;

                    panel.gameObject.SetActive(true);
                    panel.SetData(frameColor, gradeText, probabilityValue);
                }
                else
                {
                    // 남는 패널은 비활성화 (풀 안에서 대기)
                    _panels[i].gameObject.SetActive(false);
                }
            }
        }

        private void ResetScrollPosition()
        {
            if (_scrollView != null)
            {
                _scrollView.verticalNormalizedPosition = 1f;
            }
        }


        private async UniTaskVoid LoadAndLogRemoteConfigGachaEquipmentTableAsync()
        {
            try
            {
                Debug.Log("[UI_GachaRateInfo] ===== RemoteConfig GachaEquipmentTable 테스트 시작 =====");

                // RemoteConfig 서비스 바인딩
                if (_remoteConfigService == null && ServiceLocator.HasService<IRemoteConfigService>())
                {
                    _remoteConfigService = ServiceLocator.Get<IRemoteConfigService>();
                }

                if (_remoteConfigService == null)
                {
                    Debug.LogWarning("[UI_GachaRateInfo] IRemoteConfigService를 찾을 수 없습니다.");
                    return;
                }

                if (!_remoteConfigService.IsInitialized)
                {
                    Debug.LogWarning("[UI_GachaRateInfo] RemoteConfig가 초기화되지 않았습니다.");
                    return;
                }


                // RemoteConfig의 모든 키 확인 (디버깅용)
                try
                {
                    var configService = Unity.Services.RemoteConfig.RemoteConfigService.Instance;
                    var config = configService.appConfig;

                    Debug.Log($"[UI_GachaRateInfo] RemoteConfig 상태 확인:");
                    Debug.Log($"[UI_GachaRateInfo] - Config Keys 개수: {config.GetKeys().Length}");

                    // 모든 키 출력
                    var allKeys = config.GetKeys();
                    Debug.Log($"[UI_GachaRateInfo] - 사용 가능한 모든 키:");
                    foreach (var keTesty in allKeys)
                    {
                        var value = config.GetString(keTesty, "");
                        Debug.Log($"[UI_GachaRateInfo]   - '{keTesty}': 길이={value.Length}, 비어있음={string.IsNullOrEmpty(value)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UI_GachaRateInfo] RemoteConfig 상태 확인 실패: {ex.Message}");
                }

                // RemoteConfig에서 JSON 문자열 가져오기
                //const string key = "GachaEquipmentTable";
                const string key = "RemoteGachaEquipmentTableData";
                string jsonString = _remoteConfigService.GetString(key, "");

                if (string.IsNullOrEmpty(jsonString))
                {
                    Debug.LogWarning($"[UI_GachaRateInfo] RemoteConfig에서 '{key}' 키를 찾을 수 없거나 값이 비어있습니다.");
                    Debug.LogWarning("[UI_GachaRateInfo] Unity Dashboard에서 'GachaEquipmentTable' 키가 올바르게 설정되었는지 확인하세요.");
                    return;
                }

                Debug.Log($"[UI_GachaRateInfo] RemoteConfig에서 '{key}' JSON 데이터 로드 성공");
                Debug.Log($"[UI_GachaRateInfo] JSON 길이: {jsonString.Length} 문자");
                Debug.Log($"[UI_GachaRateInfo] JSON 내용 (처음 500자):\n{jsonString.Substring(0, Mathf.Min(500, jsonString.Length))}...");

                // JSON을 객체로 역직렬화 시도
                try
                {
                    // GachaEquipmentTable의 _rows 구조에 맞춰 역직렬화
                    var remoteData = JsonUtility.FromJson<RemoteGachaEquipmentTableData>(jsonString);

                    if (remoteData == null || remoteData.Rows == null || remoteData.Rows.Count == 0)
                    {
                        Debug.LogWarning("[UI_GachaRateInfo] JSON 역직렬화는 성공했지만 데이터가 비어있습니다.");
                        return;
                    }

                    Debug.Log($"[UI_GachaRateInfo] JSON 역직렬화 성공! 총 {remoteData.Rows.Count}개의 레벨 데이터 발견");

                    // 각 레벨별 데이터 출력
                    foreach (var row in remoteData.Rows)
                    {
                        Debug.Log($"[UI_GachaRateInfo] --- 레벨 {row.Level} ---");

                        if (row.Probabilities == null || row.Probabilities.Count == 0)
                        {
                            Debug.Log($"[UI_GachaRateInfo]   레벨 {row.Level}: 확률 데이터 없음");
                            continue;
                        }

                        int validProbCount = 0;
                        float totalProb = 0f;

                        foreach (var prob in row.Probabilities)
                        {
                            if (prob.Probability > 0)
                            {
                                validProbCount++;
                                totalProb += prob.Probability;
                                Debug.Log($"[UI_GachaRateInfo]   등급 {prob.Grade}: 확률 {prob.Probability}%");
                            }
                        }

                        Debug.Log($"[UI_GachaRateInfo]   유효 확률 항목: {validProbCount}개, 총 확률 합계: {totalProb}%");
                    }

                    Debug.Log("[UI_GachaRateInfo] ===== RemoteConfig GachaEquipmentTable 테스트 완료 =====");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UI_GachaRateInfo] JSON 역직렬화 실패: {ex.Message}");
                    Debug.LogError($"[UI_GachaRateInfo] 스택 트레이스: {ex.StackTrace}");
                    Debug.LogError($"[UI_GachaRateInfo] 전체 JSON 내용:\n{jsonString}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UI_GachaRateInfo] RemoteConfig 테스트 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnClickPrevLevel()
        {
            if (_currentLevel > 1)
            {
                _currentLevel--;
                RefreshUI();
            }
        }

        private void OnClickNextLevel()
        {
            if (_currentLevel < _maxLevel)
            {
                _currentLevel++;
                RefreshUI();
            }
        }
    }
}
