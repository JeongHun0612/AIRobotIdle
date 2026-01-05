using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 공통 테이블 베이스: List 직렬화 + 런타임 인덱스(Dictionary) 구성
    /// </summary>
    public abstract class TableBase<TKey, TValue> : ScriptableObject
    {
        [SerializeField] private List<TValue> _rows = new();

        [NonSerialized] private readonly Dictionary<TKey, TValue> _index = new();

        public IReadOnlyList<TValue> Rows => _rows;
        public IReadOnlyDictionary<TKey, TValue> Index => _index;

        protected abstract TKey GetKey(TValue value);

        protected virtual void OnEnable()
        {
            BuildIndex();
        }

        [ContextMenu("Rebuild Index")]
        protected void BuildIndex()
        {
            _index.Clear();
            foreach (var row in _rows)
            {
                var key = GetKey(row);
                if (_index.ContainsKey(key))
                    continue;

                _index.Add(key, row);
            }
        }

        /// <summary>
        /// 에디터 변환기에서 일괄 세팅 후 인덱스를 재구성하기 위한 진입점
        /// </summary>
        public void SetRows(List<TValue> rows)
        {
            _rows = rows ?? new List<TValue>();
            BuildIndex();
        }
    }
}

