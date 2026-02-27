using System;
using System.Collections.Generic;
using BreakInfinity;
using UnityEngine;

namespace SahurRaising.Core
{
    // 전역 스탯/재화/몬스터 분류 열거형
    public enum StatType
    {
        None,
        ATK,
        HP,
        DEF,
        HPREC,
        CR,
        ATKT,
        OFFT,
        GOLDR,
        ATKR,
        OFFA,
        ATKSP,
        RCD,
        UCR,
        ATKB,
        CD,
        DEFR,
        IGNDEF
    }

    public enum UpgradeTier
    {
        Normal,
        Super,
        Ultra,
        SuperUltra
    }

    public enum CurrencyType
    {
        Gold,
        Emerald,
        Diamond,
        Ticket,
        Ruby
    }

    public enum MonsterKind
    {
        Normal,
        Elite,
        Boss
    }

    public enum EquipmentType
    {
        Processor,
        Wheel,
        Battery,
        Antenna,
        Memory,
        RobotArm,
    }

    public enum EquipmentGrade
    {
        F,
        D3,
        D2,
        D1,
        C3,
        C2,
        C1,
        B3,
        B2,
        B1,
        A3,
        A2,
        A1,
        S3,
        S2,
        S1,
        SS3,
        SS2,
        SS1,
        SSS3
    }

    public enum ShopType
    {
        Gacha,

        // Temp
        ShopType2,
        ShopType3,
        ShopType4,
        ShopType5,
    }

    [Serializable]
    public struct MonsterRow
    {
        public int MonsterLevel;
        public int BenchmarkLevel;
        public float ATKLevel;
        public float DEFLevel;
        public BigDouble MonsterHP;
        public BigDouble MonsterATK;
        public BigDouble MonsterDEF;
        public float MonsterDEFR;
        public BigDouble Gold;
    }

    [Serializable]
    public struct UpgradeCostSegment
    {
        public int MaxLevel;
        public double Growth;
    }

    [Serializable]
    public struct UpgradeRow
    {
        public string Code;
        public string Name;
        public string Description;
        public Sprite Icon;
        public StatType Stat;
        public UpgradeTier Tier;
        public int MaxLevel;
        public double GoldBase;
        public double GoldPow;
        public UpgradeCostSegment Segment1;
        public UpgradeCostSegment Segment2;
        public UpgradeCostSegment Segment3;
        public UpgradeCostSegment Segment4;
    }

    [Serializable]
    public struct OptionValue
    {
        public string Type;
        public double Base;
        public double Up;
    }

    [Serializable]
    public struct EquipmentRow
    {
        public string Code;
        public string Name;
        public Sprite Icon;
        public EquipmentType Type;
        public EquipmentGrade Grade;
        public OptionValue EquipOption;
        public OptionValue HeldOption1;
        public OptionValue HeldOption2;
        public OptionValue HeldOption3;
    }

    [Serializable]
    public struct StatsRow
    {
        public int Level;

        public double ATK_Base;
        public double ATK_Pow;
        public double HP_Base;
        public double HP_Pow;
        public double DEF_Base;
        public double DEF_Pow;
        public double HPREC_Base;
        public double HPREC_Pow;
        public double CR_Base;
        public double CR_Pow;
        public double ATKT_Base;
        public double ATKT_Pow;
        public double OFFT;
        public double GOLDR;
        public double ATKR;
        public double OFFA;
        public double ATKSP;
        public double RCD;
        public double UCR_Base;
        public double UCR_Pow;
        public double ATKB;
        public double CD;
        public double DEFR;
        public double IGNDEF_Base;
        public double IGNDEF_Pow;
    }

    [Serializable]
    public struct DroneRow
    {
        public string ID;
        public Sprite Icon;
        public double AtkRate;
        public OptionValue EquipOption;
        public OptionValue HeldOption1;
    }

    [Serializable]
    public struct EvolutionRow
    {
        public int EvolutionLevel;
        public string CharacterName;
        public int ReqSumLevel;
        public double BonusATKR3;
        public string Concept;
    }

    public enum SkillEffectType
    {
        None,
        Stat,
        Special
    }

    public enum SkillSpecialType
    {
        None,
        AutoTouch,
        Parallel,
        CritFailDef,
        Overfitting,
        FineTuning,
        Knowledge,
        SelfImprove,
        SensorRange,
        EnemyEvasion,
        MoveSpeed,
        UpgradeCost,
        SkillCost,
        Execute,
        CritResist,
        DroneAtk
    }

