using System;
using System.Collections.Generic;

namespace xpTURN.Link
{
    /// <summary>
    /// ObjectPool is a class that manages reusable objects.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private int _usedCount = 0;
        private readonly Stack<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _objectResetter;

        public ObjectPool(Func<T> objectGenerator, Action<T> objectResetter = null, int poolCapacity = 10)
        {
            _objects = new Stack<T>(poolCapacity);
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objectResetter = objectResetter;
        }

        public T Get()
        {
            _usedCount++;
            return _objects.Count > 0 ? _objects.Pop() : _objectGenerator();
        }

        public void Release(T item)
        {
            _usedCount--;
            _objectResetter?.Invoke(item);
            _objects.Push(item);
        }
 
        public int MaxCount => _objects.Count + _usedCount;
        public int UsedCount => _usedCount;
        public int Remaining => _objects.Count;
   }
}