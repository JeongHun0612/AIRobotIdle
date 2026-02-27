using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    // Equipment 뽑기 요청
    [Serializable]
    public class PullEquipmentRequest
    {
        public int level;
        public int count;
    }

    // Equipment 뽑기 응답
    [Serializable]
    public class PullEquipmentResponse
    {
        public List<EquipmentGachaResultData> results;
    }

    [Serializable]
    public class EquipmentGachaResultData
    {
        public string itemCode;
        public string gradeKey;
        public string typeKey;
    }

    // Drone 뽑기 요청
    [Serializable]
    public class PullDroneRequest
    {
        public int level;
        public int count;
    }

    // Drone 뽑기 응답
    [Serializable]
    public class PullDroneResponse
    {
        public List<DroneGachaResultData> results;
    }

    [Serializable]
    public class DroneGachaResultData
    {
        public string itemCode;
        public string gradeKey;
    }
}
