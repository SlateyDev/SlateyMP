using System;
using System.Collections.Concurrent;

namespace SlateyMP.Framework.Util
{
	public class ObjectPool<T> where T : new() {
		private static ConcurrentBag<T> _objects = new ConcurrentBag<T>();
        private static Func<T> _objectGenerator = () => new T();

        public static T CreateObject(Func<T> generator) {
            _objectGenerator = generator;
            return CreateObject();
        }
        
		public static T CreateObject() {
			if (_objects.TryTake(out T item))
				return item;
			return _objectGenerator();
		}

		public static void ReclaimObject(T item) {
			_objects.Add(item);
		}
	}
}
