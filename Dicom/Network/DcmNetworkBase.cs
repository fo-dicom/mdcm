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
using System.IO;
using System.Net.Sockets;
using System.Threading;

using Dicom.Data;
using Dicom.IO;
using Dicom.Utility;

using NLog;

namespace Dicom.Network {
	public class DcmDimseProgress {
		#region Private Members
		private int _bytesTransfered;
		private int _estimatedCommandLength;
		private int _estimatedDatasetLength;
		#endregion

		#region Public Properties
		public readonly DateTime Started = DateTime.Now;

		public int BytesTransfered {
			get { return _bytesTransfered; }
			internal set { _bytesTransfered = value; }
		}

		public int EstimatedCommandLength {
			get { return _estimatedCommandLength; }
			internal set { _estimatedCommandLength = value; }
		}

		public int EstimatedDatasetLength {
			get { return _estimatedDatasetLength; }
			internal set { _estimatedDatasetLength = value; }
		}

		public int EstimatedBytesTotal {
			get { return EstimatedCommandLength + EstimatedDatasetLength; }
		}

		public TimeSpan TimeElapsed {
			get { return DateTime.Now.Subtract(Started); }
		}
		#endregion
	}

	internal class DcmDimseInfo : IDisposable {
		#region Members
		public DcmCommand Command;
		public DcmDataset Dataset;
		public ChunkStream CommandData;
		public ChunkStream DatasetData;
		public DicomStreamReader CommandReader;
		public DicomStreamReader DatasetReader;
		public DcmDimseProgress Progress;
		public string DatasetFile;
		public FileStream DatasetFileStream;
		public Stream DatasetStream;
		public bool IsNewDimse;
		#endregion

		#region Methods
		public DcmDimseInfo() {
			Progress = new DcmDimseProgress();
			IsNewDimse = true;
		}

		public void Close() {
			CloseCommand();
			CloseDataset();
		}

		public void CloseCommand() {
			CommandData = null;
			CommandReader = null;
		}

		public void CloseDataset() {
			DatasetStream = null;
			DatasetData = null;
			DatasetReader = null;
			if (DatasetFileStream != null) {
				DatasetFileStream.Dispose();
				DatasetFileStream = null;
			}
		}

		public void Abort() {
			Close();
			if (DatasetFile != null)
				if (File.Exists(DatasetFile)) {
					try {
						File.Delete(DatasetFile);
					} catch {
					}
				}
		}

		public void Dispose() {
			Abort();
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public abstract class DcmNetworkBase {
		#region Private Members
		private ushort _messageId;
		private string _host;
		private int _port;
		private DcmSocketType _socketType;
		private int _throttle;
		private Stream _network;
		private DcmSocket _socket;
		private DcmAssociate _assoc;
		private DcmDimseInfo _dimse;
		private Thread _thread;
		private bool _stop;
		private int _dimseTimeout;
		private int _connectTimeout;
		private int _socketTimeout;
		private bool _disableTimeout;
		private bool _isRunning;
		private bool _useFileBuffer;
		private bool _enableStreamParse;
		private string _logid = "SCU";
		private Logger _log;
		#endregion

		#region Public Constructors
		public DcmNetworkBase() {
			_messageId = 1;
			_connectTimeout = 10;
			_socketTimeout = 30;
			_dimseTimeout = 180;
			_throttle = 0;
			_isRunning = false;
			_useFileBuffer = true;
			_enableStreamParse = false;
			_log = Dicom.Debug.Log;
			_logid = "SCx";
		}
		#endregion

		#region Public Properties
		public DcmAssociate Associate {
			get { return _assoc; }
		}

		public int DimseTimeout {
			get { return _dimseTimeout; }
			set { _dimseTimeout = value; }
		}

		public int ConnectTimeout {
			get { return _connectTimeout; }
			set { _connectTimeout = value; }
		}

		public int SocketTimeout {
			get { return _socketTimeout; }
			set {
				_socketTimeout = value;
				if (Socket != null) {
					try {
						Socket.SendTimeout = _socketTimeout * 1000;
						Socket.ReceiveTimeout = _socketTimeout * 1000;
					}
					catch {
					}
				}
			}
		}

		public string Host {
			get { return _host; }
		}

		public int Port {
			get { return _port; }
		}

		public DcmSocketType SocketType {
			get { return _socketType; }
		}

		public bool CanReconnect {
			get { return !String.IsNullOrEmpty(_host) && _port != 0; }
		}

		public int ThrottleSpeed {
			get { return _throttle; }
			set {
				_throttle = value;
				if (Socket != null) {
					try {
						Socket.ThrottleSpeed = _throttle;
					}
					catch {
					}
				}
			}
		}

		public DcmSocket Socket {
			get { return _socket; }
		}

		protected Stream InternalStream {
			get { return _network; }
		}

		public bool IsClosed {
			get { return !_isRunning; }
		}

		public bool UseFileBuffer {
			get { return _useFileBuffer; }
			set { _useFileBuffer = value; }
		}

		public bool EnableStreamParse {
			get { return _enableStreamParse; }
			set { _enableStreamParse = value; }
		}

		public string LogID {
			get { return _logid; }
			set { _logid = value; }
		}

		public Logger Log {
			get { return _log; }
			set { _log = value; }
		}
		#endregion

		#region Public Methods
		public void Connect(string host, int port, DcmSocketType type) {
			_host = host;
			_port = port;
			_socketType = type;
			_stop = false;
			_isRunning = true;
			_thread = new Thread(Connect);
			_thread.IsBackground = true;
			_thread.Start();
		}
		#endregion

		#region Protected Methods
		protected void InitializeNetwork(DcmSocket socket) {
			_socket = socket;
			_socket.SendTimeout = _socketTimeout * 1000;
			_socket.ReceiveTimeout = _socketTimeout * 1000;
			_socket.ThrottleSpeed = _throttle;

			OnInitializeNetwork();

			_network = _socket.GetStream();
			_stop = false;
			_isRunning = true;
			_thread = new Thread(Process);
			_thread.IsBackground = true;
			_thread.Start();
		}

		protected void Reconnect() {
			DcmSocketType type = Socket.Type;
			ShutdownNetwork();
			Connect(Host, Port, type);
		}

		protected void ShutdownNetwork() {
			_stop = true;
			if (_thread != null) {
				if (Thread.CurrentThread.ManagedThreadId != _thread.ManagedThreadId)
					_thread.Join();
				_thread = null;	
			}
		}

		protected virtual void OnInitializeNetwork() {
		}

		protected virtual void OnConnected() {
		}

		protected virtual void OnConnectionClosed() {
		}

		protected virtual void OnNetworkError(Exception e) {
		}

		protected virtual void OnDimseTimeout() {
		}

		protected virtual void OnReceiveAbort(DcmAbortSource source, DcmAbortReason reason) {
			throw new NotImplementedException();
		}

		protected virtual void OnReceiveAssociateRequest(DcmAssociate association) {
			throw new NotImplementedException();
		}

		protected virtual void OnReceiveAssociateAccept(DcmAssociate association) {
			throw new NotImplementedException();
		}

		protected virtual void OnReceiveAssociateReject(DcmRejectResult result, DcmRejectSource source, DcmRejectReason reason) {
			throw new NotImplementedException();
		}

		protected virtual void OnReceiveReleaseRequest() {
			throw new NotImplementedException();
		}

		protected virtual void OnReceiveReleaseResponse() {
			throw new NotImplementedException();
		}



		protected virtual void OnReceiveDimseBegin(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
		}

		protected virtual void OnReceiveDimseProgress(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
		}

		protected virtual void OnReceiveDimse(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
		}

		protected virtual void OnSendDimseBegin(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
		}

		protected virtual void OnSendDimseProgress(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
		}

		protected virtual void OnSendDimse(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
		}



		protected virtual void OnPreReceiveCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance,
			DcmPriority priority, string moveAE, ushort moveMessageID, out string fileName) {
			if (UseFileBuffer) {
				fileName = Path.GetTempFileName();
			} else {
				fileName = null;
			}
		}

		protected virtual void OnReceiveCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance, 
			DcmPriority priority, string moveAE, ushort moveMessageID, DcmDataset dataset, string fileName) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnPostReceiveCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance, 
			DcmDataset dataset, string fileName) {
			if (!String.IsNullOrEmpty(fileName)) {
				if (File.Exists(fileName)) {
					try {
						File.Delete(fileName);
					} catch {
					}
				}
			}
		}

