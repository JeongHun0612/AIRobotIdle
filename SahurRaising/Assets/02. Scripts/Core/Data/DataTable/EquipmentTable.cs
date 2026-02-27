using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "EquipmentTable", menuName = "SahurRaising/Data/EquipmentTable")]
    public class EquipmentTable : TableBase<string, EquipmentRow>
    {
        // 등급별 인덱스
        [System.NonSerialized]
        private Dictionary<EquipmentGrade, List<EquipmentRow>> _equipmentByGrade;

        protected override string GetKey(EquipmentRow value) => value.Code;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_equipmentByGrade == null)
            {
                BuildGradeIndex();
            }
        }

        [ContextMenu("Rebuild Grade Index")]
        private void BuildGradeIndex()
        {
            _equipmentByGrade = new Dictionary<EquipmentGrade, List<EquipmentRow>>();

            foreach (var row in Rows)
            {
                if (!_equipmentByGrade.ContainsKey(row.Grade))
                {
                    _equipmentByGrade[row.Grade] = new List<EquipmentRow>();
                }

                _equipmentByGrade[row.Grade].Add(row);
            }
        }

        /// <summary>
        /// 등급별 장비 목록을 가져옵니다.
        /// </summary>
        /// <param name="grade">장비 등급</param>
        /// <returns>해당 등급의 장비 리스트 (없으면 빈 리스트)</returns>
        public IReadOnlyList<EquipmentRow> GetByGrade(EquipmentGrade grade)
        {
            if (_equipmentByGrade == null)
            {
                BuildGradeIndex();
            }

            if (_equipmentByGrade != null && _equipmentByGrade.TryGetValue(grade, out var equipmentList))
            {
                return equipmentList;
            }

            return new List<EquipmentRow>();
        }
    }
}

