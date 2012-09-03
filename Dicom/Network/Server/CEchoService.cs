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

namespace Dicom.Network.Server {
	public class CEchoService : DcmServiceBase {
		public CEchoService() : base() {
			LogID = "C-Echo SCP";
		}

		protected override void OnReceiveAssociateRequest(DcmAssociate association) {
			association.NegotiateAsyncOps = false;
			LogID = association.CallingAE;
			foreach (DcmPresContext pc in association.GetPresentationContexts()) {
				if (pc.AbstractSyntax == DicomUID.VerificationSOPClass) {
					if (pc.HasTransfer(DicomTransferSyntax.ImplicitVRLittleEndian)) {
						pc.SetResult(DcmPresContextResult.Accept, DicomTransferSyntax.ImplicitVRLittleEndian);
					}
					else if (pc.HasTransfer(DicomTransferSyntax.ExplicitVRLittleEndian)) {
						pc.SetResult(DcmPresContextResult.Accept, DicomTransferSyntax.ExplicitVRLittleEndian);
					}
					else {
						pc.SetResult(DcmPresContextResult.RejectTransferSyntaxesNotSupported);
					}
				} else {
					pc.SetResult(DcmPresContextResult.RejectAbstractSyntaxNotSupported);
				}
			}
			SendAssociateAccept(association);
		}

		protected override void OnReceiveCEchoRequest(byte presentationID, ushort messageID, DcmPriority priority) {
			SendCEchoResponse(presentationID, messageID, DcmStatus.Success);
		}
	}
}