		protected virtual void OnReceiveCStoreResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedInstance, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCEchoRequest(byte presentationID, ushort messageID, DcmPriority priority) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCEchoResponse(byte presentationID, ushort messageIdRespondedTo, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCFindRequest(byte presentationID, ushort messageID, DcmPriority priority, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCFindResponse(byte presentationID, ushort messageIdRespondedTo, DcmDataset dataset, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCGetRequest(byte presentationID, ushort messageID, DcmPriority priority, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCGetResponse(byte presentationID, ushort messageIdRespondedTo, DcmDataset dataset, 
			DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCMoveRequest(byte presentationID, ushort messageID, string destinationAE, DcmPriority priority, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCMoveResponse(byte presentationID, ushort messageIdRespondedTo, DcmDataset dataset, 
			DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveCCancelRequest(byte presentationID, ushort messageIdRespondedTo) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNEventReportRequest(byte presentationID, ushort messageID, DicomUID affectedClass, DicomUID affectedInstance,
			ushort eventTypeID, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNEventReportResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			ushort eventTypeID, DcmDataset dataset, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNGetRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance, DicomTag[] attributes) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNGetResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			DcmDataset dataset, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNSetRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNSetResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			DcmDataset dataset, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNActionRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance,
			ushort actionTypeID, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNActionResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			ushort actionTypeID, DcmDataset dataset, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNCreateRequest(byte presentationID, ushort messageID, DicomUID affectedClass, DicomUID affectedInstance, DcmDataset dataset) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNCreateResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			DcmDataset dataset, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNDeleteRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}

		protected virtual void OnReceiveNDeleteResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance, DcmStatus status) {
			SendAbort(DcmAbortSource.ServiceProvider, DcmAbortReason.NotSpecified);
		}


		protected ushort NextMessageID() {
			return _messageId++;
		}

		/// <summary>
		/// A DICOM Application Entity (which includes the Upper Layer service-user) 
		/// that desires to establish an association shall issue an A-ASSOCIATE request 
		/// primitive. The called AE is identified by parameters of the request 
		/// primitive. The requestor shall not issue any primitives except an A-ABORT 
		/// request primitive until it receives an A-ASSOCIATE confirmation primitive.
		/// </summary>
		/// <param name="associate"></param>
		protected void SendAssociateRequest(DcmAssociate associate) {
			_assoc = associate;
			Log.Info("{0} -> Association request:\n{1}", LogID, Associate.ToString());
			AAssociateRQ pdu = new AAssociateRQ(_assoc);
			SendRawPDU(pdu.Write());
		}

		/// <summary>
		/// The called AE shall accept or reject the association by sending an A-ASSOCIATE 
		/// response primitive with an appropriate Result parameter. The Upper layer 
		/// service-provider shall issue an A-ASSOCIATE confirmation primitive having the 
		/// same Result parameter. The Result Source parameter shall be assigned the 
		/// symbolic value of “UL service-user.”
		/// </summary>
		/// <param name="associate"></param>
		protected void SendAssociateAccept(DcmAssociate associate) {
			Log.Info("{0} -> Association accept:\n{1}", LogID, Associate.ToString());
			AAssociateAC pdu = new AAssociateAC(_assoc);
			SendRawPDU(pdu.Write());
		}

		/// <summary>
		/// The UL service-provider may not be capable of supporting the requested 
		/// association. In this situation, it shall return an A-ASSOCIATE confirmation 
		/// primitive to the requestor with an appropriate Result parameter (rejected). The 
		/// Result Source parameter shall be appropriately assigned either the symbolic value 
		/// of “UL service-provider (ACSE related function)” or “UL service-provider 
		/// (Presentation related function).” The indication primitive shall not be issued. 
		/// The association shall not be established.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="source"></param>
		/// <param name="reason"></param>
		protected void SendAssociateReject(DcmRejectResult result, DcmRejectSource source, DcmRejectReason reason) {
			Log.Info("{0} -> Association reject [result: {1}; source: {2}; reason: {3}]", LogID, result, source, reason);
			AAssociateRJ pdu = new AAssociateRJ(result, source, reason);
			SendRawPDU(pdu.Write());
		}

		/// <summary>
		/// The graceful release of an association between two AEs shall be performed 
		/// through ACSE A-RELEASE request, indication, response, and confirmation 
		/// primitives. The initiator of the service is hereafter called a requestor 
		/// and the service-user which receives the A-RELEASE indication is hereafter 
		/// called the acceptor. It shall be a confirmed service.
		/// </summary>
		protected void SendReleaseRequest() {
			Log.Info("{0} -> Association release request", LogID);
			AReleaseRQ pdu = new AReleaseRQ();
			SendRawPDU(pdu.Write());
		}

		/// <summary>
		/// The graceful release of an association between two AEs shall be performed 
		/// through ACSE A-RELEASE request, indication, response, and confirmation 
		/// primitives. The initiator of the service is hereafter called a requestor 
		/// and the service-user which receives the A-RELEASE indication is hereafter 
		/// called the acceptor. It shall be a confirmed service.
		/// </summary>
		protected void SendReleaseResponse() {
			Log.Info("{0} -> Association release response", LogID);
			AReleaseRP pdu = new AReleaseRP();
			SendRawPDU(pdu.Write());
		}

		/// <summary>
		/// The ACSE A-ABORT service shall be used by a requestor in either of the AEs 
		/// to cause the abnormal release of the association. It shall be a non-confirmed 
		/// service. However, because of the possibility of an A-ABORT service procedure 
		/// collision, the delivery of the indication primitive is not guaranteed. Should
		/// such a collision occur, both AEs are aware that the association has been 
		/// terminated. The abort shall be performed through A-ABORT request and A-ABORT 
		/// indication primitives.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="reason"></param>
		protected void SendAbort(DcmAbortSource source, DcmAbortReason reason) {
			Log.Info("{0} -> Abort [source: {1}; reason: {2}]", LogID, source, reason);
			AAbort pdu = new AAbort(source, reason);
			SendRawPDU(pdu.Write());
		}

		protected void SendCEchoRequest(byte presentationID, ushort messageID, DcmPriority priority) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateRequest(messageID, DcmCommandField.CEchoRequest, affectedClass, priority, false);
			Log.Info("{0} -> C-Echo request [pc: {1}; id: {2}]", LogID, presentationID, messageID);
			SendDimse(presentationID, command, null);
		}

		protected void SendCEchoResponse(byte presentationID, ushort messageIdRespondedTo, DcmStatus status) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateResponse(messageIdRespondedTo, DcmCommandField.CEchoResponse, affectedClass, status, false);
			Log.Info("{0} -> C-Echo response [id: {1}]", LogID, messageIdRespondedTo);
			SendDimse(presentationID, command, null);
		}

		/// <summary>
		/// The C-STORE service is used by a DIMSE-service-user to store a composite 
		/// SOP Instance on a peer DIMSE-service-user. It is a confirmed service.
		/// </summary>
		/// <param name="presentationID">The Presentation Context ID identifies the
		/// Presentation Context within the scope of a specific Association.</param>
		/// <param name="messageID">This parameter identifies the operation. It is used 
		/// to distinguish this operation from other notifications or operations that 
		/// the DIMSE-service-provider may have in progress. No two identical values 
		/// for the Message ID (0000,0110) shall be used for outstanding operations or 
		/// notifications.</param>
		/// <param name="affectedInstance">For the request/indication, this parameter 
		/// specifies the SOP Instance to be stored. It may be included in the 
		/// response/confirmation. If included in the response/confirmation, this 
		/// parameter shall be equal to the value in the request/indication.</param>
		/// <param name="priority">This parameter specifies the priority of the 
		/// C-STORE operation. It shall be one of LOW, MEDIUM, or HIGH.</param>
		/// <param name="dataset">The Data Set accompanying the C-STORE primitive 
		/// contains the Attributes of the Composite SOP Instance to be stored.</param>
		protected void SendCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance,
			DcmPriority priority, DcmDataset dataset) {
			SendCStoreRequest(presentationID, messageID, affectedInstance, priority, null, 0, dataset);
		}

		/// <summary>
		/// The C-STORE service is used by a DIMSE-service-user to store a composite 
		/// SOP Instance on a peer DIMSE-service-user. It is a confirmed service.
		/// </summary>
		/// <param name="presentationID">The Presentation Context ID identifies the
		/// Presentation Context within the scope of a specific Association.</param>
		/// <param name="messageID">This parameter identifies the operation. It is used 
		/// to distinguish this operation from other notifications or operations that 
		/// the DIMSE-service-provider may have in progress. No two identical values 
		/// for the Message ID (0000,0110) shall be used for outstanding operations or 
		/// notifications.</param>
		/// <param name="affectedInstance">For the request/indication, this parameter 
		/// specifies the SOP Instance to be stored. It may be included in the 
		/// response/confirmation. If included in the response/confirmation, this 
		/// parameter shall be equal to the value in the request/indication.</param>
		/// <param name="priority">This parameter specifies the priority of the 
		/// C-STORE operation. It shall be one of LOW, MEDIUM, or HIGH.</param>
		/// <param name="moveAE">This parameter specifies the DICOM AE Title of the 
		/// DICOM AE which invoked the C-MOVE operation from which this C-STORE 
		/// sub-operation is being performed.</param>
		/// <param name="moveMessageID">This parameter specifies the Message ID (0000,0110) 
		/// of the C-MOVE request/indication primitive from which this C-STORE 
		/// sub-operation is being performed.</param>
		/// <param name="dataset">The Data Set accompanying the C-STORE primitive 
		/// contains the Attributes of the Composite SOP Instance to be stored.</param>
		protected void SendCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance, 
			DcmPriority priority, string moveAE, ushort moveMessageID, DcmDataset dataset) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);

