using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.UI
{
    /// <summary>
    /// 메인 화면 루트 씬. 하단 탭에 따라 콘텐츠 프리팹을 교체/활성화한다.
    /// </summary>
    public class UIMainRootScene : UI_Scene
    {
        [Header("Anchors")]
        [SerializeField] private RectTransform _contentAnchor;

        [Header("Tab Prefabs")]
        [SerializeField] private GameObject _battlePrefab;
        [SerializeField] private GameObject _skillPrefab;
        [SerializeField] private GameObject _equipPrefab;
        [SerializeField] private GameObject _quizPrefab;

        private readonly Dictionary<EUITabType, GameObject> _instances = new();
        private EUITabType _currentTab = EUITabType.Battle;

        public override void OnShow()
        {
            base.OnShow();
            OpenTab(_currentTab);
        }

        public void OpenTab(EUITabType tab)
        {
            if (_contentAnchor == null)
            {
                Debug.LogError("[UIMainRootScene] ContentAnchor is not assigned.");
                return;
            }

            if (_currentTab == tab && _instances.TryGetValue(tab, out var current) && current.activeSelf)
                return;

            foreach (var kv in _instances)
            {
                if (kv.Value != null)
                    kv.Value.SetActive(false);
            }

            if (!_instances.TryGetValue(tab, out var instance) || instance == null)
            {
                var prefab = GetPrefab(tab);
                if (prefab == null)
                {
                    Debug.LogError($"[UIMainRootScene] Prefab for tab '{tab}' is not assigned.");
                    return;
                }

                instance = Object.Instantiate(prefab, _contentAnchor);
                StretchToAnchor(instance.transform as RectTransform);
                _instances[tab] = instance;
            }

            instance.SetActive(true);
            _currentTab = tab;
        }

        private GameObject GetPrefab(EUITabType tab) => tab switch
        {
            EUITabType.Battle => _battlePrefab,
            EUITabType.Skill => _skillPrefab,
            EUITabType.Equip => _equipPrefab,
            EUITabType.Quiz => _quizPrefab,
            _ => null
        };

        private void StretchToAnchor(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}

