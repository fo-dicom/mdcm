// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2009  Colby Dillion
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
using System.Collections.Generic;
using System.Threading;

namespace Dicom.Utility {
	public static class MultiThread {
		public delegate void ProcessDelegate();
#if !SILVERLIGHT
		public static void ProcessCallback(IAsyncResult result) {
			((ProcessDelegate)result.AsyncState).EndInvoke(result);
		}
#endif

        public delegate void ForDelegate(int n);
		public static void For(int start, int end, ForDelegate action) {
			For(start, end, 4, action);
		}
		public static void For(int start, int end, int chunkSize, ForDelegate action) {
			object oLock = new object();
			ProcessDelegate process = delegate() {
				for (int n = 0; n < end;) {
					lock (oLock) {
						n = start;
						start += chunkSize;
					}

					for (int k = 0; k < chunkSize && n < end; k++, n++) {
						action(n);
					}
				}
			};

			int threads = Environment.ProcessorCount;
			WaitHandle[] handles = new WaitHandle[threads];
			for (int i = 0; i < threads; i++) {
#if SILVERLIGHT
                handles[i] = new ManualResetEvent(false);
			    ThreadPool.QueueUserWorkItem(delegate(object state)
			                                     {
			                                         process();
			                                         ((ManualResetEvent)state).Set();
			                                     }, handles[i]);
#else
				handles[i] = process.BeginInvoke(ProcessCallback, process).AsyncWaitHandle;
#endif
			}
			WaitHandle.WaitAll(handles);
		}

		public delegate void ForEachDelegate<T>(T item);
		public static void ForEach<T>(IEnumerable<T> enumerable, ForEachDelegate<T> action) {
			IEnumerator<T> enumerator = enumerable.GetEnumerator();
			object oLock = new object();

			ProcessDelegate process = delegate() {
				while (true) {
					T current;

					lock (oLock) {
						if (!enumerator.MoveNext())
							return;
						current = enumerator.Current;
					}

					action(current);
				}
			};

			int threads = Environment.ProcessorCount;
			WaitHandle[] handles = new WaitHandle[threads];
			for (int i = 0; i < threads; i++) {
#if SILVERLIGHT
                handles[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(delegate(object state)
                                                 {
                                                     process();
                                                     ((ManualResetEvent)state).Set();
                                                 }, handles[i]);
#else
				handles[i] = process.BeginInvoke(ProcessCallback, process).AsyncWaitHandle;
#endif
			}
			WaitHandle.WaitAll(handles);
		}
	}
}