			DcmCommand command = CreateRequest(messageID, DcmCommandField.CStoreRequest, affectedClass, priority, true);
			command.AffectedSOPInstanceUID = affectedInstance;
			if (moveAE != null && moveAE != String.Empty) {
				command.MoveOriginatorAE = moveAE;
				command.MoveOriginatorMessageID = moveMessageID;
			}

			Log.Info("{0} -> C-Store request [pc: {1}; id: {2}]\n\t=> {3}\n\t=> {4}", LogID, presentationID, messageID, affectedInstance, affectedClass);
			SendDimse(presentationID, command, dataset);
		}

		/// <summary>
		/// The C-STORE service is used by a DIMSE-service-user to store a composite 
		/// SOP Instance on a peer DIMSE-service-user. It is a confirmed service.
		/// </summary>
		/// <param name="presentationID">The Presentation Context ID identifies the
		/// Presentation Context within the scope of a specific Association.</param>
		/// <param name="messageID">This parameter identifies the operation. It is used 
		/// to distinguish this operation from other notifications or operations that 
		/// the DIMSE-service-provider may have in progress. No two identical values 
		/// for the Message ID (0000,0110) shall be used for outstanding operations or 
		/// notifications.</param>
		/// <param name="affectedInstance">For the request/indication, this parameter 
		/// specifies the SOP Instance to be stored. It may be included in the 
		/// response/confirmation. If included in the response/confirmation, this 
		/// parameter shall be equal to the value in the request/indication.</param>
		/// <param name="priority">This parameter specifies the priority of the 
		/// C-STORE operation. It shall be one of LOW, MEDIUM, or HIGH.</param>
		/// <param name="datastream">The Data Set accompanying the C-STORE primitive 
		/// contains the Attributes of the Composite SOP Instance to be stored.</param>
		protected void SendCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance,
			DcmPriority priority, Stream datastream) {
			SendCStoreRequest(presentationID, messageID, affectedInstance, priority, null, 0, datastream);
		}

		/// <summary>
		/// The C-STORE service is used by a DIMSE-service-user to store a composite 
		/// SOP Instance on a peer DIMSE-service-user. It is a confirmed service.
		/// </summary>
		/// <param name="presentationID">The Presentation Context ID identifies the
		/// Presentation Context within the scope of a specific Association.</param>
		/// <param name="messageID">This parameter identifies the operation. It is used 
		/// to distinguish this operation from other notifications or operations that 
		/// the DIMSE-service-provider may have in progress. No two identical values 
		/// for the Message ID (0000,0110) shall be used for outstanding operations or 
		/// notifications.</param>
		/// <param name="affectedInstance">For the request/indication, this parameter 
		/// specifies the SOP Instance to be stored. It may be included in the 
		/// response/confirmation. If included in the response/confirmation, this 
		/// parameter shall be equal to the value in the request/indication.</param>
		/// <param name="priority">This parameter specifies the priority of the 
		/// C-STORE operation. It shall be one of LOW, MEDIUM, or HIGH.</param>
		/// <param name="moveAE">This parameter specifies the DICOM AE Title of the 
		/// DICOM AE which invoked the C-MOVE operation from which this C-STORE 
		/// sub-operation is being performed.</param>
		/// <param name="moveMessageID">This parameter specifies the Message ID (0000,0110) 
		/// of the C-MOVE request/indication primitive from which this C-STORE 
		/// sub-operation is being performed.</param>
		/// <param name="datastream">The Data Set accompanying the C-STORE primitive 
		/// contains the Attributes of the Composite SOP Instance to be stored.</param>
		protected void SendCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance,
			DcmPriority priority, string moveAE, ushort moveMessageID, Stream datastream) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);

			DcmCommand command = CreateRequest(messageID, DcmCommandField.CStoreRequest, affectedClass, priority, true);
			command.AffectedSOPInstanceUID = affectedInstance;
			if (moveAE != null && moveAE != String.Empty) {
				command.MoveOriginatorAE = moveAE;
				command.MoveOriginatorMessageID = moveMessageID;
			}

			Log.Info("{0} -> C-Store request [pc: {1}; id: {2}] (stream)\n\t=> {3}\n\t=> {4}", LogID, presentationID, messageID, affectedInstance, affectedClass);
			SendDimseStream(presentationID, command, datastream);
		}

		protected void SendCStoreResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedInstance, DcmStatus status) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateResponse(messageIdRespondedTo, DcmCommandField.CStoreResponse, affectedClass, status, false);
			command.AffectedSOPInstanceUID = affectedInstance;
			Log.Info("{0} -> C-Store response [id: {1}]: {2}", LogID, messageIdRespondedTo, status);
			SendDimse(presentationID, command, null);
		}

		protected void SendCFindRequest(byte presentationID, ushort messageID, DcmPriority priority, DcmDataset dataset) {
			String level = dataset.GetString(DicomTags.QueryRetrieveLevel, "UNKNOWN");
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateRequest(messageID, DcmCommandField.CFindRequest, affectedClass, priority, true);
			Log.Info("{0} -> C-Find request [pc: {1}; id: {2}; lvl: {3}]", LogID, presentationID, messageID, level);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendCFindResponse(byte presentationID, ushort messageIdRespondedTo, DcmStatus status) {
			SendCFindResponse(presentationID, messageIdRespondedTo, null, status);
		}

		protected void SendCFindResponse(byte presentationID, ushort messageIdRespondedTo, DcmDataset dataset, DcmStatus status) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateResponse(messageIdRespondedTo, DcmCommandField.CFindResponse, affectedClass, status, dataset != null);
			Log.Info("{0} -> C-Find response [id: {1}]: {2}", LogID, messageIdRespondedTo, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendCGetRequest(byte presentationID, ushort messageID, DcmPriority priority, DcmDataset dataset) {
			String level = dataset.GetString(DicomTags.QueryRetrieveLevel, "UNKNOWN");
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateRequest(messageID, DcmCommandField.CGetRequest, affectedClass, priority, true);
			Log.Info("{0} -> C-Get request [pc: {1}; id: {2}; lvl: {3}]", LogID, presentationID, messageID, level);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendCGetResponse(byte presentationID, ushort messageIdRespondedTo, DcmStatus status,
			ushort remain, ushort complete, ushort warning, ushort failure) {
			SendCGetResponse(presentationID, messageIdRespondedTo, null, status, remain, complete, warning, failure);
		}

		protected void SendCGetResponse(byte presentationID, ushort messageIdRespondedTo, DcmDataset dataset, 
			DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateResponse(messageIdRespondedTo, DcmCommandField.CGetResponse, affectedClass, status, dataset != null);
			command.NumberOfRemainingSuboperations = remain;
			command.NumberOfCompletedSuboperations = complete;
			command.NumberOfWarningSuboperations = warning;
			command.NumberOfFailedSuboperations = failure;
			Log.Info("{0} -> C-Get response [id: {1}; remain: {2}; complete: {3}; warning: {4}; failure: {5}]: {6}", 
				LogID, messageIdRespondedTo, remain, complete, warning, failure, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendCMoveRequest(byte presentationID, ushort messageID, string destinationAE, DcmPriority priority, DcmDataset dataset) {
			String level = dataset.GetString(DicomTags.QueryRetrieveLevel, "UNKNOWN");
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateRequest(messageID, DcmCommandField.CMoveRequest, affectedClass, priority, true);
			command.MoveDestinationAE = destinationAE;
			Log.Info("{0} -> C-Move request [pc: {1}; id: {2}; lvl: {3}; dest: {4}]", LogID, presentationID, messageID, level, destinationAE);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendCMoveResponse(byte presentationID, ushort messageIdRespondedTo, DcmStatus status,
			ushort remain, ushort complete, ushort warning, ushort failure) {
			SendCMoveResponse(presentationID, messageIdRespondedTo, null, status, remain, complete, warning, failure);
		}

		protected void SendCMoveResponse(byte presentationID, ushort messageIdRespondedTo, DcmDataset dataset, DcmStatus status,
			ushort remain, ushort complete, ushort warning, ushort failure) {
			DicomUID affectedClass = Associate.GetAbstractSyntax(presentationID);
			DcmCommand command = CreateResponse(messageIdRespondedTo, DcmCommandField.CMoveResponse, affectedClass, status, dataset != null);
			command.NumberOfRemainingSuboperations = remain;
			command.NumberOfCompletedSuboperations = complete;
			command.NumberOfWarningSuboperations = warning;
			command.NumberOfFailedSuboperations = failure;
			Log.Info("{0} -> C-Move response [id: {1}; remain: {2}; complete: {3}; warning: {4}; failure: {5}]: {6}",
				LogID, messageIdRespondedTo, remain, complete, warning, failure, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendCCancelRequest(byte presentationID, ushort messageIdRespondedTo) {
			DcmCommand command = new DcmCommand();
			command.CommandField = DcmCommandField.CCancelRequest;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = false;
			Log.Info("{0} -> C-Cancel request [pc: {1}; id: {2}]", LogID, presentationID, messageIdRespondedTo);
			SendDimse(presentationID, command, null);
		}

		protected void SendNEventReportRequest(byte presentationID, ushort messageID, DicomUID affectedClass, DicomUID affectedInstance, 
			ushort eventTypeID, DcmDataset dataset) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NEventReportRequest;
			command.MessageID = messageID;
			command.HasDataset = (dataset != null);
			command.AffectedSOPInstanceUID = affectedInstance;
			command.EventTypeID = eventTypeID;
			Log.Info("{0} -> N-EventReport request [pc: {1}; id: {2}; class: {3}; event: {4:x4}]", 
				LogID, presentationID, messageID, affectedClass.Description, eventTypeID);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNEventReportResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			ushort eventTypeID, DcmDataset dataset, DcmStatus status) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NEventReportResponse;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = (dataset != null);
			command.Status = status;
			command.AffectedSOPInstanceUID = affectedInstance;
			command.EventTypeID = eventTypeID;
			Log.Info("{0} -> N-EventReport response [id: {1}; class: {2}; event: {3:x4}]: {4}}",
				LogID, messageIdRespondedTo, affectedClass.Description, eventTypeID, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNGetRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance, DicomTag[] attributes) {
			DcmCommand command = new DcmCommand();
			command.RequestedSOPClassUID = requestedClass;
			command.CommandField = DcmCommandField.NGetRequest;
			command.MessageID = messageID;
			command.HasDataset = false;
			command.RequestedSOPInstanceUID = requestedInstance;
			command.AttributeIdentifierList = new DcmAttributeTag(DicomTags.AttributeIdentifierList);
			command.AttributeIdentifierList.SetValues(attributes);
			Log.Info("{0} -> N-Get request [pc: {1}; id: {2}; class: {3}]", LogID, presentationID, messageID, requestedClass.Description);
			SendDimse(presentationID, command, null);
		}

		protected void SendNGetResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance, 
			DcmDataset dataset, DcmStatus status) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NGetResponse;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = (dataset != null);
			command.Status = status;
			command.AffectedSOPInstanceUID = affectedInstance;
			Log.Info("{0} -> N-Get response [id: {1}; class: {2}]: {3}", LogID, messageIdRespondedTo, affectedClass.Description, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNSetRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance, DcmDataset dataset) {
			DcmCommand command = new DcmCommand();
			command.RequestedSOPClassUID = requestedClass;
			command.CommandField = DcmCommandField.NSetRequest;
			command.MessageID = messageID;
			command.HasDataset = (dataset != null);
			command.RequestedSOPInstanceUID = requestedInstance;
			Log.Info("{0} -> N-Set request [pc: {1}; id: {2}; class: {3}]", LogID, presentationID, messageID, requestedClass.Description);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNSetResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			DcmDataset dataset, DcmStatus status) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NSetResponse;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = (dataset != null);
			command.Status = status;
			command.AffectedSOPInstanceUID = affectedInstance;
			Log.Info("{0} -> N-Set response [id: {1}; class: {2}]: {3}", LogID, messageIdRespondedTo, affectedClass.Description, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNActionRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance, 
			ushort actionTypeID, DcmDataset dataset) {
			DcmCommand command = new DcmCommand();
			command.RequestedSOPClassUID = requestedClass;
			command.CommandField = DcmCommandField.NActionRequest;
			command.MessageID = messageID;
			command.HasDataset = (dataset != null);
			command.RequestedSOPInstanceUID = requestedInstance;
			command.ActionTypeID = actionTypeID;
			Log.Info("{0} -> N-Action request [pc: {1}; id: {2}; class: {3}; action: {4:x4}]", 
				LogID, presentationID, messageID, requestedClass, actionTypeID);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNActionResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance, 
			ushort actionTypeID, DcmDataset dataset, DcmStatus status) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NActionResponse;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = (dataset != null);
			command.Status = status;
			command.AffectedSOPInstanceUID = affectedInstance;
			command.ActionTypeID = actionTypeID;
			Log.Info("{0} -> N-Action response [id: {1}; class: {2}; action: {3:x4}]: {4}",
				LogID, messageIdRespondedTo, affectedClass.Description, actionTypeID, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNCreateRequest(byte presentationID, ushort messageID, DicomUID affectedClass, DicomUID affectedInstance, DcmDataset dataset) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NCreateRequest;
			command.MessageID = messageID;
			command.HasDataset = (dataset != null);
			command.AffectedSOPInstanceUID = affectedInstance;
			Log.Info("{0} -> N-Create request [pc: {1}; id: {2}; class: {3}]", LogID, presentationID, messageID, affectedClass.Description);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNCreateResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
			DcmDataset dataset, DcmStatus status) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NCreateResponse;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = (dataset != null);
			command.Status = status;
			command.AffectedSOPInstanceUID = affectedInstance;
			Log.Info("{0} -> N-Create response [id: {1}; class: {2}]: {3}", LogID, messageIdRespondedTo, affectedClass.Description, status);
			SendDimse(presentationID, command, dataset);
		}

		protected void SendNDeleteRequest(byte presentationID, ushort messageID, DicomUID requestedClass, DicomUID requestedInstance) {
			DcmCommand command = new DcmCommand();
			command.RequestedSOPClassUID = requestedClass;
			command.CommandField = DcmCommandField.NDeleteRequest;
			command.MessageID = messageID;
			command.HasDataset = false;
			command.RequestedSOPInstanceUID = requestedInstance;
			Log.Info("{0} -> N-Delete request [pc: {1}; id: {2}; class: {3}]", LogID, presentationID, messageID, requestedClass.Description);
			SendDimse(presentationID, command, null);
		}

		protected void SendNDeleteResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance, DcmStatus status) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = DcmCommandField.NDeleteResponse;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = false;
			command.Status = status;
			command.AffectedSOPInstanceUID = affectedInstance;
			Log.Info("{0} -> N-Delete response [id: {1}; class: {2}]: {3}", LogID, messageIdRespondedTo, affectedClass.Description, status);
			SendDimse(presentationID, command, null);
		}
		#endregion

		#region Private Methods
		private DcmCommand CreateRequest(ushort messageID, DcmCommandField commandField, DicomUID affectedClass, DcmPriority priority, bool hasDataset) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = commandField;
			command.MessageID = messageID;
			command.Priority = priority;
			command.HasDataset = hasDataset;
			return command;
		}

		private DcmCommand CreateResponse(ushort messageIdRespondedTo, DcmCommandField commandField, DicomUID affectedClass, DcmStatus status, bool hasDataset) {
			DcmCommand command = new DcmCommand();
			command.AffectedSOPClassUID = affectedClass;
			command.CommandField = commandField;
			command.MessageIDBeingRespondedTo = messageIdRespondedTo;
			command.HasDataset = hasDataset;
			command.Status = status;
			if (!String.IsNullOrEmpty(status.ErrorComment))
				command.ErrorComment = status.ErrorComment;
			return command;
		}

		private void Connect() {
			bool success = false;

			try {
				Log.Info("{0} -> Connecting to server at {1}:{2}", LogID, _host, _port);
				
				_socket = DcmSocket.Create(_socketType);
				_socket.ConnectTimeout = _connectTimeout * 1000;
				_socket.SendTimeout = _socketTimeout * 1000;
				_socket.ReceiveTimeout = _socketTimeout * 1000;
				_socket.ThrottleSpeed = _throttle;
				_socket.Connect(_host, _port);

				if (_socketType == DcmSocketType.TLS)
					Log.Info("{0} -> Authenticating SSL/TLS for server: {1}", LogID, _socket.RemoteEndPoint);

				OnInitializeNetwork();

				_network = _socket.GetStream();
				success = true;
			}
			catch (SocketException e) {
				if (e.SocketErrorCode == SocketError.TimedOut)
					Log.Error("{0} -> Connection timeout after {1} seconds", LogID, _connectTimeout);
				else
					Log.Error("{0} -> Network error: {1}", LogID, e.Message);
				OnNetworkError(e);
				OnConnectionClosed();
			}
			catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Processing failure: {1}", LogID, e.ToString());
#else
				Log.Error("{0} -> Processing failure: {1}", LogID, e.Message);
#endif
				OnNetworkError(e);
				OnConnectionClosed();
			}
			finally {
				if (!success) {
					if (_network != null) {
						try { _network.Close(); }
						catch { }
						_network = null;
					}
					if (_socket != null) {
						try { _socket.Close(); }
						catch { }
						_socket = null;
					}
					_isRunning = false;
				}
			}

			if (success)
				Process();
		}

		private void Process() {
			try {
				OnConnected();
				_disableTimeout = false;
				DateTime timeout = DateTime.Now.AddSeconds(DimseTimeout);
				while (!_stop) {
					if (_socket.Poll(1000000, SelectMode.SelectRead)) {
						if (_socket.Available == 0)
							break;
						ProcessNextPDU();
						timeout = DateTime.Now.AddSeconds(DimseTimeout);
					}
					else if (_disableTimeout) {
						timeout = DateTime.Now.AddSeconds(DimseTimeout);
					}
					else if (DimseTimeout != 0 && DateTime.Now > timeout) {
						Log.Error("{0} -> DIMSE timeout after {1} seconds", LogID, DimseTimeout);
						OnDimseTimeout();
						_stop = true;
					}
					else if (!_socket.Connected)
						break;
				}
				Log.Info("{0} -> Connection closed", LogID);
				OnConnectionClosed();
			}
			catch (SocketException e) {
				if (e.SocketErrorCode == SocketError.TimedOut)
					Log.Error("{0} -> Network timeout after {1} seconds", LogID, SocketTimeout);
				else
					Log.Error("{0} -> Network error: {1}", LogID, e.Message);
				OnNetworkError(e);
				OnConnectionClosed();
			}
			catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Processing failure: {1}", LogID, e.ToString());
#else
				Log.Error("{0} -> Processing failure: {1}", LogID, e.Message);
#endif
				OnNetworkError(e);
				Log.Info("{0} -> Connection closed", LogID);
				OnConnectionClosed();
			}
			finally {
				try { _network.Close(); } catch { }
				_network = null;
				try { _socket.Close(); } catch { }
				_socket = null;
				_isRunning = false;
				_dimse = null;
			}
		}

		private bool ProcessNextPDU() {
			RawPDU raw = new RawPDU(_network);

			if (raw.Type == 0x04) {
				if (_dimse == null) {
					_dimse = new DcmDimseInfo();
				}
			}

			try {
				raw.ReadPDU();

				switch (raw.Type) {
				case 0x01: {
						_assoc = new DcmAssociate();
						AAssociateRQ pdu = new AAssociateRQ(_assoc);
						pdu.Read(raw);
						Log.Info("{0} <- Association request:\n{1}", LogID, Associate.ToString());
						OnReceiveAssociateRequest(_assoc);
						return true;
					}
				case 0x02: {
						AAssociateAC pdu = new AAssociateAC(_assoc);
						pdu.Read(raw);
						Log.Info("{0} <- Association accept:\n{1}", LogID, Associate.ToString());
						OnReceiveAssociateAccept(_assoc);
						return true;
					}
				case 0x03: {
						AAssociateRJ pdu = new AAssociateRJ();
						pdu.Read(raw);
						Log.Info("{0} <- Association reject [result: {1}; source: {2}; reason: {3}]", LogID, pdu.Result, pdu.Source, pdu.Reason);
						OnReceiveAssociateReject(pdu.Result, pdu.Source, pdu.Reason);
						return true;
					}
				case 0x04: {
						PDataTF pdu = new PDataTF();
						pdu.Read(raw);
						//Log.Debug("{0} <- P-Data-TF", LogID);
						return ProcessPDataTF(pdu);
					}
				case 0x05: {
						AReleaseRQ pdu = new AReleaseRQ();
						pdu.Read(raw);
						Log.Info("{0} <- Association release request", LogID);
						OnReceiveReleaseRequest();
						return true;
					}
				case 0x06: {
						AReleaseRP pdu = new AReleaseRP();
						pdu.Read(raw);
						Log.Info("{0} <- Association release response", LogID);
						OnReceiveReleaseResponse();
						return true;
					}
				case 0x07: {
						AAbort pdu = new AAbort();
						pdu.Read(raw);
						Log.Info("{0} <- Association abort: {1} - {2}", LogID, pdu.Source, pdu.Reason);
						OnReceiveAbort(pdu.Source, pdu.Reason);
						return true;
					}
				case 0xFF: {
						return false;
					}
				default:
					throw new DicomNetworkException("Unknown PDU type");
				}
			} catch (SocketException) {
				throw;
			} catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Error reading PDU [type: 0x{1:x2}]: {2}", LogID, raw.Type, e.ToString());
#else
				Log.Error("{0} -> Error reading PDU [type: 0x{1:x2}]: {2}", LogID, raw.Type, e.Message);
#endif
				OnNetworkError(e);
				//String file = String.Format(@"{0}\Errors\{1}.pdu",
				//    Dicom.Debug.GetStartDirectory(), DateTime.Now.Ticks);
				//Directory.CreateDirectory(Dicom.Debug.GetStartDirectory() + @"\Errors");
				//raw.Save(file);
				return false;
			}
		}

		private bool ProcessPDataTF(PDataTF pdu) {
			try {
				byte pcid = 0;
				foreach (PDV pdv in pdu.PDVs) {
					pcid = pdv.PCID;
					if (pdv.IsCommand) {
						if (_dimse.CommandData == null)
							_dimse.CommandData = new ChunkStream();

						_dimse.CommandData.AddChunk(pdv.Value);

						if (_dimse.Command == null) {
							_dimse.Command = new DcmCommand();
						}

						if (_dimse.CommandReader == null) {
							_dimse.CommandReader = new DicomStreamReader(_dimse.CommandData);
							_dimse.CommandReader.Dataset = _dimse.Command;
						}

						_dimse.CommandReader.Read(null, DicomReadOptions.Default);

						_dimse.Progress.BytesTransfered += pdv.Value.Length;
						_dimse.Progress.EstimatedCommandLength = (int)_dimse.CommandReader.BytesEstimated;

						if (pdv.IsLastFragment) {
							_dimse.CloseCommand();

							bool isLast = true;
							if (_dimse.Command.Contains(DicomTags.DataSetType)) {
								if (_dimse.Command.GetUInt16(DicomTags.DataSetType, 0x0101) != 0x0101) {
									isLast = false;

									DcmCommandField commandField = (DcmCommandField)_dimse.Command.GetUInt16(DicomTags.CommandField, 0);
									if (commandField == DcmCommandField.CStoreRequest) {
										ushort messageID = _dimse.Command.GetUInt16(DicomTags.MessageID, 1);
										DcmPriority priority = (DcmPriority)_dimse.Command.GetUInt16(DicomTags.Priority, 0);
										DicomUID affectedInstance = _dimse.Command.GetUID(DicomTags.AffectedSOPInstanceUID);
										string moveAE = _dimse.Command.GetString(DicomTags.MoveOriginatorApplicationEntityTitle, null);
										ushort moveMessageID = _dimse.Command.GetUInt16(DicomTags.MoveOriginatorMessageID, 1);
										OnPreReceiveCStoreRequest(pcid, messageID, affectedInstance, priority, 
											moveAE, moveMessageID, out _dimse.DatasetFile);

										if (_dimse.DatasetFile != null) {
											DcmPresContext pres = Associate.GetPresentationContext(pcid);

											DicomFileFormat ff = new DicomFileFormat();
											ff.FileMetaInfo.FileMetaInformationVersion = DcmFileMetaInfo.Version;
											ff.FileMetaInfo.MediaStorageSOPClassUID = pres.AbstractSyntax;
											ff.FileMetaInfo.MediaStorageSOPInstanceUID = affectedInstance;
											ff.FileMetaInfo.TransferSyntax = pres.AcceptedTransferSyntax;
											ff.FileMetaInfo.ImplementationClassUID = Implementation.ClassUID;
											ff.FileMetaInfo.ImplementationVersionName = Implementation.Version;
											ff.FileMetaInfo.SourceApplicationEntityTitle = Associate.CalledAE;
											ff.Save(_dimse.DatasetFile, DicomWriteOptions.Default);

											_dimse.DatasetFileStream = new FileStream(_dimse.DatasetFile, FileMode.Open);
											_dimse.DatasetFileStream.Seek(0, SeekOrigin.End);
											_dimse.DatasetStream = _dimse.DatasetFileStream;
										}
									}
								}
							}
							if (isLast) {
								if (_dimse.IsNewDimse)
									OnReceiveDimseBegin(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
								OnReceiveDimseProgress(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
								OnReceiveDimse(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
								ProcessDimse(pcid);
								_dimse = null;
								return true;
							}
						}
					} else {
						if (_dimse.DatasetFile != null) {
							long pos = _dimse.DatasetFileStream.Position;
							_dimse.DatasetFileStream.Seek(0, SeekOrigin.End);
							_dimse.DatasetFileStream.Write(pdv.Value, 0, pdv.Value.Length);
							_dimse.DatasetFileStream.Position = pos;
						} else {
							if (_dimse.DatasetData == null) {
								_dimse.DatasetData = new ChunkStream();
								_dimse.DatasetStream = _dimse.DatasetData;
							}
							_dimse.DatasetData.AddChunk(pdv.Value);
						}

						if (_dimse.Dataset == null) {
							DicomTransferSyntax ts = _assoc.GetAcceptedTransferSyntax(pdv.PCID);
							_dimse.Dataset = new DcmDataset(ts);
						}

						if ((EnableStreamParse && !_dimse.Dataset.InternalTransferSyntax.IsDeflate) || pdv.IsLastFragment) {
							if (_dimse.DatasetReader == null) {
								if (_dimse.Dataset.InternalTransferSyntax.IsDeflate) {
									// DicomStreamReader needs a seekable stream
									MemoryStream ms = StreamUtility.Deflate(_dimse.DatasetStream, false);
									_dimse.DatasetReader = new DicomStreamReader(ms);
								}
								else
									_dimse.DatasetReader = new DicomStreamReader(_dimse.DatasetStream);
								_dimse.DatasetReader.Dataset = _dimse.Dataset;
							}

							_dimse.Progress.BytesTransfered += pdv.Value.Length;

							long remaining = _dimse.DatasetReader.BytesRemaining + pdv.Value.Length;
							if (remaining >= _dimse.DatasetReader.BytesNeeded || pdv.IsLastFragment) {
								if (_dimse.DatasetReader.Read(null, DicomReadOptions.Default) != DicomReadStatus.Success && pdv.IsLastFragment) {
									// ???
								}

								_dimse.Progress.EstimatedDatasetLength = (int)_dimse.DatasetReader.BytesEstimated;
							}
						}

						if (pdv.IsLastFragment) {
							_dimse.Close();

							if (_dimse.IsNewDimse)
								OnReceiveDimseBegin(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
							OnReceiveDimseProgress(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
							OnReceiveDimse(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
							ProcessDimse(pcid);
							_dimse = null;
							return true;
						}
					}
				}

				if (_dimse.IsNewDimse) {
					OnReceiveDimseBegin(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
					_dimse.IsNewDimse = false;
				} else {
					OnReceiveDimseProgress(pcid, _dimse.Command, _dimse.Dataset, _dimse.Progress);
				}

				return true;
			} catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Error reading DIMSE: {1}", LogID, e.ToString());
#else
				Log.Error("{0} -> Error reading DIMSE: {1}", LogID, e.ToString());//e.Message);
#endif
				_dimse.Abort();
				_dimse = null;
				return false;
			}
		}

		private void SaveDimseToFile(DcmDimseInfo dimse, byte pcid, string fileName) {
			string path = Path.GetFullPath(fileName);
			for (int i = 1; File.Exists(path); i++) {
				path = Path.GetFullPath(fileName);
				string ext = Path.GetExtension(path);
				path = path.Substring(0, path.Length - ext.Length);
				path += String.Format(" ({0})", i);
				path += ext;
			}

			if (String.IsNullOrEmpty(dimse.DatasetFile)) {
				DcmPresContext pres = Associate.GetPresentationContext(pcid);

				DicomFileFormat ff = new DicomFileFormat();
				ff.FileMetaInfo.FileMetaInformationVersion = DcmFileMetaInfo.Version;
				ff.FileMetaInfo.MediaStorageSOPClassUID = pres.AbstractSyntax;
				ff.FileMetaInfo.MediaStorageSOPInstanceUID = dimse.Command.AffectedSOPInstanceUID;
				ff.FileMetaInfo.TransferSyntax = pres.AcceptedTransferSyntax;
				ff.FileMetaInfo.ImplementationClassUID = Implementation.ClassUID;
				ff.FileMetaInfo.ImplementationVersionName = Implementation.Version;
				ff.FileMetaInfo.SourceApplicationEntityTitle = Associate.CalledAE;
				ff.Save(fileName, DicomWriteOptions.Default);
			}

			long pos = dimse.DatasetStream.Position;

			using (FileStream fs = File.OpenWrite(fileName)) {
				fs.Seek(0, SeekOrigin.End);
				dimse.DatasetStream.Seek(0, SeekOrigin.Begin);
				StreamUtility.Copy(dimse.DatasetStream, fs);
			}

			dimse.DatasetStream.Position = pos;
		}

		private bool ProcessDimse(byte presentationID) {
			if (!Associate.HasPresentationContextID(presentationID) ||
				Associate.GetPresentationContextResult(presentationID) != DcmPresContextResult.Accept) {
				Log.Error("{0} -> Received DIMSE for unaccepted Presentation Context ID [pcid: {1}]", LogID, presentationID);
				SendAbort(DcmAbortSource.ServiceUser, DcmAbortReason.NotSpecified);
				return true;
			}

			if (_dimse.Command == null) {
				Log.Error("{0} -> Unable to process DIMSE; Command DataSet not received.", LogID);
				SendAbort(DcmAbortSource.ServiceUser, DcmAbortReason.NotSpecified);
				return true;
			}

			DcmCommandField commandField = _dimse.Command.CommandField;

			if (commandField == DcmCommandField.CStoreRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DcmPriority priority = _dimse.Command.Priority;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				string moveAE = _dimse.Command.MoveOriginatorAE;
				ushort moveMessageID = _dimse.Command.MoveOriginatorMessageID;
				Log.Info("{0} <- C-Store request [pc: {1}; id: {2}]{3}", 
					LogID, presentationID, messageID, (_dimse.DatasetFile != null ? " (stream)" : ""));
				try {
					OnReceiveCStoreRequest(presentationID, messageID, affectedInstance, priority, moveAE, moveMessageID, _dimse.Dataset, _dimse.DatasetFile);
				} finally {
					OnPostReceiveCStoreRequest(presentationID, messageID, affectedInstance, _dimse.Dataset, _dimse.DatasetFile);
				}
				return true;
			}

			if (commandField == DcmCommandField.CStoreResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- C-Store response [id: {1}]: {2}",
						LogID, messageIdRespondedTo, status);
				}
				else {
					Log.Info("{0} <- C-Store response [id: {1}]: {2}\n\t=> {3}",
						LogID, messageIdRespondedTo, status, _dimse.Command.GetErrorString());
				}
				OnReceiveCStoreResponse(presentationID, messageIdRespondedTo, affectedInstance, status);
				return true;
			}

			if (commandField == DcmCommandField.CEchoRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DcmPriority priority = _dimse.Command.Priority;
				Log.Info("{0} <- C-Echo request [pc: {1}; id: {2}]", 
					LogID, presentationID, messageID);
				OnReceiveCEchoRequest(presentationID, messageID, priority);
				return true;
			}

			if (commandField == DcmCommandField.CEchoResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- C-Echo response [{1}]: {2}",
						LogID, messageIdRespondedTo, status);
				}
				else {
					Log.Info("{0} <- C-Echo response [{1}]: {2}\n\t=> {3}",
						LogID, messageIdRespondedTo, status, _dimse.Command.GetErrorString());
				}
				OnReceiveCEchoResponse(presentationID, messageIdRespondedTo, status);
				return true;
			}

			if (commandField == DcmCommandField.CFindRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DcmPriority priority = _dimse.Command.Priority;
				String level = _dimse.Dataset.GetString(DicomTags.QueryRetrieveLevel, "UNKNOWN");
				Log.Info("{0} <- C-Find request [pc: {1}; id: {2}; lvl: {3}]", 
					LogID, presentationID, messageID, level);
				OnReceiveCFindRequest(presentationID, messageID, priority, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.CFindResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success || status.State == DcmState.Pending) {
					Log.Info("{0} <- C-Find response [id: {1}]: {2}",
						LogID, messageIdRespondedTo, status);
				}
				else {
					Log.Info("{0} <- C-Find response [id: {1}]: {2}=>\n\t {3}",
						LogID, messageIdRespondedTo, status, _dimse.Command.GetErrorString());
				}
				OnReceiveCFindResponse(presentationID, messageIdRespondedTo, _dimse.Dataset, status);
				return true;
			}

			if (commandField == DcmCommandField.CGetRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DcmPriority priority = _dimse.Command.Priority;
				String level = _dimse.Dataset.GetString(DicomTags.QueryRetrieveLevel, "UNKNOWN");
				Log.Info("{0} <- C-Get request [pc: {1}; id: {2}; lvl: {3}]", 
					LogID, presentationID, messageID, level);
				OnReceiveCGetRequest(presentationID, messageID, priority, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.CGetResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DcmStatus status = _dimse.Command.Status;
				ushort remain = _dimse.Command.NumberOfRemainingSuboperations;
				ushort complete = _dimse.Command.NumberOfCompletedSuboperations;
				ushort warning = _dimse.Command.NumberOfWarningSuboperations;
				ushort failure = _dimse.Command.NumberOfFailedSuboperations;
				if (status.State == DcmState.Success || status.State == DcmState.Pending) {
					Log.Info("{0} <- C-Get response [id: {1}; remain: {2}; complete: {3}; warning: {4}; failure: {5}]: {6}",
						LogID, messageIdRespondedTo, remain, complete, warning, failure, status);
				}
				else {
					Log.Info("{0} <- C-Get response [id: {1}; remain: {2}; complete: {3}; warning: {4}; failure: {5}]: {6}\n\t=> {7}",
						LogID, messageIdRespondedTo, remain, complete, warning, failure, status, _dimse.Command.GetErrorString());
				}
				OnReceiveCGetResponse(presentationID, messageIdRespondedTo, _dimse.Dataset, status, remain, complete, warning, failure);
				return true;
			}

			if (commandField == DcmCommandField.CMoveRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DcmPriority priority = _dimse.Command.Priority;
				string destAE = _dimse.Command.MoveDestinationAE;
				String level = _dimse.Dataset.GetString(DicomTags.QueryRetrieveLevel, "UNKNOWN");
				Log.Info("{0} <- C-Move request [pc: {1}; id: {2}; lvl: {3}; dest: {4}]", 
					LogID, presentationID, messageID, level, destAE);
				OnReceiveCMoveRequest(presentationID, messageID, destAE, priority, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.CMoveResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DcmStatus status = _dimse.Command.Status;
				ushort remain = _dimse.Command.NumberOfRemainingSuboperations;
				ushort complete = _dimse.Command.NumberOfCompletedSuboperations;
				ushort warning = _dimse.Command.NumberOfWarningSuboperations;
				ushort failure = _dimse.Command.NumberOfFailedSuboperations;
				if (status.State == DcmState.Success || status.State == DcmState.Pending) {
					Log.Info("{0} <- C-Move response [id: {1}; remain: {2}; complete: {3}; warning: {4}; failure: {5}]: {6}",
						LogID, messageIdRespondedTo, remain, complete, warning, failure, status);
				}
				else {
					Log.Info("{0} <- C-Move response [id: {1}; remain: {2}; complete: {3}; warning: {4}; failure: {5}]: {6}\n\t=> {7}",
						LogID, messageIdRespondedTo, remain, complete, warning, failure, status, _dimse.Command.GetErrorString());
				}
				OnReceiveCMoveResponse(presentationID, messageIdRespondedTo, _dimse.Dataset, status, remain, complete, warning, failure);
				return true;
			}

			if (commandField == DcmCommandField.CCancelRequest) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				Log.Info("{0} <- C-Cancel request [pc: {1}; id: {2}]", LogID, presentationID, messageIdRespondedTo);
				OnReceiveCCancelRequest(presentationID, messageIdRespondedTo);
				return true;
			}

			if (commandField == DcmCommandField.NEventReportRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				ushort eventTypeID = _dimse.Command.EventTypeID;
				Log.Info("{0} <- N-EventReport request [pc: {1}; id: {2}; class: {3}; event: {4:x4}]",
					LogID, presentationID, messageID, affectedClass.Description, eventTypeID);
				OnReceiveNEventReportRequest(presentationID, messageID, affectedClass, affectedInstance, eventTypeID, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.NEventReportResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				ushort eventTypeID = _dimse.Command.EventTypeID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- N-EventReport response [id: {1}; class: {2}; event: {3:x4}]: {4}",
						LogID, messageIdRespondedTo, affectedClass.Description, eventTypeID, status);
				}
				else {
					Log.Info("{0} <- N-EventReport response [id: {1}; class: {2}; event: {3:x4}]: {4}\n\t=> {5}",
						LogID, messageIdRespondedTo, affectedClass.Description, eventTypeID, status, _dimse.Command.GetErrorString());
				}
				OnReceiveNEventReportResponse(presentationID, messageIdRespondedTo, affectedClass, affectedInstance, eventTypeID, _dimse.Dataset, status);
				return true;
			}

			if (commandField == DcmCommandField.NGetRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DicomUID requestedClass = _dimse.Command.RequestedSOPClassUID;
				DicomUID requestedInstance = _dimse.Command.RequestedSOPInstanceUID;
				DicomTag[] attributes = new DicomTag[0];
				if (_dimse.Command.AttributeIdentifierList != null)
					attributes = _dimse.Command.AttributeIdentifierList.GetValues();
				Log.Info("{0} <- N-Get request [pc: {1}; id: {2}; class: {3}]", 
					LogID, presentationID, messageID, requestedClass.Description);
				OnReceiveNGetRequest(presentationID, messageID, requestedClass, requestedInstance, attributes);
				return true;
			}

			if (commandField == DcmCommandField.NGetResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- N-Get response [id: {1}; class: {2}]: {3}",
						LogID, messageIdRespondedTo, affectedClass.Description, status);
				}
				else {
					Log.Info("{0} <- N-Get response [id: {1}; class: {2}]: {3}\n\t=> {4}",
						LogID, messageIdRespondedTo, affectedClass.Description, status, _dimse.Command.GetErrorString());
				}
				OnReceiveNGetResponse(presentationID, messageIdRespondedTo, affectedClass, affectedInstance, _dimse.Dataset, status);
				return true;
			}

			if (commandField == DcmCommandField.NSetRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DicomUID requestedClass = _dimse.Command.RequestedSOPClassUID;
				DicomUID requestedInstance = _dimse.Command.RequestedSOPInstanceUID;
				Log.Info("{0} <- N-Set request [pc: {1}; id: {2}; class: {3}]", 
					LogID, presentationID, messageID, requestedClass.Description);
				OnReceiveNSetRequest(presentationID, messageID, requestedClass, requestedInstance, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.NSetResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- N-Set response [id: {1}; class: {2}]: {3}",
						LogID, messageIdRespondedTo, affectedClass.Description, status);
				}
				else {
					Log.Info("{0} <- N-Set response [id: {1}; class: {2}]: {3}\n\t=> {4}",
						LogID, messageIdRespondedTo, affectedClass.Description, status, _dimse.Command.GetErrorString());
				}
				OnReceiveNSetResponse(presentationID, messageIdRespondedTo, affectedClass, affectedInstance, _dimse.Dataset, status);
				return true;
			}

			if (commandField == DcmCommandField.NActionRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DicomUID requestedClass = _dimse.Command.RequestedSOPClassUID;
				DicomUID requestedInstance = _dimse.Command.RequestedSOPInstanceUID;
				ushort actionTypeID = _dimse.Command.ActionTypeID;
				Log.Info("{0} <- N-Action request [pc: {1}; id: {2}; class: {3}; action: {4:x4}]",
					LogID, presentationID, messageID, requestedClass.Description, actionTypeID);
				OnReceiveNActionRequest(presentationID, messageID, requestedClass, requestedInstance, actionTypeID, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.NActionResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				ushort actionTypeID = _dimse.Command.ActionTypeID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- N-Action response [id: {1}; class: {2}; action: {3:x4}]: {4}",
						LogID, messageIdRespondedTo, affectedClass.Description, actionTypeID, status);
				}
				else {
					Log.Info("{0} <- N-Action response [id: {1}; class: {2}; action: {3:x4}]: {4}\n\t=> {5}",
						LogID, messageIdRespondedTo, affectedClass.Description, actionTypeID, status, _dimse.Command.GetErrorString());
				}
				OnReceiveNActionResponse(presentationID, messageIdRespondedTo, affectedClass, affectedInstance, actionTypeID, _dimse.Dataset, status);
				return true;
			}

			if (commandField == DcmCommandField.NCreateRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				Log.Info("{0} <- N-Create request [pc: {1}; id: {2}; class: {3}]", 
					LogID, presentationID, messageID, affectedClass.Description);
				OnReceiveNCreateRequest(presentationID, messageID, affectedClass, affectedInstance, _dimse.Dataset);
				return true;
			}

			if (commandField == DcmCommandField.NCreateResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- N-Create response [id: {1}; class: {2}]: {3}",
						LogID, messageIdRespondedTo, affectedClass.Description, status);
				}
				else {
					Log.Info("{0} <- N-Create response [id: {1}; class: {2}]: {3}\n\t=> {4}",
						LogID, messageIdRespondedTo, affectedClass.Description, status, _dimse.Command.GetErrorString());
				}
				OnReceiveNCreateResponse(presentationID, messageIdRespondedTo, affectedClass, affectedInstance, _dimse.Dataset, status);
				return true;
			}

			if (commandField == DcmCommandField.NDeleteRequest) {
				ushort messageID = _dimse.Command.MessageID;
				DicomUID requestedClass = _dimse.Command.RequestedSOPClassUID;
				DicomUID requestedInstance = _dimse.Command.RequestedSOPInstanceUID;
				Log.Info("{0} <- N-Delete request [pc: {1}; id: {2}; class: {3}]", 
					LogID, presentationID, messageID, requestedClass.Description);
				OnReceiveNDeleteRequest(presentationID, messageID, requestedClass, requestedInstance);
				return true;
			}

			if (commandField == DcmCommandField.NDeleteResponse) {
				ushort messageIdRespondedTo = _dimse.Command.MessageIDBeingRespondedTo;
				DicomUID affectedClass = _dimse.Command.AffectedSOPClassUID;
				DicomUID affectedInstance = _dimse.Command.AffectedSOPInstanceUID;
				DcmStatus status = _dimse.Command.Status;
				if (status.State == DcmState.Success) {
					Log.Info("{0} <- N-Delete response [id: {1}; class: {2}]: {3}",
						LogID, messageIdRespondedTo, affectedClass.Description, status);
				}
				else {
					Log.Info("{0} <- N-Delete response [id: {1}; class: {2}]: {3}\n\t=> {4}",
						LogID, messageIdRespondedTo, affectedClass.Description, status, _dimse.Command.GetErrorString());
				}
				OnReceiveNDeleteResponse(presentationID, messageIdRespondedTo, affectedClass, affectedInstance, status);
				return true;
			}

			return false;
		}

		private void SendRawPDU(RawPDU pdu) {
			try {
				lock (_socket) {
					_disableTimeout = true;
					pdu.WritePDU(_network);
					_disableTimeout = false;
				}
			}
			catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Error sending PDU [type: 0x{1:x2}]: {2}", LogID, pdu.Type, e.ToString());
#else
				Log.Error("{0} -> Error sending PDU [type: 0x{1:x2}]: {2}", LogID, pdu.Type, e.Message);
#endif
				OnNetworkError(e);
			}
		}

		private bool SendDimse(byte pcid, DcmCommand command, DcmDataset dataset) {
			try {
				_disableTimeout = true;

				DicomTransferSyntax ts = _assoc.GetAcceptedTransferSyntax(pcid);

				if (dataset != null && ts != dataset.InternalTransferSyntax) {
					if (ts.IsEncapsulated || dataset.InternalTransferSyntax.IsEncapsulated)
						throw new DicomNetworkException("Unable to transcode encapsulated transfer syntax!");
					dataset.ChangeTransferSyntax(ts, null);
				}

				DcmDimseProgress progress = new DcmDimseProgress();

				progress.EstimatedCommandLength = (int)command.CalculateWriteLength(DicomTransferSyntax.ImplicitVRLittleEndian, DicomWriteOptions.Default | DicomWriteOptions.CalculateGroupLengths);

				if (dataset != null)
					progress.EstimatedDatasetLength = (int)dataset.CalculateWriteLength(ts, DicomWriteOptions.Default);

				PDataTFStream pdustream = new PDataTFStream(_network, pcid, (int)_assoc.MaximumPduLength);
				pdustream.OnPduSent += delegate() {
					progress.BytesTransfered = pdustream.BytesSent;
					OnSendDimseProgress(pcid, command, dataset, progress);
				};

				lock (_socket) {
					OnSendDimseBegin(pcid, command, dataset, progress);

					DicomStreamWriter dsw = new DicomStreamWriter(pdustream);
					dsw.Write(command, DicomWriteOptions.Default | DicomWriteOptions.CalculateGroupLengths);

					if (dataset != null) {
						pdustream.IsCommand = false;
						dsw.Write(dataset, DicomWriteOptions.Default);
					}

					// flush last pdu
					pdustream.Flush(true);

					OnSendDimse(pcid, command, dataset, progress);
				}

				return true;
			}
			catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Error sending DIMSE: {1}", LogID, e.ToString());
#else
				Log.Error("{0} -> Error sending DIMSE: {1}", LogID, e.Message);
#endif
				OnNetworkError(e);
				return false;
			}
			finally {
				_disableTimeout = false;
			}
		}

		private bool SendDimseStream(byte pcid, DcmCommand command, Stream datastream) {
			try {
				_disableTimeout = true;

				DicomTransferSyntax ts = _assoc.GetAcceptedTransferSyntax(pcid);

				DcmDimseProgress progress = new DcmDimseProgress();

				progress.EstimatedCommandLength = (int)command.CalculateWriteLength(DicomTransferSyntax.ImplicitVRLittleEndian, DicomWriteOptions.Default | DicomWriteOptions.CalculateGroupLengths);

				if (datastream != null)
					progress.EstimatedDatasetLength = (int)datastream.Length - (int)datastream.Position;

				PDataTFStream pdustream = new PDataTFStream(_network, pcid, (int)_assoc.MaximumPduLength);
				pdustream.OnPduSent += delegate() {
					progress.BytesTransfered = pdustream.BytesSent;
					OnSendDimseProgress(pcid, command, null, progress);
				};

				lock (_socket) {
					OnSendDimseBegin(pcid, command, null, progress);

					DicomStreamWriter dsw = new DicomStreamWriter(pdustream);
					dsw.Write(command, DicomWriteOptions.Default | DicomWriteOptions.CalculateGroupLengths);
					dsw = null;

					if (datastream != null) {
						pdustream.IsCommand = false;
						pdustream.Write(datastream);
					}

					// last pdu is automatically flushed when streaming
					//pdustream.Flush(true);

					OnSendDimse(pcid, command, null, progress);
				}

				return true;
			}
			catch (Exception e) {
#if DEBUG
				Log.Error("{0} -> Error sending DIMSE: {1}", LogID, e.ToString());
#else
				Log.Error("{0} -> Error sending DIMSE: {1}", LogID, e.Message);
#endif
				OnNetworkError(e);
				return false;
			}
			finally {
				_disableTimeout = false;
			}
		}
		#endregion
	}
}
