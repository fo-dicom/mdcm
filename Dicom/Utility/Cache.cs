using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dicom.Utility {
	public interface ICache<T> {
		T this[string key] {
			get;
			set;
		}
	}

	public class LeastRecentUsedCache<T> : ICache<T>, IDictionary<string, T> {
		#region Private Members
		private const int LockTimeout = 1000; // 1 second

		private int _maxItems;
		private List<string> _removalQueue;
		private Dictionary<string, T> _index;
		private object _lock;
		#endregion

		#region Public Constructors
		public LeastRecentUsedCache(int maxItems) {
			_maxItems = maxItems;
			_removalQueue = new List<string>(_maxItems);
			_index = new Dictionary<string, T>(_maxItems);
			_lock = new object();
		}
		#endregion

		#region Public Properties
		public int MaximumItems {
			get { return _maxItems; }
		}

		public ICollection<string> Keys {
			get { return _index.Keys; }
		}

		public ICollection<T> Values {
			get { return _index.Values; }
		}

		public int Count {
			get { return _index.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public T this[string key] {
			get {
				lock (_lock) {
					if (_index.ContainsKey(key)) {
						_removalQueue.Remove(key);
						_removalQueue.Add(key);
						return _index[key];
					}
					return default(T);
				}
			}
			set {
				Add(key, value);
			}
		}
		#endregion

		#region Public Members
		public void Add(string key, T value) {
			lock (_lock) {
				if (_index.ContainsKey(key)) {
					_removalQueue.Remove(key);
				}
				else {
					if (_removalQueue.Count == MaximumItems) {
						string removeKey = _removalQueue[0];
						Remove(removeKey);
					}
				}

				_index[key] = value;
				_removalQueue.Add(key);
			}
		}

		public void Add(KeyValuePair<string, T> item) {
			Add(item.Key, item.Value);
		}

		public bool Contains(KeyValuePair<string, T> item) {
			return ContainsKey(item.Key);
		}

		public bool Remove(string key) {
			lock (_lock) {
				bool ret = _index.Remove(key);
				_removalQueue.Remove(key);
				return ret;
			}
		}

		public bool Remove(KeyValuePair<string, T> item) {
			return Remove(item.Key);
		}

		public bool ContainsKey(string key) {
			lock (_lock) {
				return _index.ContainsKey(key);
			}
		}

		public bool TryGetValue(string key, out T value) {
			lock (_lock) {
				if (_index.TryGetValue(key, out value)) {
					_removalQueue.Remove(key);
					_removalQueue.Add(key);
					return true;
				}
				return false;
			}
		}

		public void Clear() {
			lock (_lock) {
				_index.Clear();
				_removalQueue.Clear();
			}
		}

		public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
			return _index.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return _index.GetEnumerator();
		}

		public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) {
			lock (_lock) {
				foreach (KeyValuePair<string, T> kv in _index) {
					array[arrayIndex++] = kv;
				}
			}
		}
		#endregion
	}
}
