using System;
using System.Collections.Generic;
using BreakInfinity;
using UnityEngine;

namespace SahurRaising.Core
{
    // 전역 스탯/재화/몬스터 분류 열거형
    public enum StatType
    {
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
        Weapon,

        // Temp
        EquipmentType2,
        EquipmentType3,
        EquipmentType4,
        EquipmentType5,
        EquipmentType6,
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
    }

    [Serializable]
    public struct GachaDroneRow
    {
        public string ID;
        public List<float> Probabilities;
    }

    [Serializable]
    public struct GachaEquipmentRow
    {
        public EquipmentGrade Grade;
        public List<float> Probabilities;
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
    public struct EquipmentInventoryEntry
    {
        public string Code;
        public int Level;
        public int Count;
    }

    [Serializable]
    public struct EquipmentInventoryInfo
    {
        public int Level;
        public int Count;

        public EquipmentInventoryInfo(int level, int count)
        {
            Level = level;
            Count = count;
        }
    }

    [Serializable]
    public class EquipmentSaveData
    {
        // 장착한 장비 (장비 타입별로 1개씩)
        public string EquippedWeapon;

        // 6종류를 지원하기 위해 추가 슬롯 (필요시 확장)
        public string EquippedSlot2;
        public string EquippedSlot3;
        public string EquippedSlot4;
        public string EquippedSlot5;
        public string EquippedSlot6;

        // 모든 장비의 소지 개수
        public List<EquipmentInventoryEntry> Inventory = new();

        // 이미 본 장비(NEW가 꺼진 장비) 코드 목록
        public List<string> SeenCodes = new();
    }
}