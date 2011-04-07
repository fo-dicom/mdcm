// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Author:
//    Colby Dillion (colby.dillion@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dicom.Utility {
	public interface IPreloadable<Tstate> {
		bool IsLoaded { get; }
		void Load(Tstate state);
	}

	public class PreloadQueue<Titem, Tstate> : IEnumerable<Titem> where Titem : class, IPreloadable<Tstate> {
		#region Private Members
		private Queue<Titem> _queue;
		private object _queueLock;
		private Tstate _state;
		#endregion

		#region Public Constructor
		public PreloadQueue(Tstate state) {
			_queue = new Queue<Titem>();
			_queueLock = new object();
			_state = state;
		}
		#endregion

		#region Public Properties
		public int Count {
			get {
				lock (_queueLock) {
					return _queue.Count;
				}
			}
		}
		#endregion

		#region Public Methods
		public Titem Dequeue() {
			Titem item = default(Titem);
			lock (_queueLock) {
				if (_queue.Count == 0)
					throw new ArgumentOutOfRangeException("No items in preload queue");
				item = _queue.Dequeue();				
			}
			if (!item.IsLoaded) {
				lock (item) {
					if (!item.IsLoaded)
						item.Load(_state);
				}
			}
			return item;
		}

		public void Enqueue(Titem item) {
			lock (_queueLock) {
				_queue.Enqueue(item);
			}
		}

		public void Enqueue(ICollection<Titem> items) {
			lock (_queueLock) {
				foreach (Titem item in items) {
					_queue.Enqueue(item);
				}
			}
		}

		public void UnloadTo(IList<Titem> destination) {
			lock (_queueLock) {
				while (_queue.Count > 0)
					destination.Add(_queue.Dequeue());
			}
		}

		public void Preload(int count) {
			if (count == 0)
				return;

			lock (_queueLock) {
				if (count < 0)
					count = _queue.Count;

				foreach (Titem item in _queue) {
					if (count-- <= 0)
						return;

					if (!item.IsLoaded)
						ThreadPool.QueueUserWorkItem(PreloadProc, item);
				}
			}
		}

		public void Sort() {
			lock (_queueLock) {
				List<Titem> list = new List<Titem>();
				UnloadTo(list);
				list.Sort();
				Enqueue(list);
			}
		}

		public void Sort(IComparer<Titem> comparer) {
			lock (_queueLock) {
				List<Titem> list = new List<Titem>();
				UnloadTo(list);
				list.Sort(comparer);
				Enqueue(list);
			}
		}

		public void Sort(Comparison<Titem> comparison) {
			lock (_queueLock) {
				List<Titem> list = new List<Titem>();
				UnloadTo(list);
				list.Sort(comparison);
				Enqueue(list);
			}
		}

		public IEnumerator<Titem> GetEnumerator() {
			return _queue.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return _queue.GetEnumerator();
		}
		#endregion

		#region Private Methods
		private void PreloadProc(object state) {
			try {
				Titem item = (Titem)state;
				if (item.IsLoaded)
					return;
				lock (item) {
					if (item.IsLoaded)
						return;
					item.Load(_state);
				}
			}
			catch {
			}
		}
		#endregion
	}
}
