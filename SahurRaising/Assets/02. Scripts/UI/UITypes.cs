using UnityEngine;

namespace SahurRaising.UI
{
    public enum ESceneUIType
    {
        None,
        Loading,
        MainBattle
    }

    public enum EPopupUIType
    {
        None = 0,
        Setting,

        // 하단 레이어 탭
        Equipment = 10,
        Skill = 11,
        Enhance = 12,
        Shop = 13,
        Drone = 14,

        GachaResult = 30,

        AdvanceResult = 40,

        // 공용
        //Temp = 20,

    }
}