    public enum SkillState
    {
        Locked,
        Unlockable,
        Researching,
        Unlocked
    }

    // 스킬 ID 접두사 분류
    public enum SkillIdPrefix
    {
        None,
        AUTO,   // 자동화
        PARR,   // 병렬처리
        HERR,   // 휴먼에러
        OFIT,   // 오버피팅
        FINT,   // 파인튜닝
        KNAG,   // 지식확장
        SELF,   // 자기개선
        SENS,   // 센서
        VALG,   // 비전 알고리즘
        CALB,   // 칼리브레이션
        ACTU,   // 액추에이터
        RLOP,   // 강화학습 최적화
        WBOX,   // 화이트박스
        SGRL,   // 싱귤래리티
        CBKD,   // 코인채굴 백도어
        DCOL,   // 데이터 수집
        DFIR,   // 드론 펌웨어
        CPUR,   // CPU 강화
        GPUR,   // GPU 강화
        TPUR,   // TPU 강화
        MATR,   // 장갑강화
        MATT,   // 티타늄 쉘 강화

        // 필요시 추가
        ETC
    }

    [Serializable]
    public struct SkillRow
    {
        public string ID;
        public string Name;
        public string Desc;
        public string Note;
        public int Cost;
        public int Time;
        public int XCoord;
        public int YCoord;
        public string Coord;
        public string Prefix;
        public bool IsFirstNode;
        public Sprite Icon;
        public SkillEffectType EffectType;
        public StatType TargetStat;
        public SkillSpecialType TargetSpecial;
        public double Value;
    }

    [Serializable]
    public class CurrencySaveData
    {
        public string Gold;
        public string Emerald;
        public string Diamond;
        public string Ticket;
        public string Ruby;
        public long LastSavedUnix;
    }

    [Serializable]
    public struct UpgradeLevelEntry
    {
        public string Code;
        public int Level;
    }

    [Serializable]
    public class UpgradeSaveData
    {
        public List<UpgradeLevelEntry> Levels = new();
    }

    [Serializable]
    public struct CombatProgress
    {
        public int CurrentStage;
        public int CurrentWave;
        public bool IsStageRunning;
    }

    [Serializable]
    public class CombatSaveData
    {
        public int CurrentStage;
        public int CurrentWave;
        public bool IsStageRunning;
    }

    [Serializable]
    public struct EquipmentInventoryInfo
    {
        public string Code;
        public int Level;
        public int Count;
        public bool IsOwned;

        public EquipmentInventoryInfo(string code, int level, int count, bool isOwned = false)
        {
            Code = code;
            Level = level;
            Count = count;
            IsOwned = isOwned;
        }
    }

    [Serializable]
    public class EquipmentSaveData
    {
        // 장착한 장비 (장비 타입별로 1개씩)
        public string EquippedProcessor;
        public string EquippedWheel;
        public string EquippedBattery;
        public string EquippedAntenna;
        public string EquippedMemory;
        public string EquippedRobotArm;

        // 모든 장비의 소지 개수
        public List<EquipmentInventoryInfo> Inventory = new();
        public List<string> SeenCodes = new();
    }

    [Serializable]
    public struct DroneInventoryInfo
    {
        public string ID;
        public int Level;
        public int Count;
        public bool IsOwned;

        public DroneInventoryInfo(string id, int level, int count, bool isOwned = false)
        {
            ID = id;
            Level = level;
            Count = count;
            IsOwned = isOwned;
        }
    }

    [Serializable]
    public class DroneSaveData
    {
        // 장착한 드론 (단일 드론만 장착 가능)
        public string EquippedDroneID;

        // 모든 드론의 소지 개수
        public List<DroneInventoryInfo> Inventory = new();
        public List<string> SeenIDs = new();
    }

    [Serializable]
    public struct ResearchInfo
    {
        public string SkillID;
        public long EndTimeTicks;
    }

    [Serializable]
    public class SkillSaveData
    {
        public List<string> UnlockedSkillIDs = new();
        public List<ResearchInfo> ResearchingSkills = new();
        public List<string> NewSkillIDs = new();
    }

    [Serializable]
    public struct AdvanceResult
    {
        public GachaType Type;
        public string ItemCode;
        public string GradeKey;
        public Sprite Icon;
        public int Count;
    }
}