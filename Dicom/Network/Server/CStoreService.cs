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
using System.Text;

using Dicom.Data;

namespace Dicom.Network.Server {
	public delegate DcmStatus DcmCStoreEchoCallback(CStoreService client, byte presentationID, ushort messageID, DcmPriority priority);
	public delegate void DcmCStoreDimseCallback(CStoreService client, byte presentationID, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress);
	public delegate DcmStatus DcmCStoreCallback(CStoreService client, byte presentationID, ushort messageID, DicomUID affectedInstance, 
										DcmPriority priority, string moveAE, ushort moveMessageID, DcmDataset dataset, string fileName);
	public delegate DcmAssociateResult DcmAssociationCallback(CStoreService client, DcmAssociate association);

	public class CStoreService : DcmServiceBase {
		public DcmCStoreEchoCallback OnCEchoRequest;
		public DcmCStoreCallback OnCStoreRequest;
		public DcmCStoreDimseCallback OnCStoreRequestBegin;
		public DcmCStoreDimseCallback OnCStoreRequestProgress;
		public DcmAssociationCallback OnAssociationRequest;

		public CStoreService() : base() {
			UseFileBuffer = true;
			LogID = "C-Store SCP";
		}

		protected override void OnReceiveAssociateRequest(DcmAssociate association) {
			association.NegotiateAsyncOps = false;
			LogID = association.CallingAE;
			if (OnAssociationRequest != null) {
				DcmAssociateResult result = OnAssociationRequest(this, association);
				if (result == DcmAssociateResult.RejectCalledAE) {
					SendAssociateReject(DcmRejectResult.Permanent, DcmRejectSource.ServiceUser, DcmRejectReason.CalledAENotRecognized);
					return;
				}
				else if (result == DcmAssociateResult.RejectCallingAE) {
					SendAssociateReject(DcmRejectResult.Permanent, DcmRejectSource.ServiceUser, DcmRejectReason.CallingAENotRecognized);
					return;
				}
				else if (result == DcmAssociateResult.RejectNoReason) {
					SendAssociateReject(DcmRejectResult.Permanent, DcmRejectSource.ServiceUser, DcmRejectReason.NoReasonGiven);
					return;
				}
				else {
					foreach (DcmPresContext pc in association.GetPresentationContexts()) {
						if (pc.Result == DcmPresContextResult.Proposed)
							pc.SetResult(DcmPresContextResult.RejectNoReason);
					}
				}
			}
			else {
				DcmAssociateProfile profile = DcmAssociateProfile.Find(association, true);
				profile.Apply(association);
			}
			SendAssociateAccept(association);
		}

		protected override void OnReceiveCEchoRequest(byte presentationID, ushort messageID, DcmPriority priority) {
			DcmStatus status = DcmStatus.Success;
			if (OnCEchoRequest != null)
				status = OnCEchoRequest(this, presentationID, messageID, priority);
			SendCEchoResponse(presentationID, messageID, status);
		}

		protected override void OnReceiveCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance, 
			DcmPriority priority, string moveAE, ushort moveMessageID, DcmDataset dataset, string fileName)
		{
			DcmStatus status = DcmStatus.Success;

			if (OnCStoreRequest != null)
				status = OnCStoreRequest(this, presentationID, messageID, affectedInstance, priority, moveAE, moveMessageID, dataset, fileName);

			SendCStoreResponse(presentationID, messageID, affectedInstance, status);
		}

		protected override void OnReceiveDimseBegin(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
			if (command.CommandField == DcmCommandField.CStoreRequest && OnCStoreRequestBegin != null)
				OnCStoreRequestBegin(this, pcid, command, dataset, progress);
		}

		protected override void OnReceiveDimseProgress(byte pcid, DcmCommand command, DcmDataset dataset, DcmDimseProgress progress) {
			if (command.CommandField == DcmCommandField.CStoreRequest && OnCStoreRequestProgress != null)
				OnCStoreRequestProgress(this, pcid, command, dataset, progress);
		}
	}
}
