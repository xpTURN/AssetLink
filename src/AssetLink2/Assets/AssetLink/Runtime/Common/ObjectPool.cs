using System;
using System.Collections.Generic;

namespace xpTURN.AssetLink
{
    /// <summary>
    /// ObjectPool is a class that manages reusable objects.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private int _usedCount = 0;
        private int _poolCapacity;
        private Stack<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _objectResetter;

        public ObjectPool(Func<T> objectGenerator, Action<T> objectResetter = null, int poolCapacity = 10)
        {
            if (poolCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(poolCapacity), "poolCapacity must be non-negative.");

            _poolCapacity = poolCapacity;
            _objects = new Stack<T>(poolCapacity);
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objectResetter = objectResetter;
        }

        public void SetPoolCapacity(int poolCapacity)
        {
            if (poolCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(poolCapacity), "poolCapacity must be non-negative.");

            _poolCapacity = poolCapacity;

            // Recreate stack with new capacity
            var newStack = new Stack<T>(poolCapacity);

            // Move existing items to new stack (exclude items exceeding capacity)
            var itemsToKeep = Math.Min(_objects.Count, poolCapacity);
            var temp = new T[itemsToKeep];
            for (int i = 0; i < itemsToKeep; i++)
                temp[i] = _objects.Pop();

            // Push in reverse order to preserve original order
            for (int i = itemsToKeep - 1; i >= 0; i--)
                newStack.Push(temp[i]);

            _objects = newStack;
        }

        public T Get()
        {
            _usedCount++;
            return _objects.Count > 0 ? _objects.Pop() : _objectGenerator();
        }

        public void Release(T item)
        {
            _usedCount = Math.Max(0, _usedCount - 1);
            _objectResetter?.Invoke(item);

            // If capacity is exceeded, discard item for GC
            if (_objects.Count < _poolCapacity)
                _objects.Push(item);
        }

        public int Capacity => _poolCapacity;
        public int MaxCount => _objects.Count + _usedCount;
        public int UsedCount => _usedCount;
        public int Remaining => _objects.Count;
    }
}