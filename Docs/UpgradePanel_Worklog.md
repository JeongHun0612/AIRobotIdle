# Upgrade 패널(메인 상시 UI)

## 현재 목표(요청사항)
- **메인 화면에서- [x] **[UI]** `UI_Upgrade_Slot` 프리팹 제작
  - [x] 아이콘, 타이틀, 설명, 레벨, 수치, 비용 텍스트, 업그레이드 버튼 구성
  - [x] 잠금 상태 표시용 `Panel_Lock` 구성 (반투명 오버레이)
- [x] **[UI]** `UI_Upgrade_Panel` 프리팹 제작 및 배치
  - [x] Scroll View 및 Content 설정 (Vertical Layout Group, Content Size Fitter)
  - [x] 계층 구조 분리 (`Root_Normal`, `Lock_Super`, `Root_Super` 등)
  - [x] `UI_Main` 레이아웃에 통합 (상단바/전투화면/업그레이드/하단바 비율 조정)에서 조회**해서 바인딩
- 조건 미달 시 슬롯/티어는 **자물쇠 UI로 잠금 표시**
- 첨부된 이미지 느낌 (패널(세로 드래그 가능) - 셀 - 슬롯 및 설명과 버튼)
