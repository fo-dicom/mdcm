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
using System.Text;

using Dicom.Data;

namespace Dicom.Network {
	public enum DcmRoleSelection {
		Disabled,
		SCU,
		SCP,
		Both,
		None
	}

	public enum DcmPresContextResult : byte {
		Proposed = 255,
		Accept = 0,
		RejectUser = 1,
		RejectNoReason = 2,
		RejectAbstractSyntaxNotSupported = 3,
		RejectTransferSyntaxesNotSupported = 4
	}

	public class DcmPresContext {
		#region Private Members
		private byte _pcid;
		private DcmPresContextResult _result;
		private DicomUID _abstract;
		private List<DicomTransferSyntax> _transfers;
		#endregion

		#region Public Constructor
		public DcmPresContext(byte pcid, DicomUID abstractSyntax) {
			_pcid = pcid;
			_result = DcmPresContextResult.Proposed;
			_abstract = abstractSyntax;
			_transfers = new List<DicomTransferSyntax>();
		}

		internal DcmPresContext(byte pcid, DicomUID abstractSyntax, DicomTransferSyntax transferSyntax, DcmPresContextResult result) {
			_pcid = pcid;
			_result = result;
			_abstract = abstractSyntax;
			_transfers = new List<DicomTransferSyntax>();
			_transfers.Add(transferSyntax);
		}
		#endregion

		#region Public Properties
		public byte ID {
			get { return _pcid; }
		}

		public DcmPresContextResult Result {
			get { return _result; }
		}

		public DicomUID AbstractSyntax {
			get { return _abstract; }
		}

		public DicomTransferSyntax AcceptedTransferSyntax {
			get {
				if (_transfers.Count > 0)
					return _transfers[0];
				return null;
			}
		}
		#endregion

		#region Public Members
		public void SetResult(DcmPresContextResult result) {
			SetResult(result, _transfers[0]);
		}

		public void SetResult(DcmPresContextResult result, DicomTransferSyntax acceptedTs) {
			_transfers.Clear();
			_transfers.Add(acceptedTs);
			_result = result;
		}

		public bool AcceptTransferSyntaxes(params DicomTransferSyntax[] acceptedTs) {
			if (Result == DcmPresContextResult.Accept)
				return true;
			foreach (DicomTransferSyntax ts in acceptedTs) {
				if (HasTransfer(ts)) {
					SetResult(DcmPresContextResult.Accept, ts);
					return true;
				}
			}
			return false;
		}

		public void AddTransfer(DicomTransferSyntax ts) {
			if (!_transfers.Contains(ts))
				_transfers.Add(ts);
		}

		public void RemoveTransfer(DicomTransferSyntax ts) {
			if (_transfers.Contains(ts))
				_transfers.Remove(ts);
		}

		public void ClearTransfers() {
			_transfers.Clear();
		}

		public IList<DicomTransferSyntax> GetTransfers() {
			return _transfers.AsReadOnly();
		}

		public bool HasTransfer(DicomTransferSyntax ts) {
			return _transfers.Contains(ts);
		}

		public string GetResultDescription() {
			switch (_result) {
			case DcmPresContextResult.Accept:
				return "Accept";
			case DcmPresContextResult.Proposed:
				return "Proposed";
			case DcmPresContextResult.RejectAbstractSyntaxNotSupported:
				return "Reject - Abstract Syntax Not Supported";
			case DcmPresContextResult.RejectNoReason:
				return "Reject - No Reason";
			case DcmPresContextResult.RejectTransferSyntaxesNotSupported:
				return "Reject - Transfer Syntaxes Not Supported";
			case DcmPresContextResult.RejectUser:
				return "Reject - User";
			default:
				return "Unknown";
			}
		}
		#endregion
	}

	public class DcmAssociate {
		#region Private Members
		private DicomUID _appCtxNm;
		private DicomUID _implClass;
		private string _implVersion;
		private uint _maxPdu;
		private string _calledAe;
		private string _callingAe;
		private bool _negotiateAsync;
		private int _opsInvoked;
		private int _opsPerformed;
		private SortedList<byte, DcmPresContext> _presContexts;
		#endregion

