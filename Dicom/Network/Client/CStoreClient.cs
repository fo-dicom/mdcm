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
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;

using Dicom;
using Dicom.Codec;
using Dicom.Data;
using Dicom.Network;
using Dicom.Utility;

namespace Dicom.Network.Client {
	internal enum CStoreRequestResult {
		Success,
		Reassociate,
		Failure
	}

	/// <summary>
	/// C-Store Request Information
	/// </summary>
	public class CStoreRequestInfo : IPreloadable<CStoreClient> {
		#region Private Members
		private bool _loaded;
		private string _fileName;
		private DicomTransferSyntax _transferSyntax;
		private DicomTransferSyntax _originalTransferSyntax;
		private DcmDataset _dataset;
		private Exception _exception;
		private object _userState;
		private DcmStatus _status;
		private DicomUID _sopClass;
		private DicomUID _sopInst;
		private uint _datasetSize;
		#endregion

		#region Public Constructors
		public CStoreRequestInfo(string fileName) : this(fileName, null) {
		}

		public CStoreRequestInfo(string fileName, object userModel) {
			try {
				_fileName = fileName;
#if SILVERLIGHT
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.FileExists(fileName))
                        throw new FileNotFoundException("Unable to load DICOM file!");
#else
                    using (var store = IsolatedStorageFile.)
				    if (!File.Exists(fileName))
					    throw new FileNotFoundException("Unable to load DICOM file!", fileName);
#endif

                    DicomTag stopTag = (userModel != null) ? DicomTags.PixelData : DcmFileMetaInfo.StopTag;
                    DicomFileFormat ff = new DicomFileFormat();
                    ff.Load(fileName, stopTag, DicomReadOptions.Default);
                    _transferSyntax = ff.FileMetaInfo.TransferSyntax;
                    _originalTransferSyntax = _transferSyntax;
                    _sopClass = ff.FileMetaInfo.MediaStorageSOPClassUID;
                    _sopInst = ff.FileMetaInfo.MediaStorageSOPInstanceUID;
                    if (userModel != null)
                    {
                        ff.Dataset.LoadDicomFields(userModel);
                        _userState = userModel;
                    }
                    _status = DcmStatus.Pending;
#if SILVERLIGHT
                }
#endif
			}
			catch (Exception e) {
				_status = DcmStatus.ProcessingFailure;
				_exception = e;
				throw;
			}
		}
		#endregion

		#region Public Properties
		public bool IsLoaded {
			get { return _loaded; }
		}

		public string FileName {
			get { return _fileName; }
		}

		public DcmDataset Dataset {
			get { return _dataset; }
		}

		public uint DatasetSize {
			get { return _datasetSize; }
		}

		public DicomUID SOPClassUID {
			get { return _sopClass; }
		}

		public DicomUID SOPInstanceUID {
			get { return _sopInst; }
		}

		public DicomTransferSyntax TransferSyntax {
			get { return _transferSyntax; }
		}

		public bool HasError {
			get { return _exception != null; }
		}

		public Exception Error {
			get { return _exception; }
		}

		public DcmStatus Status {
			get { return _status; }
			internal set { _status = value; }
		}

		public object UserState {
			get { return _userState; }
			set { _userState = value; }
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Loads the DICOM file and changes the transfer syntax if needed. (Internal)
		/// </summary>
		/// <param name="client">C-Store Client</param>
		public void Load(CStoreClient client) {
			if (_loaded)
				return;

			try {
				DicomTransferSyntax tx = null;

				foreach (DcmPresContext pc in client.Associate.GetPresentationContexts()) {
					if (pc.Result == DcmPresContextResult.Accept && pc.AbstractSyntax == _sopClass) {
						tx = pc.AcceptedTransferSyntax;
						break;
					}
				}

				if (tx == null)
					throw new DicomNetworkException("No accepted presentation contexts for abstract syntax: " + _sopClass.Description);

				// Possible to stream from file?
				if (!client.DisableFileStreaming && tx == TransferSyntax) {
					using (FileStream fs = DicomFileFormat.GetDatasetStream(_fileName)) {
						_datasetSize = Convert.ToUInt32(fs.Length - fs.Position);
						fs.Close();
					}
					return;
				}

				DcmCodecParameters codecParams = null;
				if (tx == client.PreferredTransferSyntax)
					codecParams = client.PreferredTransferSyntaxParams;

				DicomFileFormat ff = new DicomFileFormat();
				ff.Load(FileName, DicomReadOptions.DefaultWithoutDeferredLoading);

				if (_originalTransferSyntax != tx) {
					if (_originalTransferSyntax.IsEncapsulated) {
						// Dataset is compressed... decompress
						try {
							ff.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian, null);
						}
						catch {
							client.Log.Error("{0} -> Unable to change transfer syntax:\n\tclass: {1}\n\told: {2}\n\tnew: {3}\n\treason: {4}\n\tcodecs: {5} - {6}",
								client.LogID, SOPClassUID.Description, _originalTransferSyntax, DicomTransferSyntax.ExplicitVRLittleEndian,
#if DEBUG
								HasError ? "Unknown" : Error.ToString(),
#else
								HasError ? "Unknown" : Error.Message,
#endif
								DicomCodec.HasCodec(_originalTransferSyntax), DicomCodec.HasCodec(DicomTransferSyntax.ExplicitVRLittleEndian));
							throw;
						}
					}

					if (tx.IsEncapsulated) {
						// Dataset needs to be compressed
						try {
							ff.ChangeTransferSyntax(tx, codecParams);
						}
						catch {
							client.Log.Error("{0} -> Unable to change transfer syntax:\n\tclass: {1}\n\told: {2}\n\tnew: {3}\n\treason: {4}\n\tcodecs: {5} - {6}",
								client.LogID, SOPClassUID.Description, ff.Dataset.InternalTransferSyntax, tx,
#if DEBUG
								HasError ? "Unknown" : Error.ToString(),
#else
								HasError ? "Unknown" : Error.Message,
#endif
								DicomCodec.HasCodec(ff.Dataset.InternalTransferSyntax), DicomCodec.HasCodec(tx));
							throw;
						}
					}
				}

				_dataset = ff.Dataset;
				_datasetSize = _dataset.CalculateWriteLength(tx, DicomWriteOptions.Default);
				_transferSyntax = tx;
			}
			catch (Exception e) {
				_dataset = null;
				_transferSyntax = _originalTransferSyntax;
				_status = DcmStatus.ProcessingFailure;
				_exception = e;
			}
			finally {
				_loaded = true;
			}
		}

		/// <summary>
		/// Unloads dataset from memory. (Internal)
		/// </summary>
		public void Unload() {
			_dataset = null;
			_transferSyntax = _originalTransferSyntax;
			_loaded = false;
		}

		/// <summary>
		/// Unloads the dataset and clears any errors. (Internal)
		/// </summary>
		public void Reset() {
			Unload();
			_status = DcmStatus.Pending;
			_exception = null;
		}

		internal CStoreRequestResult Send(CStoreClient client) {
			Load(client);

			if (HasError) {
				if (client.Associate.FindAbstractSyntax(SOPClassUID) == 0) {
					client.Reassociate();
					return CStoreRequestResult.Reassociate;
				}
			}

			if (client.OnCStoreRequestBegin != null)
				client.OnCStoreRequestBegin(client, this);

			if (HasError) {
				Status = DcmStatus.UnrecognizedOperation;
				if (client.OnCStoreRequestFailed != null)
					client.OnCStoreRequestFailed(client, this);
				return CStoreRequestResult.Failure;
			}

			byte pcid = client.Associate.FindAcceptedAbstractSyntaxWithTransferSyntax(SOPClassUID, TransferSyntax);

			if (pcid == 0) {
				client.Log.Info("{0} -> C-Store request failed: No accepted presentation context for {1}", client.LogID, SOPClassUID.Description);
				Status = DcmStatus.SOPClassNotSupported;
				if (client.OnCStoreRequestFailed != null)
					client.OnCStoreRequestFailed(client, this);
				return CStoreRequestResult.Failure;
			}

			if (_dataset != null) {
				client.SendCStoreRequest(pcid, SOPInstanceUID, _dataset);
			}
			else {
				using (Stream s = DicomFileFormat.GetDatasetStream(FileName)) {
					client.SendCStoreRequest(pcid, SOPInstanceUID, s);
					s.Close();
				}
			}

			if (client.OnCStoreRequestComplete != null)
				client.OnCStoreRequestComplete(client, this);

			return CStoreRequestResult.Success;
		}
		#endregion
	}

	public delegate void CStoreClientCallback(CStoreClient client);
	public delegate void CStoreRequestCallback(CStoreClient client, CStoreRequestInfo info);
	public delegate void CStoreRequestProgressCallback(CStoreClient client, CStoreRequestInfo info, DcmDimseProgress progress);

	/// <summary>
	/// DICOM C-Store SCU
	/// </summary>
	public class CStoreClient : DcmClientBase {
		#region Private Members
		private int _preloadCount = 1;
		private PreloadQueue<CStoreRequestInfo, CStoreClient> _sendQueue;
		private CStoreRequestInfo _current;
		private DicomTransferSyntax _preferredTransferSyntax;
		private DcmCodecParameters _preferedSyntaxParams;
		private bool _disableFileStream = false;
		private bool _serialPresContexts = false;
		private int _linger = 0;
		private Dictionary<DicomUID, List<DicomTransferSyntax>> _presContextMap = new Dictionary<DicomUID, List<DicomTransferSyntax>>();
		private object _lock = new object();
		private bool _cancel = false;
		private bool _offerExplicit = false;
		#endregion

		#region Public Constructors
		public CStoreClient() : base() {
			CallingAE = "STORE_SCU";
			CalledAE = "STORE_SCP";
			_sendQueue = new PreloadQueue<CStoreRequestInfo, CStoreClient>(this);
			_current = null;
		}
		#endregion

		#region Public Properties
		public CStoreRequestCallback OnCStoreRequestBegin;
		public CStoreRequestCallback OnCStoreRequestFailed;
		public CStoreRequestProgressCallback OnCStoreRequestProgress;
		public CStoreRequestCallback OnCStoreRequestComplete;
		public CStoreRequestCallback OnCStoreResponseReceived;

		public CStoreClientCallback OnCStoreConnected;
		public CStoreClientCallback OnCStoreComplete;
		public CStoreClientCallback OnCStoreClosed;

		/// <summary>
		/// First transfer syntax proposed in association.  Used if accepted.
		/// </summary>
		public DicomTransferSyntax PreferredTransferSyntax {
			get { return _preferredTransferSyntax; }
			set { _preferredTransferSyntax = value; }
		}

		/// <summary>
		/// Codec parameters for the preferred transfer syntax.
		/// </summary>
		public DcmCodecParameters PreferredTransferSyntaxParams {
			get { return _preferedSyntaxParams; }
			set { _preferedSyntaxParams = value; }
		}

		/// <summary>
		/// Create a unique presentation context for each combination of abstract and transfer syntaxes.
		/// </summary>
		public bool SerializedPresentationContexts {
			get { return _serialPresContexts; }
			set { _serialPresContexts = value; }
		}

		/// <summary>
		/// Set to true to force DICOM datasets to be loaded into memory.
		/// </summary>
		public bool DisableFileStreaming {
			get { return _disableFileStream; }
			set { _disableFileStream = value; }
		}

		/// <summary>
		/// Propose Explicit VR Little Endian for all presentation contexts
		/// </summary>
		public bool OfferExplicitSyntax {
			get { return _offerExplicit; }
			set { _offerExplicit = value; }
		}

		/// <summary>
		/// Number of requests to keep preloaded in memory.
		/// </summary>
		public int PreloadCount {
			get { return _preloadCount; }
			set { _preloadCount = value; }
		}

		/// <summary>
		/// Time to keep association alive after sending last image in queue.
		/// </summary>
		public int Linger {
			get { return _linger; }
			set { _linger = value; }
		}

		/// <summary>
		/// Number of pending DICOM files to be sent.
		/// </summary>
		public int PendingCount {
			get {
				if (_current != null)
					return _sendQueue.Count + 1;
				return _sendQueue.Count;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Enqueues a file to be transfered to the remote DICOM node.
		/// </summary>
		/// <param name="fileName">File containing DICOM dataset.</param>
		/// <returns>C-Store Request Information</returns>
		public CStoreRequestInfo AddFile(string fileName) {
			return AddFile(fileName, null);
		}

		/// <summary>
		/// Enqueues a file to be transfered to the remote DICOM node.
		/// </summary>
		/// <param name="fileName">File containing DICOM dataset.</param>
		/// <param name="userModel">
		/// User class containing one or more properties marked with a <see cref="Dicom.Data.DicomFieldAttribute"/>.
		/// This object will be stored in the UserState property of the CStoreRequestInfo object.
		/// </param>
		/// <returns>C-Store Request Information</returns>
		public CStoreRequestInfo AddFile(string fileName, object userModel) {
			CStoreRequestInfo info = new CStoreRequestInfo(fileName, userModel);
			AddFile(info);
			return info;
		}

		/// <summary>
		/// Enqueues a file to be transfered to the remote DICOM node.
		/// </summary>
		/// <param name="info">C-Store Request Information</param>
		public void AddFile(CStoreRequestInfo info) {
			if (info.HasError)
				return;

			lock (_lock) {
				_sendQueue.Enqueue(info);
				if (!_presContextMap.ContainsKey(info.SOPClassUID)) {
					_presContextMap.Add(info.SOPClassUID, new List<DicomTransferSyntax>());
				}
				if (!_presContextMap[info.SOPClassUID].Contains(info.TransferSyntax)) {
					_presContextMap[info.SOPClassUID].Add(info.TransferSyntax);
				}
			}

			if (Linger > 0 && IsClosed && CanReconnect && !ClosedOnError)
				Reconnect();
		}

		/// <summary>
		/// Cancels all pending C-Store requests and releases the association.
		/// 
		/// Call Reconnect() to resume transfer.
		/// </summary>
		/// <param name="wait">
		/// If true the command will wait for the current C-Store operation to complete 
		/// and the association to be released.
		/// </param>
		public void Cancel(bool wait) {
			_cancel = true;
			if (!IsClosed) {
				if (wait)
					Wait();
				else
					Close();
			}
		}
		#endregion

		#region Protected Methods
		protected override void OnConnected() {
			if (OnCStoreConnected != null) {
				try {
					OnCStoreConnected(this);
				}
				catch (Exception e) {
					Log.Error("Unhandled exception in user C-Store Connected Callback: {0}", e.Message);
				}
			}

			if (PendingCount > 0) {
				DcmAssociate associate = new DcmAssociate();

				lock (_lock) {
					foreach (DicomUID uid in _presContextMap.Keys) {
						if (_preferredTransferSyntax != null) {
							if (!_presContextMap[uid].Contains(_preferredTransferSyntax))
								_presContextMap[uid].Remove(_preferredTransferSyntax);
							_presContextMap[uid].Insert(0, _preferredTransferSyntax);
						}
						if (_offerExplicit && !_presContextMap[uid].Contains(DicomTransferSyntax.ExplicitVRLittleEndian))
							_presContextMap[uid].Add(DicomTransferSyntax.ExplicitVRLittleEndian);
						if (!_presContextMap[uid].Contains(DicomTransferSyntax.ImplicitVRLittleEndian))
							_presContextMap[uid].Add(DicomTransferSyntax.ImplicitVRLittleEndian);

						if (!DicomUID.IsImageStorage(uid)) {
							List<DicomTransferSyntax> remove = new List<DicomTransferSyntax>();
							foreach (DicomTransferSyntax tx in _presContextMap[uid]) {
								if (DicomTransferSyntax.IsImageCompression(tx))
									remove.Add(tx);
							}
							foreach (DicomTransferSyntax tx in remove) {
								_presContextMap[uid].Remove(tx);
							}
						}
					}

					if (SerializedPresentationContexts) {
						foreach (DicomUID uid in _presContextMap.Keys) {
							foreach (DicomTransferSyntax ts in _presContextMap[uid]) {
								byte pcid = associate.AddPresentationContext(uid);
								associate.AddTransferSyntax(pcid, ts);
							}
						}
					}
					else {
						foreach (DicomUID uid in _presContextMap.Keys) {
							byte pcid = associate.AddOrGetPresentationContext(uid);
							foreach (DicomTransferSyntax ts in _presContextMap[uid]) {
								associate.AddTransferSyntax(pcid, ts);
							}
						}
					}
				}

				associate.CalledAE = CalledAE;
				associate.CallingAE = CallingAE;
				associate.MaximumPduLength = MaxPduSize;

				SendAssociateRequest(associate);
			}
			else {
				Close();
			}
		}

		protected override void OnConnectionClosed() {
			if (_current != null) {
				_current.Reset();
				AddFile(_current);
				_current = null;
			}

			if (!ClosedOnError && !_cancel) {
				if (PendingCount > 0) {
					Reconnect();
					return;
				}

				if (OnCStoreComplete != null) {
					try {
						OnCStoreComplete(this);
					}
					catch (Exception e) {
						Log.Error("Unhandled exception in user C-Store Complete Callback: {0}", e.Message);
					}
				}
			}

			if (OnCStoreClosed != null) {
				try {
					OnCStoreClosed(this);
				}
				catch (Exception e) {
					Log.Error("Unhandled exception in user C-Store Closed Callback: {0}", e.Message);
				}
			}
		}

		protected override void OnReceiveAssociateAccept(DcmAssociate association) {
			SendNextCStoreRequest();
		}

		protected override void OnReceiveReleaseResponse() {
			InternalClose(PendingCount == 0);
		}

		protected override void OnReceiveCStoreResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedInstance, DcmStatus status) {
			_current.Status = status;
			if (OnCStoreResponseReceived != null) {
				try {
					OnCStoreResponseReceived(this, _current);
				}
				catch (Exception e) {
					Log.Error("Unhandled exception in user C-Store Response Callback: {0}", e.ToString());
				}
			}
			_current.Unload();
			_current = null;
			SendNextCStoreRequest();
		}

		protected override void OnSendDimseProgress(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
			if (OnCStoreRequestProgress != null && _current != null) {
				try {
					OnCStoreRequestProgress(this, _current, progress);
				}
				catch (Exception e) {
					Log.Error("Unhandled exception in user C-Store Progress Callback: {0}", e.Message);
				}
			}
		}

		private void SendNextCStoreRequest() {
			DateTime linger = DateTime.Now.AddSeconds(Linger + 1);
			while (linger > DateTime.Now && !_cancel) {
				while (_sendQueue.Count > 0 && !_cancel) {
					_current = _sendQueue.Dequeue();
					_sendQueue.Preload(_preloadCount);

					CStoreRequestResult result = _current.Send(this);

					if (result == CStoreRequestResult.Success ||
						result == CStoreRequestResult.Reassociate)
						return;

					linger = DateTime.Now.AddSeconds(Linger + 1);
				}
				Thread.Sleep(100);
			}
			SendReleaseRequest();
		}
		#endregion

		#region Internal Methods
		internal void Reassociate() {
			SendReleaseRequest();
		}

		internal void SendCStoreRequest(byte pcid, DicomUID instUid, Stream stream) {
			SendCStoreRequest(pcid, NextMessageID(), instUid, Priority, stream);
		}

		internal void SendCStoreRequest(byte pcid, DicomUID instUid, DcmDataset dataset) {
			SendCStoreRequest(pcid, NextMessageID(), instUid, Priority, dataset);
		}
		#endregion
	}
}
