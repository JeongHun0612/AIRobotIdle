using UnityEngine;
using UnityEditor;
using SahurRaising.GamePlay;
using System.Collections.Generic;

namespace SahurRaising.Editors
{
    [CustomEditor(typeof(SpawnPattern))]
    public class SpawnPatternEditor : UnityEditor.Editor
    {
        private SpawnPattern _target;
        private SerializedProperty _spawnSlotsProp;
        
        // 그래프 설정
        private const float GRAPH_HEIGHT = 300f;
        private const float PADDING = 40f;
        private const float HANDLE_SIZE = 12f;
        
        // 뷰 설정
        private float _maxX = 5f; // X축 최대값 (Order Offset)
        private int _selectedSlotIndex = -1;

        private void OnEnable()
        {
            _target = (SpawnPattern)target;
            _spawnSlotsProp = serializedObject.FindProperty("_spawnSlots");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. 기본 정보 그리기
            EditorGUILayout.LabelField("기본 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_patternName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_description"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("타이밍 & 가중치", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_spawnDelay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_patternCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_selectionWeight"));
            EditorGUILayout.Space();

            // 2. 비주얼 에디터 그리기
            DrawVisualEditor();

            // 3. 슬롯 리스트 그리기 (기본 인스펙터 스타일)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("상세 데이터 (리스트)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_spawnSlotsProp, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVisualEditor()
        {
            EditorGUILayout.LabelField("패턴 시각화 (드래그하여 수정)", EditorStyles.boldLabel);

            // 그래프 영역 확보
            Rect graphRect = GUILayoutUtility.GetRect(0, GRAPH_HEIGHT, GUILayout.ExpandWidth(true));
            
            // 배경 그리기
            EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f));
            
            // 그리드 그리기
            DrawGrid(graphRect);

            // 입력 처리
            HandleInput(graphRect);

            // 슬롯 포인트 그리기
            DrawSlots(graphRect);
            
            // 범례 및 도움말
            EditorGUILayout.HelpBox(
                "가로(X): 등장 순서 (Order Offset)\n" +
                "세로(Y): 맵 위치 (위=+1, 아래=-1)\n" +
                "조작: 점을 드래그하여 이동, 빈 공간 클릭하여 선택 해제\n" +
                "우클릭: 점 추가/삭제", 
                MessageType.Info);
        }

        private void DrawGrid(Rect rect)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            
            // X축 (Order) 그리드
            for (float i = 0; i <= _maxX; i += 0.5f)
            {
                float x = RectToGraphX(rect, i);
                Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
            }

            // Y축 (Position) 그리드
            // 중앙선 (0)
            float y0 = RectToGraphY(rect, 0f);
            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            Handles.DrawLine(new Vector3(rect.x, y0), new Vector3(rect.xMax, y0));

            // 상단 (+1), 하단 (-1)
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            float yTop = RectToGraphY(rect, 1f);
            float yBot = RectToGraphY(rect, -1f);
            Handles.DrawLine(new Vector3(rect.x, yTop), new Vector3(rect.xMax, yTop));
            Handles.DrawLine(new Vector3(rect.x, yBot), new Vector3(rect.xMax, yBot));
            
            // 라벨
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = Color.gray;
            
            GUI.Label(new Rect(rect.x + 5, y0 - 15, 30, 20), "Center", style);
            GUI.Label(new Rect(rect.x + 5, yTop, 30, 20), "+1.0", style);
            GUI.Label(new Rect(rect.x + 5, yBot - 15, 30, 20), "-1.0", style);
        }

        private void DrawSlots(Rect rect)
        {
            for (int i = 0; i < _spawnSlotsProp.arraySize; i++)
            {
                SerializedProperty slotProp = _spawnSlotsProp.GetArrayElementAtIndex(i);
                float order = slotProp.FindPropertyRelative("OrderOffset").floatValue;
                float yPos = slotProp.FindPropertyRelative("YNormalized").floatValue;

                Vector2 pos = new Vector2(
                    RectToGraphX(rect, order),
                    RectToGraphY(rect, yPos)
                );

                // 선택된 슬롯 강조
                Color color = (i == _selectedSlotIndex) ? Color.green : Color.cyan;
                
                // 몬스터 종류에 따라 색상 변경 (선택적)
                var kind = (SahurRaising.Core.MonsterKind)slotProp.FindPropertyRelative("MonsterKind").enumValueIndex;
                if (kind == SahurRaising.Core.MonsterKind.Elite) color = Color.yellow;
                if (kind == SahurRaising.Core.MonsterKind.Boss) color = Color.red;

                Handles.color = color;
                
                // 점 그리기
                if (GUI.Button(new Rect(pos.x - HANDLE_SIZE/2, pos.y - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE), GUIContent.none, GUIStyle.none))
                {
                    _selectedSlotIndex = i;
                    GUI.FocusControl(null); // 포커스 해제
                }
                
                // 핸들 모양
                Handles.DrawSolidDisc(pos, Vector3.forward, HANDLE_SIZE / 2);
                
                // 인덱스 표시
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(pos.x + 8, pos.y - 8, 20, 20), i.ToString(), labelStyle);
            }
        }

