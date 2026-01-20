using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 제네릭 오브젝트 풀 인터페이스
    /// GC 할당을 최소화하기 위해 오브젝트를 재사용합니다.
    /// </summary>
    public interface IObjectPool<T> where T : class
    {
        T Get();
        void Release(T obj);
        void Clear();
        int CountActive { get; }
        int CountInactive { get; }
    }

    /// <summary>
    /// MonoBehaviour 전용 오브젝트 풀
    /// 프리팹 기반으로 인스턴스를 생성/재사용합니다.
    /// </summary>
    public class MonoObjectPool<T> : IObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _pool = new();
        private readonly HashSet<T> _activeObjects = new();
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly int _maxSize;

        public int CountActive => _activeObjects.Count;
        public int CountInactive => _pool.Count;

        /// <summary>
        /// 오브젝트 풀 생성자
        /// </summary>
        /// <param name="prefab">복제할 프리팹</param>
        /// <param name="parent">생성된 오브젝트의 부모 Transform</param>
        /// <param name="initialSize">초기 풀 크기</param>
        /// <param name="maxSize">최대 풀 크기 (0 = 무제한)</param>
        /// <param name="onGet">Get 시 호출되는 콜백</param>
        /// <param name="onRelease">Release 시 호출되는 콜백</param>
        public MonoObjectPool(
            T prefab,
            Transform parent = null,
            int initialSize = 0,
            int maxSize = 100,
            Action<T> onGet = null,
            Action<T> onRelease = null)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _onGet = onGet;
            _onRelease = onRelease;

            // 초기 풀 채우기
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
            }
        }

        public T Get()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = CreateNew();
            }

            obj.gameObject.SetActive(true);
            _activeObjects.Add(obj);
            _onGet?.Invoke(obj);

            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;
            if (!_activeObjects.Contains(obj)) return;

            _activeObjects.Remove(obj);
            _onRelease?.Invoke(obj);
            obj.gameObject.SetActive(false);

            // 최대 크기 제한
            if (_maxSize > 0 && _pool.Count >= _maxSize)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
                return;
            }

            _pool.Push(obj);
        }

        public void Clear()
        {
            foreach (var obj in _activeObjects)
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }
            _activeObjects.Clear();

            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        private T CreateNew()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            return obj;
        }
    }
}
