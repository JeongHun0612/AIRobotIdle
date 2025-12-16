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
        Armor,
        Accessory //필요할거같아서 일단추가
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
}