		#region Public Constructor
		public DcmAssociate() {
			_maxPdu = (uint)PDataTFStream.MaxPduSizeLimit;
			_appCtxNm = DicomUID.DICOMApplicationContextName;
			_implClass = Implementation.ClassUID;
			_implVersion = Implementation.Version;
			_presContexts = new SortedList<byte, DcmPresContext>();
			_negotiateAsync = false;
			_opsInvoked = 1;
			_opsPerformed = 1;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the Application Context Name.
		/// </summary>
		public DicomUID ApplicationContextName {
			get { return _appCtxNm; }
			set { _appCtxNm = value; }
		}

		/// <summary>
		/// Gets or sets the Implementation Class UID.
		/// </summary>
		public DicomUID ImplementationClass {
			get { return _implClass; }
			set { _implClass = value; }
		}

		/// <summary>
		/// Gets or sets the Implementation Version Name.
		/// </summary>
		public string ImplementationVersion {
			get { return _implVersion; }
			set { _implVersion = value; }
		}

		/// <summary>
		/// Gets or sets the Maximum PDU Length.
		/// </summary>
		public uint MaximumPduLength {
			get { return _maxPdu; }
			set { _maxPdu = value; }
		}

		/// <summary>
		/// Gets or sets the Called AE title.
		/// </summary>
		public string CalledAE {
			get { return _calledAe; }
			set { _calledAe = value; }
		}

		/// <summary>
		/// Gets or sets the Calling AE title.
		/// </summary>
		public string CallingAE {
			get { return _callingAe; }
			set { _callingAe = value; }
		}

		public bool NegotiateAsyncOps {
			get { return _negotiateAsync; }
			set { _negotiateAsync = value; }
		}

		public int AsyncOpsInvoked {
			get { return _opsInvoked; }
			set {
				_opsInvoked = value;
				_negotiateAsync = true;
			}
		}

		public int AsyncOpsPerformed {
			get { return _opsPerformed; }
			set {
				_opsPerformed = value;
				_negotiateAsync = true;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Adds a Presentation Context to the DICOM Associate.
		/// </summary>
		public void AddPresentationContext(byte pcid, DicomUID abstractSyntax) {
			_presContexts.Add(pcid, new DcmPresContext(pcid, abstractSyntax));
		}

		/// <summary>
		/// Adds a Presentation Context to the DICOM Associate.
		/// </summary>
		public byte AddPresentationContext(DicomUID abstractSyntax) {
			byte pcid = 1;
			foreach (byte id in _presContexts.Keys) {
				if (id >= pcid)
					pcid = (byte)(id + 2);
			}
			AddPresentationContext(pcid, abstractSyntax);
			return pcid;
		}

		public byte AddOrGetPresentationContext(DicomUID abstractSyntax) {
			byte pcid = 1;
			foreach (byte id in _presContexts.Keys) {
				if (_presContexts[id].AbstractSyntax == abstractSyntax)
				    return id;
				if (id >= pcid)
					pcid = (byte)(id + 2);
			}
			AddPresentationContext(pcid, abstractSyntax);
			return pcid;
		}

		/// <summary>
		/// Determines if the specified Presentation Context ID exists.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <returns>True if exists.</returns>
		public bool HasPresentationContextID(byte pcid) {
			return _presContexts.ContainsKey(pcid);
		}

		/// <summary>
		/// Gets a list of the Presentation Context IDs in the DICOM Associate.
		/// </summary>
		public IList<byte> GetPresentationContextIDs() {
			return _presContexts.Keys;
		}

		/// <summary>
		/// Sets the result of the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <param name="result">Result</param>
		public void SetPresentationContextResult(byte pcid, DcmPresContextResult result) {
			GetPresentationContext(pcid).SetResult(result);
		}

		/// <summary>
		/// Gets the result of the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <returns>Result</returns>
		public DcmPresContextResult GetPresentationContextResult(byte pcid) {
			return GetPresentationContext(pcid).Result;
		}

		/// <summary>
		/// Adds a Transfer Syntax to the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <param name="ts">Transfer Syntax</param>
		public void AddTransferSyntax(byte pcid, DicomTransferSyntax ts) {
			GetPresentationContext(pcid).AddTransfer(ts);
		}

		/// <summary>
		/// Gets the number of Transfer Syntaxes in the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <returns>Number of Transfer Syntaxes</returns>
		public int GetTransferSyntaxCount(byte pcid) {
			return GetPresentationContext(pcid).GetTransfers().Count;
		}

		/// <summary>
		/// Gets the Transfer Syntax at the specified index.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <param name="index">Index of Transfer Syntax</param>
		/// <returns>Transfer Syntax</returns>
		public DicomTransferSyntax GetTransferSyntax(byte pcid, int index) {
			return GetPresentationContext(pcid).GetTransfers()[index];
		}

		/// <summary>
		/// Removes a Transfer Syntax from the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <param name="ts">Transfer Syntax</param>
		public void RemoveTransferSyntax(byte pcid, DicomTransferSyntax ts) {
			GetPresentationContext(pcid).RemoveTransfer(ts);
		}

		/// <summary>
		/// Gets the Abstract Syntax for the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <returns>Abstract Syntax</returns>
		public DicomUID GetAbstractSyntax(byte pcid) {
			return GetPresentationContext(pcid).AbstractSyntax;
		}

		/// <summary>
		/// Gets the accepted Transfer Syntax for the specified Presentation Context.
		/// </summary>
		/// <param name="pcid">Presentation Context ID</param>
		/// <returns>Transfer Syntax</returns>
		public DicomTransferSyntax GetAcceptedTransferSyntax(byte pcid) {
			return GetPresentationContext(pcid).AcceptedTransferSyntax;
		}

		public void SetAcceptedTransferSyntax(byte pcid, int index) {
			DicomTransferSyntax ts = GetPresentationContext(pcid).GetTransfers()[index];
			GetPresentationContext(pcid).ClearTransfers();
			GetPresentationContext(pcid).AddTransfer(ts);
		}

		public void SetAcceptedTransferSyntax(byte pcid, DicomTransferSyntax ts) {
			GetPresentationContext(pcid).ClearTransfers();
			GetPresentationContext(pcid).AddTransfer(ts);
		}

		/// <summary>
		/// Finds the Presentation Context with the specified Abstract Syntax.
		/// </summary>
		/// <param name="abstractSyntax">Abstract Syntax</param>
		/// <returns>Presentation Context ID</returns>
		public byte FindAbstractSyntax(DicomUID abstractSyntax) {
			foreach (DcmPresContext ctx in _presContexts.Values) {
				if (ctx.AbstractSyntax == abstractSyntax && ctx.Result == DcmPresContextResult.Accept)
					return ctx.ID;
			}
			foreach (DcmPresContext ctx in _presContexts.Values) {
				if (ctx.AbstractSyntax == abstractSyntax)
					return ctx.ID;
			}
			return 0;
		}

		/// <summary>
		/// Finds the Presentation Context with the specified Abstract Syntax and Transfer Syntax.
		/// </summary>
		/// <param name="abstractSyntax">Abstract Syntax</param>
		/// <param name="transferSyntax">Transfer Syntax</param>
		/// <returns>Presentation Context ID</returns>
		public byte FindAbstractSyntaxWithTransferSyntax(DicomUID abstractSyntax, DicomTransferSyntax trasferSyntax) {
			foreach (DcmPresContext ctx in _presContexts.Values) {
				if (ctx.AbstractSyntax == abstractSyntax && ctx.HasTransfer(trasferSyntax))
					return ctx.ID;
			}
			return 0;
		}

		/// <summary>
		/// Finds the Presentation Context with the specified Abstract Syntax and Transfer Syntax.
		/// </summary>
		/// <param name="abstractSyntax">Abstract Syntax</param>
		/// <param name="transferSyntax">Transfer Syntax</param>
		/// <returns>Presentation Context ID</returns>
		public byte FindAcceptedAbstractSyntaxWithTransferSyntax(DicomUID abstractSyntax, DicomTransferSyntax trasferSyntax) {
			foreach (DcmPresContext ctx in _presContexts.Values) {
				if (ctx.Result == DcmPresContextResult.Accept && ctx.AbstractSyntax == abstractSyntax && ctx.HasTransfer(trasferSyntax))
					return ctx.ID;
			}
			return 0;
		}

		public void AddPresentationContext(byte pcid, DicomUID abstractSyntax, DicomTransferSyntax transferSyntax, DcmPresContextResult result) {
			_presContexts.Add(pcid, new DcmPresContext(pcid, abstractSyntax, transferSyntax, result));
		}

		public DcmPresContext GetPresentationContext(byte pcid) {
			DcmPresContext ctx = null;
			if (!_presContexts.TryGetValue(pcid, out ctx))
				throw new DicomNetworkException("Invalid Presentaion Context ID");
			return ctx;
		}

		public IList<DcmPresContext> GetPresentationContexts() {
			return _presContexts.Values;
		}
		#endregion

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Application Context:     {0}\n", _appCtxNm);
			sb.AppendFormat("Implementation Class:    {0}\n", _implClass);
			sb.AppendFormat("Implementation Version:  {0}\n", _implVersion);
			sb.AppendFormat("Maximum PDU Size:        {0}\n", _maxPdu);
			sb.AppendFormat("Called AE Title:         {0}\n", _calledAe);
			sb.AppendFormat("Calling AE Title:        {0}\n", _callingAe);
			if (NegotiateAsyncOps)
				sb.AppendFormat("Asynchronous Operations: {0}:{1}\n", _opsInvoked, _opsPerformed);
			sb.AppendFormat("Presentation Contexts:   {0}\n", _presContexts.Count);
			foreach (DcmPresContext pctx in _presContexts.Values) {
				sb.AppendFormat("  Presentation Context:  {0} [{1}]\n", pctx.ID, pctx.GetResultDescription());
				sb.AppendFormat("      Abstract:  {0}\n", (pctx.AbstractSyntax.Type == DicomUidType.Unknown) ?
					pctx.AbstractSyntax.UID : pctx.AbstractSyntax.Description);
				foreach (DicomTransferSyntax ts in pctx.GetTransfers()) {
					sb.AppendFormat("      Transfer:  {0}\n", (ts.UID.Type == DicomUidType.Unknown) ?
						ts.UID.UID : ts.UID.Description);
				}
			}
			sb.Length = sb.Length - 1;
			return sb.ToString();
		}
	}
}
