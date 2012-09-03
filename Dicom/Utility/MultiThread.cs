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
#if !SILVERLIGHT
using System.Threading.Tasks;
#endif

namespace Dicom.Utility {
	public static class MultiThread {
		public delegate void ProcessDelegate();
#if !SILVERLIGHT
		public static void ProcessCallback(IAsyncResult result) {
			((ProcessDelegate)result.AsyncState).EndInvoke(result);
		}
#endif

		public static void For(int start, int end, Action<int> action) {
#if SILVERLIGHT
			object oLock = new object();
			ProcessDelegate process = delegate() {
				for (int n = 0; n < end;) {
					lock (oLock) {
						n = start;
						start++;
					}

					action(n);
				}
			};

			int threads = Environment.ProcessorCount;
			WaitHandle[] handles = new WaitHandle[threads];
			for (int i = 0; i < threads; i++) {

                handles[i] = new ManualResetEvent(false);
			    ThreadPool.QueueUserWorkItem(delegate(object state)
			                                     {
			                                         process();
			                                         ((ManualResetEvent)state).Set();
			                                     }, handles[i]);
			}
			WaitHandle.WaitAll(handles);
#else
			Parallel.For(start, end, action);
#endif
			
		}

		public static void ForEach<T>(IEnumerable<T> enumerable, Action<T> action) {
#if SILVERLIGHT
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

                handles[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(delegate(object state)
                                                 {
                                                     process();
                                                     ((ManualResetEvent)state).Set();
                                                 }, handles[i]);
			}
			WaitHandle.WaitAll(handles);
#else
			Parallel.ForEach(enumerable, action);
#endif
		}
	}
}
