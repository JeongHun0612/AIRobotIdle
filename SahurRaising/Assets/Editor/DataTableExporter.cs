using System.Globalization;
using System.IO;
using System.Text;
using SahurRaising.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject 테이블을 CSV 파일로 역변환(익스포트)하는 에디터 도구
/// 메뉴: Tools/SahurRaising/Export Table to CSV
/// 
/// 사용법:
///   1. Project 창에서 내보내고 싶은 테이블 SO를 선택
///   2. 메뉴 또는 우클릭 → Export Table to CSV 실행
///   3. CSV 파일이 Assets/10.Data/ 에 덮어쓰기 됨
/// </summary>
public static class DataTableExporter
{
    private const string CsvPath = "Assets/10.Data";

    // ─────────────────────────────── 메뉴 ───────────────────────────────

    [MenuItem("Tools/SahurRaising/Export Table to CSV")]
    public static void ExportSelected()
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Export Table to CSV",
                "Project 창에서 내보낼 테이블 SO를 먼저 선택해주세요.", "확인");
            return;
        }

        if (TryExport(selected))
        {
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/SahurRaising/Export Table to CSV", validate = true)]
    private static bool ExportSelectedValidate()
    {
        var obj = Selection.activeObject;
        if (obj == null) return false;

        return obj is UpgradeTable
            || obj is MonsterTable
            || obj is EquipmentTable
            || obj is DroneTable
            || obj is EvolutionTable
            || obj is SkillTable;
    }

    // ─────────────────────── 에셋 우클릭 컨텍스트 메뉴 ───────────────────────

    [MenuItem("Assets/Export Table to CSV")]
    private static void ExportFromContext()
    {
        ExportSelected();
    }

    [MenuItem("Assets/Export Table to CSV", validate = true)]
    private static bool ExportFromContextValidate()
    {
        return ExportSelectedValidate();
    }

    // ─────────────────────────── 분기 처리 ─────────────────────────────

    private static bool TryExport(Object selected)
    {
        switch (selected)
        {
            case UpgradeTable t:
                return ExportUpgrade(t);
            case MonsterTable t:
                return ExportMonster(t);
            case EquipmentTable t:
                return ExportEquipment(t);
            case DroneTable t:
                return ExportDrone(t);
            case EvolutionTable t:
                return ExportEvolution(t);
            case SkillTable t:
                return ExportSkill(t);
            default:
                EditorUtility.DisplayDialog("Export Table to CSV",
                    $"지원하지 않는 타입입니다: {selected.GetType().Name}\n" +
                    "지원 타입: UpgradeTable, MonsterTable, EquipmentTable, DroneTable, EvolutionTable, SkillTable",
                    "확인");
                return false;
        }
    }

    // ──────────────────────── Upgrade ────────────────────────────

    private static bool ExportUpgrade(UpgradeTable table)
    {
        var fileName = "AI 로봇 키우기 DB - Upgrade.csv";
        var sb = new StringBuilder();

        // 헤더 (DataTableBuilder의 컬럼 순서와 정확히 일치)
        sb.AppendLine("코드,업그레이드명,설명,Stat,구분,MaxLv,Gold_Base,Gold_Pow," +
                       "MaxLv1,Gold_Grow1,MaxLv2,Gold_Grow2,MaxLv3,Gold_Grow3,MaxLv4,Gold_Grow4");

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(",",
                row.Code,
                row.Name,
                row.Description,
                row.Stat.ToString(),
                TierToKorean(row.Tier),
                row.MaxLevel.ToString(),
                D(row.GoldBase),
                D(row.GoldPow),
                I(row.Segment1.MaxLevel), D(row.Segment1.Growth),
                I(row.Segment2.MaxLevel), D(row.Segment2.Growth),
                I(row.Segment3.MaxLevel), D(row.Segment3.Growth),
                I(row.Segment4.MaxLevel), D(row.Segment4.Growth)
            ));
        }

        return WriteAndNotify(fileName, sb);
    }

    // ──────────────────────── Monster ────────────────────────────

    private static bool ExportMonster(MonsterTable table)
    {
        var fileName = "AI 로봇 키우기 DB - Monster.csv";
        var sb = new StringBuilder();

        sb.AppendLine("MonsterLevel,BenchmarkLevel,ATKLevel,DEFLevel,MonsterHP,MonsterATK,MonsterDEF,MonsterDEFR,Gold");

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(",",
                row.MonsterLevel.ToString(),
                row.BenchmarkLevel.ToString(),
                F(row.ATKLevel),
                F(row.DEFLevel),
                BD(row.MonsterHP),
                BD(row.MonsterATK),
                BD(row.MonsterDEF),
                F(row.MonsterDEFR),
                BD(row.Gold)
            ));
        }

        return WriteAndNotify(fileName, sb);
    }

    // ──────────────────────── Equipment ──────────────────────────

    private static bool ExportEquipment(EquipmentTable table)
    {
        var fileName = "AI 로봇 키우기 DB - Equipment.csv";
        var sb = new StringBuilder();

        sb.AppendLine("EquipmentCode,EquipmentName,EquipmentType,EquipmentGrade," +
                       "EquipOption_Type,EquipOption_Base,EquipOption_Up," +
                       "HeldOption1_Type,HeldOption1_Base,HeldOption1_Up," +
                       "HeldOption2_Type,HeldOption2," +
                       "HeldOption3_Type,HeldOption3");

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(",",
                row.Code,
                row.Name,
                row.Type.ToString(),
                row.Grade.ToString(),
                row.EquipOption.Type, D(row.EquipOption.Base), D(row.EquipOption.Up),
                row.HeldOption1.Type, D(row.HeldOption1.Base), D(row.HeldOption1.Up),
                row.HeldOption2.Type, D(row.HeldOption2.Base),
                row.HeldOption3.Type, D(row.HeldOption3.Base)
            ));
        }

        return WriteAndNotify(fileName, sb);
    }

    // ──────────────────────── Drone ──────────────────────────────

    private static bool ExportDrone(DroneTable table)
    {
        var fileName = "AI 로봇 키우기 DB - Drone.csv";
        var sb = new StringBuilder();

        sb.AppendLine("ID,AtkRate,EquipOption_Base,EquipOption_Up," +
                       "HeldOption1_Type,HeldOption1_Base,HeldOption1_Up");

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(",",
                row.ID,
                D(row.AtkRate),
                D(row.EquipOption.Base), D(row.EquipOption.Up),
                row.HeldOption1.Type, D(row.HeldOption1.Base), D(row.HeldOption1.Up)
            ));
        }

        return WriteAndNotify(fileName, sb);
    }

    // ──────────────────────── Evolution ──────────────────────────

    private static bool ExportEvolution(EvolutionTable table)
    {
        var fileName = "AI 로봇 키우기 DB - Evolution.csv";
        var sb = new StringBuilder();

        sb.AppendLine("EvolutionLevel,CharacterName,ReqSumLevel,BonusATKR3,컨셉");

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(",",
                row.EvolutionLevel.ToString(),
                row.CharacterName,
                row.ReqSumLevel.ToString(),
                D(row.BonusATKR3),
                row.Concept
            ));
        }

        return WriteAndNotify(fileName, sb);
    }

    // ──────────────────────── Skill ──────────────────────────────

    private static bool ExportSkill(SkillTable table)
    {
        var fileName = "AI 로봇 키우기 DB - Skill.csv";
        var sb = new StringBuilder();

        sb.AppendLine("ID,Name,Desc,Note,Cost,Time,XCoord,YCoord,Coord,Prefix,isFirstNode,EffectType,TargetStat,Value");

        foreach (var row in table.Rows)
        {
            // TargetStat: EffectType에 따라 StatType 또는 SkillSpecialType 출력
            string targetStatStr = row.EffectType switch
            {
                SkillEffectType.Stat => row.TargetStat.ToString(),
                SkillEffectType.Special => row.TargetSpecial.ToString(),
                _ => ""
            };

            sb.AppendLine(string.Join(",",
                row.ID,
                row.Name,
                row.Desc,
                row.Note,
                row.Cost.ToString(),
                row.Time.ToString(),
                row.XCoord.ToString(),
                row.YCoord.ToString(),
                row.Coord,
                row.Prefix,
                row.IsFirstNode ? "TRUE" : "",
                row.EffectType.ToString(),
                targetStatStr,
                D(row.Value)
            ));
        }

        return WriteAndNotify(fileName, sb);
    }

    // ──────────────────────── 유틸리티 ──────────────────────────

    /// <summary>
    /// CSV 파일 작성 및 결과 알림
    /// </summary>
    private static bool WriteAndNotify(string fileName, StringBuilder sb)
    {
        var fullPath = Path.Combine(CsvPath, fileName);

        // 마지막 줄 끝의 불필요한 개행 제거
        var content = sb.ToString().TrimEnd('\r', '\n') + "\n";

        if (!EditorUtility.DisplayDialog("Export Table to CSV",
            $"다음 파일을 덮어쓰시겠습니까?\n{fullPath}", "덮어쓰기", "취소"))
        {
            return false;
        }

        Directory.CreateDirectory(CsvPath);
        File.WriteAllText(fullPath, content, Encoding.UTF8);

        Debug.Log($"[DataTableExporter] CSV 내보내기 완료: {fullPath}");
        return true;
    }

    // double → 문자열 (불변 문화권, 불필요한 0 제거)
    private static string D(double v)
    {
        if (v == 0d) return "0";
        return v.ToString("G", CultureInfo.InvariantCulture);
    }

    // float → 문자열
    private static string F(float v)
    {
        if (v == 0f) return "0";
        return v.ToString("G", CultureInfo.InvariantCulture);
    }

    // int → 문자열 (빈 값 허용: 0이면 빈 문자열도 가능, 하지만 기존 CSV는 0도 표기하므로 그대로)
    private static string I(int v)
    {
        return v.ToString();
    }

    // BigDouble → 문자열 (과학적 표기법)
    private static string BD(BreakInfinity.BigDouble v)
    {
        if (v <= 0) return "0";
        return v.ToString();
    }

    // UpgradeTier → 한국어 문자열 (DataTableBuilder.ParseTier의 역변환)
    private static string TierToKorean(UpgradeTier tier)
    {
        return tier switch
        {
            UpgradeTier.Normal => "일반",
            UpgradeTier.Super => "슈퍼",
            UpgradeTier.Ultra => "울트라",
            UpgradeTier.SuperUltra => "슈퍼울트라",
            _ => tier.ToString()
        };
    }
}
