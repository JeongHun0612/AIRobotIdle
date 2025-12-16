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

        // TODO 하단 레이어 탭
        Equipment = 10,

        // TODO 공용
        Gacha = 20,
    }
}
