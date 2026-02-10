using System.Collections.Generic;
using System.IO;
using BreakInfinity;
using SahurRaising.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV를 테이블 단위 ScriptableObject로 변환하는 에디터 빌더
/// 메뉴: Tools/SahurRaising/Build Data Tables
/// </summary>
public static class DataTableBuilder
{
    private const string CsvPath = "Assets/10.Data";
    private const string OutputPath = "Assets/06. ScriptableObject/Data";

    [MenuItem("Tools/SahurRaising/Build Data Tables")]
    public static void Build()
    {
        Directory.CreateDirectory(OutputPath);

        BuildMonster();
        BuildUpgrade();
        BuildStats();
        BuildEquipment();
        BuildDrone();
        BuildEvolution();
        BuildSkill();
        BuildGachaDrone();
        BuildGachaEquipment();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataTableBuilder] 빌드 완료");
    }

    // Monster 테이블 컬럼명
    private const string COL_MON_LEVEL = "MonsterLevel";
    private const string COL_MON_BENCH = "BenchmarkLevel";
    private const string COL_MON_ATKLV = "ATKLevel";
    private const string COL_MON_DEFLV = "DEFLevel";
    private const string COL_MON_HP = "MonsterHP";
    private const string COL_MON_ATK = "MonsterATK";
    private const string COL_MON_DEF = "MonsterDEF";
    private const string COL_MON_DEFR = "MonsterDEFR";
    private const string COL_MON_GOLD = "Gold";

    private static void BuildMonster()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Monster.csv");
        var rows = new List<MonsterRow>();

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            rows.Add(new MonsterRow
            {
                MonsterLevel = row.Int(COL_MON_LEVEL),
                BenchmarkLevel = row.Int(COL_MON_BENCH),
                ATKLevel = row.Float(COL_MON_ATKLV),
                DEFLevel = row.Float(COL_MON_DEFLV),
                MonsterHP = row.BigDoubleValue(COL_MON_HP),
                MonsterATK = row.BigDoubleValue(COL_MON_ATK),
                MonsterDEF = row.BigDoubleValue(COL_MON_DEF),
                MonsterDEFR = row.Float(COL_MON_DEFR),
                Gold = row.BigDoubleValue(COL_MON_GOLD)
            });
        }

        SaveTable<int, MonsterRow, MonsterTable>("MonsterTable.asset", rows);
    }

    // Upgrade 테이블 컬럼명
    private const string COL_UP_CODE = "코드";
    private const string COL_UP_NAME = "업그레이드명";
    private const string COL_UP_DESC = "설명";
    private const string COL_UP_STAT = "Stat";
    private const string COL_UP_TIER = "구분";
    private const string COL_UP_MAX = "MaxLv";
    private const string COL_UP_BASE = "Gold_Base";
    private const string COL_UP_POW = "Gold_Pow";
    private const string COL_UP_MAX1 = "MaxLv1";
    private const string COL_UP_GROW1 = "Gold_Grow1";
    private const string COL_UP_MAX2 = "MaxLv2";
    private const string COL_UP_GROW2 = "Gold_Grow2";
    private const string COL_UP_MAX3 = "MaxLv3";
    private const string COL_UP_GROW3 = "Gold_Grow3";
    private const string COL_UP_MAX4 = "MaxLv4";
    private const string COL_UP_GROW4 = "Gold_Grow4";

    private static void BuildUpgrade()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Upgrade.csv");
        var rows = new List<UpgradeRow>();

        // CSV 재빌드 시 수동 지정한 아이콘이 유실되지 않도록 기존 SO의 값을 보존한다.
        var assetPath = Path.Combine(OutputPath, "UpgradeTable.asset");
        var existingTable = AssetDatabase.LoadAssetAtPath<UpgradeTable>(assetPath);

        var iconByCode = new Dictionary<string, Sprite>();
        if (existingTable != null && existingTable.Rows != null)
        {
            foreach (var r in existingTable.Rows)
            {
                if (string.IsNullOrEmpty(r.Code) || r.Icon == null)
                    continue;

                iconByCode[r.Code] = r.Icon;
            }
        }

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            var code = row.String(COL_UP_CODE);

            var newRow = new UpgradeRow
            {
                Code = code,
                Name = row.String(COL_UP_NAME),
                Description = row.String(COL_UP_DESC),
                Stat = row.EnumValue<StatType>(COL_UP_STAT),
                Tier = ParseTier(row.String(COL_UP_TIER)),
                MaxLevel = row.Int(COL_UP_MAX),
                GoldBase = row.Double(COL_UP_BASE),
                GoldPow = row.Double(COL_UP_POW),
                Segment1 = new UpgradeCostSegment { MaxLevel = row.Int(COL_UP_MAX1), Growth = row.Double(COL_UP_GROW1) },
                Segment2 = new UpgradeCostSegment { MaxLevel = row.Int(COL_UP_MAX2), Growth = row.Double(COL_UP_GROW2) },
                Segment3 = new UpgradeCostSegment { MaxLevel = row.Int(COL_UP_MAX3), Growth = row.Double(COL_UP_GROW3) },
                Segment4 = new UpgradeCostSegment { MaxLevel = row.Int(COL_UP_MAX4), Growth = row.Double(COL_UP_GROW4) },
            };

            if (!string.IsNullOrEmpty(code) && iconByCode.TryGetValue(code, out var icon))
            {
                newRow.Icon = icon;
            }

            rows.Add(newRow);
        }

        SaveTable<string, UpgradeRow, UpgradeTable>("UpgradeTable.asset", rows);
    }

    // Stats 테이블 컨럼명
    private const string COL_ST_LV = "Lv";
    private const string COL_ST_ATK_BASE = "ATK_Base";
    private const string COL_ST_ATK_POW = "ATK_Pow";
    private const string COL_ST_HP_BASE = "HP_Base";
    private const string COL_ST_HP_POW = "HP_Pow";
    private const string COL_ST_DEF_BASE = "DEF_Base";
    private const string COL_ST_DEF_POW = "DEF_Pow";
    private const string COL_ST_HPREC_BASE = "HPREC_Base";
    private const string COL_ST_HPREC_POW = "HPREC_Pow";
    private const string COL_ST_CR_BASE = "CR_Base";
    private const string COL_ST_CR_POW = "CR_Pow";
    private const string COL_ST_ATKT_BASE = "ATKT_Base";
    private const string COL_ST_ATKT_POW = "ATKT_Pow";
    private const string COL_ST_OFFT = "OFFT";
    private const string COL_ST_GOLDR = "GOLDR";
    private const string COL_ST_ATKR = "ATKR";
    private const string COL_ST_OFFA = "OFFA";
    private const string COL_ST_ATKSP = "ATKSP";
    private const string COL_ST_RCD = "RCD";
    private const string COL_ST_UCR_BASE = "UCR_Base";
    private const string COL_ST_UCR_POW = "UCR_Pow";
    private const string COL_ST_ATKB = "ATKB";
    private const string COL_ST_CD = "CD";
    private const string COL_ST_DEFR = "DEFR";
    private const string COL_ST_IGNDEF_BASE = "IGNDEF_Base";
    private const string COL_ST_IGNDEF_POW = "IGNDEF_Pow";

    private static void BuildStats()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Stats.csv");
        var rows = new List<StatsRow>();

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            rows.Add(new StatsRow
            {
                Level = row.Int(COL_ST_LV),
                ATK_Base = row.Double(COL_ST_ATK_BASE),
                ATK_Pow = row.Double(COL_ST_ATK_POW),
                HP_Base = row.Double(COL_ST_HP_BASE),
                HP_Pow = row.Double(COL_ST_HP_POW),
                DEF_Base = row.Double(COL_ST_DEF_BASE),
                DEF_Pow = row.Double(COL_ST_DEF_POW),
                HPREC_Base = row.Double(COL_ST_HPREC_BASE),
                HPREC_Pow = row.Double(COL_ST_HPREC_POW),
                CR_Base = row.Double(COL_ST_CR_BASE),
                CR_Pow = row.Double(COL_ST_CR_POW),
                ATKT_Base = row.Double(COL_ST_ATKT_BASE),
                ATKT_Pow = row.Double(COL_ST_ATKT_POW),
                OFFT = row.Double(COL_ST_OFFT),
                GOLDR = row.Double(COL_ST_GOLDR),
                ATKR = row.Double(COL_ST_ATKR),
                OFFA = row.Double(COL_ST_OFFA),
                ATKSP = row.Double(COL_ST_ATKSP),
                RCD = row.Double(COL_ST_RCD),
                UCR_Base = row.Double(COL_ST_UCR_BASE),
                UCR_Pow = row.Double(COL_ST_UCR_POW),
                ATKB = row.Double(COL_ST_ATKB),
                CD = row.Double(COL_ST_CD),
                DEFR = row.Double(COL_ST_DEFR),
                IGNDEF_Base = row.Double(COL_ST_IGNDEF_BASE),
                IGNDEF_Pow = row.Double(COL_ST_IGNDEF_POW)
            });
        }

        SaveTable<int, StatsRow, StatsTable>("StatsTable.asset", rows);
    }



    // Equipment 테이블 컬럼명
    private const string COL_EQ_CODE = "EquipmentCode";
    private const string COL_EQ_NAME = "EquipmentName";
    private const string COL_EQ_TYPE = "EquipmentType";
    private const string COL_EQ_GRADE = "EquipmentGrade";
    private const string COL_EQ_OPT_TYPE = "EquipOption_Type";
    private const string COL_EQ_OPT_BASE = "EquipOption_Base";
    private const string COL_EQ_OPT_UP = "EquipOption_Up";
    private const string COL_EQ_H1_TYPE = "HeldOption1_Type";
    private const string COL_EQ_H1_BASE = "HeldOption1_Base";
    private const string COL_EQ_H1_UP = "HeldOption1_Up";
    private const string COL_EQ_H2_TYPE = "HeldOption2_Type";
    private const string COL_EQ_H2_VAL = "HeldOption2";
    private const string COL_EQ_H3_TYPE = "HeldOption3_Type";
    private const string COL_EQ_H3_VAL = "HeldOption3";

    private static void BuildEquipment()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Equipment.csv");
        var rows = new List<EquipmentRow>();

        var assetPath = Path.Combine(OutputPath, "EquipmentTable.asset");
        var existingTable = AssetDatabase.LoadAssetAtPath<EquipmentTable>(assetPath);

        var iconByCode = new Dictionary<string, Sprite>();
        if (existingTable != null && existingTable.Rows != null)
        {
            foreach (var r in existingTable.Rows)
            {
                if (string.IsNullOrEmpty(r.Code) || r.Icon == null)
                    continue;

                iconByCode[r.Code] = r.Icon;
            }
        }

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            var code = row.String(COL_EQ_CODE);

            var newRow = new EquipmentRow
            {
                Code = code,
                Type = row.EnumValue<EquipmentType>(COL_EQ_TYPE),
                Name = row.String(COL_EQ_NAME),
                Grade = row.EnumValue<EquipmentGrade>(COL_EQ_GRADE),
                EquipOption = new OptionValue
                {
                    Type = row.String(COL_EQ_OPT_TYPE),
                    Base = row.Double(COL_EQ_OPT_BASE),
                    Up = row.Double(COL_EQ_OPT_UP),
                },
                HeldOption1 = new OptionValue
                {
                    Type = row.String(COL_EQ_H1_TYPE),
                    Base = row.Double(COL_EQ_H1_BASE),
                    Up = row.Double(COL_EQ_H1_UP),
                },
                HeldOption2 = new OptionValue
                {
                    Type = row.String(COL_EQ_H2_TYPE),
                    Base = row.Double(COL_EQ_H2_VAL),
                    Up = 0,
                },
                HeldOption3 = new OptionValue
                {
                    Type = row.String(COL_EQ_H3_TYPE),
                    Base = row.Double(COL_EQ_H3_VAL),
                    Up = 0,
                }
            };

            if (!string.IsNullOrEmpty(code) && iconByCode.TryGetValue(code, out var icon))
            {
                newRow.Icon = icon;
            }

            rows.Add(newRow);
        }

        SaveTable<string, EquipmentRow, EquipmentTable>("EquipmentTable.asset", rows);
    }

    // Drone 테이블 컬럼명
    private const string COL_DR_ID = "ID";
    private const string COL_DR_ATK_RATE = "AtkRate";
    private const string COL_DR_EQ_OPT_BASE = "EquipOption_Base";
    private const string COL_DR_EQ_OPT_UP = "EquipOption_Up";
    private const string COL_DR_HELD1_TYPE = "HeldOption1_Type";
    private const string COL_DR_HELD1_BASE = "HeldOption1_Base";
    private const string COL_DR_HELD1_UP = "HeldOption1_Up";

    private static void BuildDrone()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Drone.csv");
        var rows = new List<DroneRow>();

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            rows.Add(new DroneRow
            {
                ID = row.String(COL_DR_ID),
                AtkRate = row.Double(COL_DR_ATK_RATE),
                EquipOption = new OptionValue
                {
                    Type = "ATKR2",
                    Base = row.Double(COL_DR_EQ_OPT_BASE),
                    Up = row.Double(COL_DR_EQ_OPT_UP)
                },
                HeldOption1 = new OptionValue
                {
                    Type = row.String(COL_DR_HELD1_TYPE),
                    Base = row.Double(COL_DR_HELD1_BASE),
                    Up = row.Double(COL_DR_HELD1_UP)
                }
            });
        }

        SaveTable<string, DroneRow, DroneTable>("DroneTable.asset", rows);
    }

    // Evolution 테이블 컬럼명
    private const string COL_EVO_LV = "EvolutionLevel";
    private const string COL_EVO_NAME = "CharacterName";
    private const string COL_EVO_REQ = "ReqSumLevel";
    private const string COL_EVO_BONUS = "BonusATKR3";
    private const string COL_EVO_CONCEPT = "컨셉";

    private static void BuildEvolution()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Evolution.csv");
        var rows = new List<EvolutionRow>();

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            rows.Add(new EvolutionRow
            {
                EvolutionLevel = row.Int(COL_EVO_LV),
                CharacterName = row.String(COL_EVO_NAME),
                ReqSumLevel = row.Int(COL_EVO_REQ),
                BonusATKR3 = row.Double(COL_EVO_BONUS),
                Concept = row.String(COL_EVO_CONCEPT)
            });
        }

        SaveTable<int, EvolutionRow, EvolutionTable>("EvolutionTable.asset", rows);
    }

    // Skill 테이블 컬럼명
    private const string COL_SK_ID = "ID";
    private const string COL_SK_NAME = "Name";
    private const string COL_SK_DESC = "Desc";
    private const string COL_SK_NOTE = "Note";
    private const string COL_SK_COST = "Cost";
    private const string COL_SK_TIME = "Time";
    private const string COL_SK_X = "XCoord";
    private const string COL_SK_Y = "YCoord";
    private const string COL_SK_COORD = "Coord";
    private const string COL_SK_PREFIX = "Prefix";
    private const string COL_SK_FIRST = "isFirstNode";

    private static void BuildSkill()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Skill.csv");
        var rows = new List<SkillRow>();

        // CSV 재빌드 시 수동 지정한 아이콘이 유실되지 않도록 기존 SO의 값을 보존한다.
        var assetPath = Path.Combine(OutputPath, "SkillTable.asset");
        var existingTable = AssetDatabase.LoadAssetAtPath<SkillTable>(assetPath);

        var iconById = new Dictionary<string, Sprite>();
        if (existingTable != null && existingTable.Rows != null)
        {
            foreach (var r in existingTable.Rows)
            {
                if (string.IsNullOrEmpty(r.ID) || r.Icon == null)
                    continue;

                iconById[r.ID] = r.Icon;
            }
        }

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            var id = row.String(COL_SK_ID);

            var newRow = new SkillRow
            {
                ID = id,
                Name = row.String(COL_SK_NAME),
                Desc = row.String(COL_SK_DESC),
                Note = row.String(COL_SK_NOTE),
                Cost = row.Int(COL_SK_COST),
                Time = row.Int(COL_SK_TIME),
                XCoord = row.Int(COL_SK_X),
                YCoord = row.Int(COL_SK_Y),
                Coord = row.String(COL_SK_COORD),
                Prefix = row.String(COL_SK_PREFIX),
                IsFirstNode = !string.IsNullOrEmpty(row.String(COL_SK_FIRST)),
                EffectType = row.EnumValue<SkillEffectType>("EffectType"),
                Value = row.Double("Value")
            };

            // TargetStat 파싱 로직 추가
            var targetStatStr = row.String("TargetStat");
            if (newRow.EffectType == SkillEffectType.Stat)
            {
                if (System.Enum.TryParse<StatType>(targetStatStr, true, out var statType))
                {
                    newRow.TargetStat = statType;
                }
                else
                {
                    Debug.LogWarning($"[DataTableBuilder] Skill {id}: Invalid StatType '{targetStatStr}'");
                }
            }
            else if (newRow.EffectType == SkillEffectType.Special)
            {
                if (System.Enum.TryParse<SkillSpecialType>(targetStatStr, true, out var specialType))
                {
                    newRow.TargetSpecial = specialType;
                }
                else
                {
                    Debug.LogWarning($"[DataTableBuilder] Skill {id}: Invalid SkillSpecialType '{targetStatStr}'");
                }
            }

            if (!string.IsNullOrEmpty(id) && iconById.TryGetValue(id, out var icon))
            {
                newRow.Icon = icon;
            }

            rows.Add(newRow);
        }

        SaveTable<string, SkillRow, SkillTable>("SkillTable.asset", rows);
    }

    private static void BuildGachaEquipment()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Gacha_Equipment.csv");

        // CSV를 읽어서 레벨별로 그룹화
        var levelData = new Dictionary<int, List<GradeProbability>>();

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            var grade = row.EnumValue<EquipmentGrade>("Level"); // CSV의 "Level" 컬럼은 실제로 등급

            for (int level = 1; level <= 50; level++)  // Max 50 levels safety break
            {
                try
                {
                    var prob = row.Float(level.ToString());
                    if (prob >= 0)
                    {
                        if (!levelData.ContainsKey(level))
                            levelData[level] = new List<GradeProbability>();

                        levelData[level].Add(new GradeProbability
                        {
                            Grade = grade,
                            Probability = prob
                        });
                    }
                }
                catch (KeyNotFoundException)
                {
                    break;
                }
            }
        }

        // 레벨별 Row 생성
        var rows = new List<GachaEquipmentRow>();
        foreach (var kvp in levelData)
        {
            rows.Add(new GachaEquipmentRow
            {
                Level = kvp.Key,
                Probabilities = kvp.Value
            });
        }

        SaveTable<int, GachaEquipmentRow, GachaEquipmentTable>("GachaEquipmentTable.asset", rows);
    }

    private static void BuildGachaDrone()
    {
        var path = Path.Combine(CsvPath, "AI 로봇 키우기 DB - Gacha_Drone.csv");

        // CSV를 읽어서 레벨별로 그룹화
        var levelData = new Dictionary<int, List<DroneProbability>>();

        foreach (var row in CsvUtil.Read(path, skipHeader: true))
        {
            var droneID = row.String("Level"); // CSV의 "Level" 컬럼은 실제로 드론 ID

            for (int level = 1; level <= 50; level++) // Max 50 levels safety break
            {
                try
                {
                    var prob = row.Float(level.ToString());
                    if (prob >= 0)
                    {
                        if (!levelData.ContainsKey(level))
                            levelData[level] = new List<DroneProbability>();

                        levelData[level].Add(new DroneProbability
                        {
                            ID = droneID,
                            Probability = prob
                        });
                    }
                }
                catch (KeyNotFoundException)
                {
                    break;
                }
            }
        }

        // 레벨별 Row 생성
        var rows = new List<GachaDroneRow>();
        foreach (var kvp in levelData)
        {
            rows.Add(new GachaDroneRow
            {
                Level = kvp.Key,
                Probabilities = kvp.Value
            });
        }

        SaveTable<int, GachaDroneRow, GachaDroneTable>("GachaDroneTable.asset", rows);
    }

    private static void SaveTable<TKey, TValue, TTable>(string fileName, List<TValue> rows)
        where TTable : TableBase<TKey, TValue>
    {
        var assetPath = Path.Combine(OutputPath, fileName);
        var table = AssetDatabase.LoadAssetAtPath<TTable>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<TTable>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        table.SetRows(rows);
        EditorUtility.SetDirty(table);
    }

    private static UpgradeTier ParseTier(string raw)
    {
        var value = (raw ?? string.Empty).Trim().Trim('"');
        return value switch
        {
            "일반" => UpgradeTier.Normal,
            "슈퍼" => UpgradeTier.Super,
            "울트라" => UpgradeTier.Ultra,
            "슈퍼울트라" => UpgradeTier.SuperUltra,
            _ => throw new System.ArgumentException($"Unknown UpgradeTier value '{raw}'")
        };
    }


}