        private void HandleInput(Rect rect)
        {
            Event e = Event.current;
            
            // 드래그 처리
            if (e.type == EventType.MouseDrag && _selectedSlotIndex >= 0 && _selectedSlotIndex < _spawnSlotsProp.arraySize)
            {
                if (rect.Contains(e.mousePosition))
                {
                    SerializedProperty slotProp = _spawnSlotsProp.GetArrayElementAtIndex(_selectedSlotIndex);
                    
                    float newOrder = GraphToRectX(rect, e.mousePosition.x);
                    float newY = GraphToRectY(rect, e.mousePosition.y);

                    // 값 클램핑 및 스냅 (선택적)
                    newOrder = Mathf.Max(0, newOrder);
                    newY = Mathf.Clamp(newY, -1f, 1f);

                    // Shift 누르면 스냅
                    if (e.shift)
                    {
                        newOrder = Mathf.Round(newOrder * 2) / 2f; // 0.5 단위
                        newY = Mathf.Round(newY * 4) / 4f; // 0.25 단위
                    }

                    slotProp.FindPropertyRelative("OrderOffset").floatValue = newOrder;
                    slotProp.FindPropertyRelative("YNormalized").floatValue = newY;
                    
                    e.Use();
                }
            }
            
            // 클릭 처리 (선택 해제 및 우클릭 메뉴)
            if (e.type == EventType.MouseDown)
            {
                if (rect.Contains(e.mousePosition))
                {
                    if (e.button == 0) // 좌클릭
                    {
                        // 빈 공간 클릭 시 선택 해제 (DrawSlots의 버튼이 이벤트를 먼저 먹지 않았다면)
                        _selectedSlotIndex = -1;
                        GUI.FocusControl(null);
                        Repaint();
                    }
                    else if (e.button == 1) // 우클릭
                    {
                        ShowContextMenu(e.mousePosition, rect);
                        e.Use();
                    }
                }
            }
        }

        private void ShowContextMenu(Vector2 mousePos, Rect rect)
        {
            GenericMenu menu = new GenericMenu();
            
            float order = GraphToRectX(rect, mousePos.x);
            float yPos = GraphToRectY(rect, mousePos.y);
            
            // 값 보정
            order = Mathf.Max(0, order);
            yPos = Mathf.Clamp(yPos, -1f, 1f);

            menu.AddItem(new GUIContent("Add Point Here"), false, () => {
                AddPoint(order, yPos);
            });

            if (_selectedSlotIndex >= 0)
            {
                menu.AddItem(new GUIContent("Delete Selected Point"), false, () => {
                    DeleteSelectedPoint();
                });
                
                menu.AddItem(new GUIContent("Duplicate Selected"), false, () => {
                    DuplicateSelectedPoint();
                });
            }

            menu.ShowAsContext();
        }

        private void AddPoint(float order, float yPos)
        {
            int index = _spawnSlotsProp.arraySize;
            _spawnSlotsProp.InsertArrayElementAtIndex(index);
            SerializedProperty newSlot = _spawnSlotsProp.GetArrayElementAtIndex(index);
            
            newSlot.FindPropertyRelative("OrderOffset").floatValue = order;
            newSlot.FindPropertyRelative("YNormalized").floatValue = yPos;
            newSlot.FindPropertyRelative("MonsterKind").enumValueIndex = (int)SahurRaising.Core.MonsterKind.Normal;
            newSlot.FindPropertyRelative("Note").stringValue = "";

            serializedObject.ApplyModifiedProperties();
            _selectedSlotIndex = index;
        }

        private void DeleteSelectedPoint()
        {
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _spawnSlotsProp.arraySize)
            {
                _spawnSlotsProp.DeleteArrayElementAtIndex(_selectedSlotIndex);
                _selectedSlotIndex = -1;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void DuplicateSelectedPoint()
        {
             if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _spawnSlotsProp.arraySize)
            {
                SerializedProperty source = _spawnSlotsProp.GetArrayElementAtIndex(_selectedSlotIndex);
                float order = source.FindPropertyRelative("OrderOffset").floatValue;
                float yPos = source.FindPropertyRelative("YNormalized").floatValue;
                
                AddPoint(order + 0.2f, yPos); // 약간 옆에 생성
            }
        }

        // 좌표 변환 헬퍼 함수들
        private float RectToGraphX(Rect rect, float value)
        {
            // value: 0 ~ _maxX -> rect.x + PADDING ~ rect.xMax - PADDING
            float t = value / _maxX;
            return Mathf.Lerp(rect.x + PADDING, rect.xMax - PADDING, t);
        }

        private float RectToGraphY(Rect rect, float value)
        {
            // value: -1 ~ 1 -> rect.yMax - PADDING ~ rect.y + PADDING (Y축 반전 주의)
            // -1이 아래쪽(큰 Y값), 1이 위쪽(작은 Y값)
            float t = (value + 1f) / 2f; // 0 ~ 1
            return Mathf.Lerp(rect.yMax - PADDING, rect.y + PADDING, t);
        }

        private float GraphToRectX(Rect rect, float pixelX)
        {
            float t = Mathf.InverseLerp(rect.x + PADDING, rect.xMax - PADDING, pixelX);
            return t * _maxX;
        }

        private float GraphToRectY(Rect rect, float pixelY)
        {
            float t = Mathf.InverseLerp(rect.yMax - PADDING, rect.y + PADDING, pixelY);
            return (t * 2f) - 1f;
        }
    }
}
