#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SahurRaising.Core;

namespace SahurRaising.GamePlay.Editor
{
    /// <summary>
    /// 기본 스폰 패턴들을 생성하는 에디터 유틸리티
    /// 메뉴: SahurRaising/Combat/Create Default Spawn Patterns
    /// </summary>
    public static class SpawnPatternCreator
    {
        private const string PATTERN_PATH = "Assets/06. ScriptableObject/Combat/SpawnPatterns/";

        [MenuItem("Tools/SahurRaising/Combat/Create Default Spawn Patterns")]
        public static void CreateDefaultPatterns()
        {
            CreateLinePattern();
            CreateVFormationPattern();
            CreateDiagonalPattern();
            CreateSwarmPattern();
            CreateBossEntryPattern();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SpawnPatternCreator] 기본 스폰 패턴 5개 생성 완료!");
        }

        /// <summary>
        /// 일렬 패턴 (● ● ● ●)
        /// </summary>
        private static void CreateLinePattern()
        {
            var pattern = ScriptableObject.CreateInstance<SpawnPattern>();
            
            // 리플렉션으로 private 필드 설정
            SetPrivateField(pattern, "_patternName", "Line");
            SetPrivateField(pattern, "_description", "일렬로 등장하는 기본 패턴");
            SetPrivateField(pattern, "_spawnDelay", 0.2f);
            SetPrivateField(pattern, "_patternCooldown", 0.5f);
            SetPrivateField(pattern, "_selectionWeight", 2f);

            var slots = new System.Collections.Generic.List<SpawnSlot>
            {
                new SpawnSlot(0f, 0f, MonsterKind.Normal) { Note = "중앙" },
                new SpawnSlot(0.3f, 0f, MonsterKind.Normal) { Note = "중앙" },
                new SpawnSlot(0.6f, 0f, MonsterKind.Normal) { Note = "중앙" },
            };
            SetPrivateField(pattern, "_spawnSlots", slots);

            SaveAsset(pattern, PATTERN_PATH + "Pattern_Line.asset");
        }

        /// <summary>
        /// V자 포메이션 (  ●  / ● ● ●)
        /// </summary>
        private static void CreateVFormationPattern()
        {
            var pattern = ScriptableObject.CreateInstance<SpawnPattern>();
            
            SetPrivateField(pattern, "_patternName", "V-Formation");
            SetPrivateField(pattern, "_description", "V자 형태로 등장하는 패턴");
            SetPrivateField(pattern, "_spawnDelay", 0.15f);
            SetPrivateField(pattern, "_patternCooldown", 0.8f);
            SetPrivateField(pattern, "_selectionWeight", 1.5f);

            var slots = new System.Collections.Generic.List<SpawnSlot>
            {
                new SpawnSlot(0f, 0f, MonsterKind.Normal) { Note = "선두" },
                new SpawnSlot(0.5f, 0.4f, MonsterKind.Normal) { Note = "좌상단" },
                new SpawnSlot(0.5f, -0.4f, MonsterKind.Normal) { Note = "우하단" },
                new SpawnSlot(1f, 0.7f, MonsterKind.Normal) { Note = "좌상단 끝" },
                new SpawnSlot(1f, -0.7f, MonsterKind.Normal) { Note = "우하단 끝" },
            };
            SetPrivateField(pattern, "_spawnSlots", slots);

            SaveAsset(pattern, PATTERN_PATH + "Pattern_VFormation.asset");
        }

        /// <summary>
        /// 대각선 패턴 (●  / ●  /  ●)
        /// </summary>
        private static void CreateDiagonalPattern()
        {
            var pattern = ScriptableObject.CreateInstance<SpawnPattern>();
            
            SetPrivateField(pattern, "_patternName", "Diagonal");
            SetPrivateField(pattern, "_description", "대각선으로 순차 등장");
            SetPrivateField(pattern, "_spawnDelay", 0.25f);
            SetPrivateField(pattern, "_patternCooldown", 0.6f);
            SetPrivateField(pattern, "_selectionWeight", 1f);

            var slots = new System.Collections.Generic.List<SpawnSlot>
            {
                new SpawnSlot(0f, 0.6f, MonsterKind.Normal) { Note = "상단 시작" },
                new SpawnSlot(0.4f, 0.2f, MonsterKind.Normal) { Note = "중상단" },
                new SpawnSlot(0.8f, -0.2f, MonsterKind.Normal) { Note = "중하단" },
                new SpawnSlot(1.2f, -0.6f, MonsterKind.Normal) { Note = "하단 끝" },
            };
            SetPrivateField(pattern, "_spawnSlots", slots);

            SaveAsset(pattern, PATTERN_PATH + "Pattern_Diagonal.asset");
        }

        /// <summary>
        /// 무리 패턴 (랜덤 클러스터)
        /// </summary>
        private static void CreateSwarmPattern()
        {
            var pattern = ScriptableObject.CreateInstance<SpawnPattern>();
            
            SetPrivateField(pattern, "_patternName", "Swarm");
            SetPrivateField(pattern, "_description", "무리지어 한꺼번에 등장");
            SetPrivateField(pattern, "_spawnDelay", 0.08f);
            SetPrivateField(pattern, "_patternCooldown", 1f);
            SetPrivateField(pattern, "_selectionWeight", 1f);

            var slots = new System.Collections.Generic.List<SpawnSlot>
            {
                new SpawnSlot(0f, 0.3f, MonsterKind.Normal),
                new SpawnSlot(0.1f, -0.2f, MonsterKind.Normal),
                new SpawnSlot(0.2f, 0.5f, MonsterKind.Normal),
                new SpawnSlot(0.15f, -0.5f, MonsterKind.Normal),
                new SpawnSlot(0.25f, 0f, MonsterKind.Normal),
                new SpawnSlot(0.3f, 0.4f, MonsterKind.Normal),
            };
            SetPrivateField(pattern, "_spawnSlots", slots);

            SaveAsset(pattern, PATTERN_PATH + "Pattern_Swarm.asset");
        }

        /// <summary>
        /// 보스 등장 패턴 (엘리트 + 보스)
        /// </summary>
        private static void CreateBossEntryPattern()
        {
            var pattern = ScriptableObject.CreateInstance<SpawnPattern>();
            
            SetPrivateField(pattern, "_patternName", "Boss Entry");
            SetPrivateField(pattern, "_description", "보스와 호위 엘리트 등장");
            SetPrivateField(pattern, "_spawnDelay", 0.3f);
            SetPrivateField(pattern, "_patternCooldown", 1.5f);
            SetPrivateField(pattern, "_selectionWeight", 0.3f); // 낮은 확률

            var slots = new System.Collections.Generic.List<SpawnSlot>
            {
                new SpawnSlot(0f, 0.5f, MonsterKind.Elite) { Note = "호위 상단" },
                new SpawnSlot(0f, -0.5f, MonsterKind.Elite) { Note = "호위 하단" },
                new SpawnSlot(0.8f, 0f, MonsterKind.Boss) { Note = "보스 중앙" },
            };
            SetPrivateField(pattern, "_spawnSlots", slots);

            SaveAsset(pattern, PATTERN_PATH + "Pattern_BossEntry.asset");
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private static void SaveAsset(SpawnPattern pattern, string path)
        {
            // 기존 에셋이 있으면 삭제
            var existingAsset = AssetDatabase.LoadAssetAtPath<SpawnPattern>(path);
            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(pattern, path);
            Debug.Log($"[SpawnPatternCreator] 패턴 생성: {path}");
        }
    }
}
#endif
