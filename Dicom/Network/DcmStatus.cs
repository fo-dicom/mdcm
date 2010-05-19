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

namespace Dicom.Network {
	/// <summary>State of a DICOM status code</summary>
	public enum DcmState {
		/// <summary>Success.</summary>
		Success,

		/// <summary>Cancel.</summary>
		Cancel,

		/// <summary>Pending.</summary>
		Pending,

		/// <summary>Warning.</summary>
		Warning,

		/// <summary>Failure.</summary>
		Failure
	}

	/// <summary>DICOM Status</summary>
	public class DcmStatus {
		/// <summary>DICOM status code.</summary>
		public readonly ushort Code;

		/// <summary>State of this DICOM status code.</summary>
		public readonly DcmState State;

		/// <summary>Description.</summary>
		public readonly string Description;

		public readonly string ErrorComment = null;
		
		private readonly ushort Mask;

		/// <summary>
		/// Initializes a new instance of the <see cref="DcmStatus"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="status">The status.</param>
		/// <param name="desc">The desc.</param>
		public DcmStatus(string code, DcmState status, string desc) {
			Code = ushort.Parse(code.Replace('x', '0'), System.Globalization.NumberStyles.HexNumber);

			StringBuilder msb = new StringBuilder();
			msb.Append(code.ToLower());
			msb.Replace('0', 'F').Replace('1', 'F').Replace('2', 'F')
				.Replace('3', 'F').Replace('4', 'F').Replace('5', 'F')
				.Replace('6', 'F').Replace('7', 'F').Replace('8', 'F')
				.Replace('9', 'F').Replace('a', 'F').Replace('b', 'F')
				.Replace('c', 'F').Replace('d', 'F').Replace('e', 'F')
				.Replace('x', '0');
			Mask = ushort.Parse(msb.ToString(), System.Globalization.NumberStyles.HexNumber);

			State = status;
			Description = desc;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DcmStatus"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="status">The status.</param>
		/// <param name="desc">The desc.</param>
		/// <param name="comment">The comment.</param>
		public DcmStatus(string code, DcmState status, string desc, string comment) : this(code, status, desc) {
			ErrorComment = comment;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DcmStatus"/> class.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <param name="comment">The comment.</param>
		public DcmStatus(DcmStatus status, string comment) : this(String.Format("{0:x4}", status.Code), status.State, status.Description, comment) {
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString() {
			if (State == DcmState.Warning || State == DcmState.Failure) {
				if (!String.IsNullOrEmpty(ErrorComment))
					return String.Format("{0} [{1:x4}: {2}] -> {3}", State, Code, Description, ErrorComment);
				return String.Format("{0} [{1:x4}: {2}]", State, Code, Description);
			}
			return Description;
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="s1">DICOM Status</param>
		/// <param name="s2">DICOM Status</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(DcmStatus s1, DcmStatus s2) {
			if ((object)s1 == null || (object)s2 == null)
				return false;
			return (s1.Code & s2.Mask) == (s2.Code & s1.Mask);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="s1">DICOM Status</param>
		/// <param name="s2">DICOM Status</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(DcmStatus s1, DcmStatus s2) {
			if ((object)s1 == null || (object)s2 == null)
				return false;
			return !(s1 == s2);
		}


		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
		public override bool Equals(object obj) {
			return (DcmStatus)obj == this;
		}


		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode() {
			return base.GetHashCode();
		}

		#region Static
		private static List<DcmStatus> Entries = new List<DcmStatus>();

		static DcmStatus() {
			#region Load Dicom Status List
			Entries.Add(Success);
			Entries.Add(Cancel);
			Entries.Add(Pending);
			Entries.Add(AttributeListError);
			Entries.Add(AttributeValueOutOfRange);
			Entries.Add(SOPClassNotSupported);
			Entries.Add(ClassInstanceConflict);
			Entries.Add(DuplicateSOPInstance);
			Entries.Add(DuplicateInvocation);
			Entries.Add(InvalidArgumentValue);
			Entries.Add(InvalidAttributeValue);
			Entries.Add(InvalidObjectInstance);
			Entries.Add(MissingAttribute);
			Entries.Add(MissingAttributeValue);
			Entries.Add(MistypedArgument);
			Entries.Add(NoSuchArgument);
			Entries.Add(NoSuchEventType);
			Entries.Add(NoSuchObjectInstance);
			Entries.Add(NoSuchSOPClass);
			Entries.Add(ProcessingFailure);
			Entries.Add(ResourceLimitation);
			Entries.Add(UnrecognizedOperation);
			Entries.Add(NoSuchActionType);
			Entries.Add(StorageStorageOutOfResources);
			Entries.Add(StorageDataSetDoesNotMatchSOPClassError);
			Entries.Add(StorageCannotUnderstand);
			Entries.Add(StorageCoercionOfDataElements);
			Entries.Add(StorageDataSetDoesNotMatchSOPClassWarning);
			Entries.Add(StorageElementsDiscarded);
			Entries.Add(QueryRetrieveOutOfResources);
			Entries.Add(QueryRetrieveUnableToCalculateNumberOfMatches);
			Entries.Add(QueryRetrieveUnableToPerformSuboperations);
			Entries.Add(QueryRetrieveMoveDestinationUnknown);
			Entries.Add(QueryRetrieveIdentifierDoesNotMatchSOPClass);
			Entries.Add(QueryRetrieveUnableToProcess);
			Entries.Add(QueryRetrieveOptionalKeysNotSupported);
			Entries.Add(QueryRetrieveSubOpsOneOrMoreFailures);
			Entries.Add(PrintManagementMemoryAllocationNotSupported);
			Entries.Add(PrintManagementFilmSessionPrintingNotSupported);
			Entries.Add(PrintManagementFilmSessionEmptyPage);
			Entries.Add(PrintManagementFilmBoxEmptyPage);
			Entries.Add(PrintManagementImageDemagnified);
			Entries.Add(PrintManagementMinMaxDensityOutOfRange);
			Entries.Add(PrintManagementImageCropped);
			Entries.Add(PrintManagementImageDecimated);
			Entries.Add(PrintManagementFilmSessionEmpty);
			Entries.Add(PrintManagementPrintQueueFull);
			Entries.Add(PrintManagementImageLargerThanImageBox);
			Entries.Add(PrintManagementInsufficientMemoryInPrinter);
			Entries.Add(PrintManagementCombinedImageLargerThanImageBox);
			Entries.Add(PrintManagementExistingFilmBoxNotPrinted);
			Entries.Add(MediaCreationManagementDuplicateInitiateMediaCreation);
			Entries.Add(MediaCreationManagementMediaCreationRequestAlreadyCompleted);
			Entries.Add(MediaCreationManagementMediaCreationRequestAlreadyInProgress);
			Entries.Add(MediaCreationManagementCancellationDeniedForUnspecifiedReason);
			#endregion
		}

		/// <summary>
		/// Looks up the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static DcmStatus Lookup(ushort code) {
			foreach (DcmStatus status in Entries) {
				if (status.Code == (code & status.Mask))
					return status;
			}
			return ProcessingFailure;
		}

		#region Dicom Statuses
		/// <summary>Success: Success</summary>
		public static DcmStatus Success = new DcmStatus("0000", DcmState.Success, "Success");

		/// <summary>Cancel: Cancel</summary>
		public static DcmStatus Cancel = new DcmStatus("FE00", DcmState.Cancel, "Cancel");

		/// <summary>Pending: Pending</summary>
		public static DcmStatus Pending = new DcmStatus("FF00", DcmState.Pending, "Pending");

		/// <summary>Warning: Attribute list error</summary>
		public static DcmStatus AttributeListError = new DcmStatus("0107", DcmState.Warning, "Attribute list error");

		/// <summary>Warning: Attribute Value Out of Range</summary>
		public static DcmStatus AttributeValueOutOfRange = new DcmStatus("0116", DcmState.Warning, "Attribute Value Out of Range");

		/// <summary>Failure: Refused: SOP class not supported</summary>
		public static DcmStatus SOPClassNotSupported = new DcmStatus("0122", DcmState.Failure, "Refused: SOP class not supported");

		/// <summary>Failure: Class-instance conflict</summary>
		public static DcmStatus ClassInstanceConflict = new DcmStatus("0119", DcmState.Failure, "Class-instance conflict");

		/// <summary>Failure: Duplicate SOP instance</summary>
		public static DcmStatus DuplicateSOPInstance = new DcmStatus("0111", DcmState.Failure, "Duplicate SOP instance");

		/// <summary>Failure: Duplicate invocation</summary>
		public static DcmStatus DuplicateInvocation = new DcmStatus("0210", DcmState.Failure, "Duplicate invocation");

		/// <summary>Failure: Invalid argument value</summary>
		public static DcmStatus InvalidArgumentValue = new DcmStatus("0115", DcmState.Failure, "Invalid argument value");

		/// <summary>Failure: Invalid attribute value</summary>
		public static DcmStatus InvalidAttributeValue = new DcmStatus("0106", DcmState.Failure, "Invalid attribute value");

		/// <summary>Failure: Invalid object instance</summary>
		public static DcmStatus InvalidObjectInstance = new DcmStatus("0117", DcmState.Failure, "Invalid object instance");

		/// <summary>Failure: Missing attribute</summary>
		public static DcmStatus MissingAttribute = new DcmStatus("0120", DcmState.Failure, "Missing attribute");

		/// <summary>Failure: Missing attribute value</summary>
		public static DcmStatus MissingAttributeValue = new DcmStatus("0121", DcmState.Failure, "Missing attribute value");

		/// <summary>Failure: Mistyped argument</summary>
		public static DcmStatus MistypedArgument = new DcmStatus("0212", DcmState.Failure, "Mistyped argument");

		/// <summary>Failure: No such argument</summary>
		public static DcmStatus NoSuchArgument = new DcmStatus("0114", DcmState.Failure, "No such argument");

		/// <summary>Failure: No such event type</summary>
		public static DcmStatus NoSuchEventType = new DcmStatus("0113", DcmState.Failure, "No such event type");

		/// <summary>Failure: No Such object instance</summary>
		public static DcmStatus NoSuchObjectInstance = new DcmStatus("0112", DcmState.Failure, "No Such object instance");

		/// <summary>Failure: No Such SOP class</summary>
		public static DcmStatus NoSuchSOPClass = new DcmStatus("0118", DcmState.Failure, "No Such SOP class");

		/// <summary>Failure: Processing failure</summary>
		public static DcmStatus ProcessingFailure = new DcmStatus("0110", DcmState.Failure, "Processing failure");

		/// <summary>Failure: Resource limitation</summary>
		public static DcmStatus ResourceLimitation = new DcmStatus("0213", DcmState.Failure, "Resource limitation");

		/// <summary>Failure: Unrecognized operation</summary>
		public static DcmStatus UnrecognizedOperation = new DcmStatus("0211", DcmState.Failure, "Unrecognized operation");

		/// <summary>Failure: No such action type</summary>
		public static DcmStatus NoSuchActionType = new DcmStatus("0123", DcmState.Failure, "No such action type");

		/// <summary>Storage Failure: Out of Resources</summary>
		public static DcmStatus StorageStorageOutOfResources = new DcmStatus("A7xx", DcmState.Failure, "Out of Resources");

		/// <summary>Storage Failure: Data Set does not match SOP Class (Error)</summary>
		public static DcmStatus StorageDataSetDoesNotMatchSOPClassError = new DcmStatus("A9xx", DcmState.Failure, "Data Set does not match SOP Class (Error)");

		/// <summary>Storage Failure: Cannot understand</summary>
		public static DcmStatus StorageCannotUnderstand = new DcmStatus("Cxxx", DcmState.Failure, "Cannot understand");

		/// <summary>Storage Warning: Coercion of Data Elements</summary>
		public static DcmStatus StorageCoercionOfDataElements = new DcmStatus("B000", DcmState.Warning, "Coercion of Data Elements");

		/// <summary>Storage Warning: Data Set does not match SOP Class (Warning)</summary>
		public static DcmStatus StorageDataSetDoesNotMatchSOPClassWarning = new DcmStatus("B007", DcmState.Warning, "Data Set does not match SOP Class (Warning)");

		/// <summary>Storage Warning: Elements Discarded</summary>
		public static DcmStatus StorageElementsDiscarded = new DcmStatus("B006", DcmState.Warning, "Elements Discarded");

		/// <summary>QueryRetrieve Failure: Out of Resources</summary>
		public static DcmStatus QueryRetrieveOutOfResources = new DcmStatus("A700", DcmState.Failure, "Out of Resources");

		/// <summary>QueryRetrieve Failure: Unable to calculate number of matches</summary>
		public static DcmStatus QueryRetrieveUnableToCalculateNumberOfMatches = new DcmStatus("A701", DcmState.Failure, "Unable to calculate number of matches");

		/// <summary>QueryRetrieve Failure: Unable to perform suboperations</summary>
		public static DcmStatus QueryRetrieveUnableToPerformSuboperations = new DcmStatus("A702", DcmState.Failure, "Unable to perform suboperations");

		/// <summary>QueryRetrieve Failure: Move Destination unknown</summary>
		public static DcmStatus QueryRetrieveMoveDestinationUnknown = new DcmStatus("A801", DcmState.Failure, "Move Destination unknown");

		/// <summary>QueryRetrieve Failure: Identifier does not match SOP Class</summary>
		public static DcmStatus QueryRetrieveIdentifierDoesNotMatchSOPClass = new DcmStatus("A900", DcmState.Failure, "Identifier does not match SOP Class");

		/// <summary>QueryRetrieve Failure: Unable to process</summary>
		public static DcmStatus QueryRetrieveUnableToProcess = new DcmStatus("Cxxx", DcmState.Failure, "Unable to process");

		/// <summary>QueryRetrieve Pending: Optional Keys Not Supported</summary>
		public static DcmStatus QueryRetrieveOptionalKeysNotSupported = new DcmStatus("FF01", DcmState.Pending, "Optional Keys Not Supported");

		/// <summary>QueryRetrieve Warning: Sub-operations Complete - One or more Failures</summary>
		public static DcmStatus QueryRetrieveSubOpsOneOrMoreFailures = new DcmStatus("B000", DcmState.Warning, "Sub-operations Complete - One or more Failures");

		/// <summary>PrintManagement Warning: Memory allocation not supported</summary>
		public static DcmStatus PrintManagementMemoryAllocationNotSupported = new DcmStatus("B000", DcmState.Warning, "Memory allocation not supported");

		/// <summary>PrintManagement Warning: Film session printing (collation) is not supported</summary>
		public static DcmStatus PrintManagementFilmSessionPrintingNotSupported = new DcmStatus("B601", DcmState.Warning, "Film session printing (collation) is not supported");

		/// <summary>PrintManagement Warning: Film session SOP instance hierarchy does not contain image box SOP instances (empty page)</summary>
		public static DcmStatus PrintManagementFilmSessionEmptyPage = new DcmStatus("B602", DcmState.Warning, "Film session SOP instance hierarchy does not contain image box SOP instances (empty page)");

		/// <summary>PrintManagement Warning: Film box SOP instance hierarchy does not contain image box SOP instances (empty page)</summary>
		public static DcmStatus PrintManagementFilmBoxEmptyPage = new DcmStatus("B603", DcmState.Warning, "Film box SOP instance hierarchy does not contain image box SOP instances (empty page)");

		/// <summary>PrintManagement Warning: Image size is larger than image box size, the image has been demagnified</summary>
		public static DcmStatus PrintManagementImageDemagnified = new DcmStatus("B604", DcmState.Warning, "Image size is larger than image box size, the image has been demagnified");

		/// <summary>PrintManagement Warning: Requested min density or max density outside of printer's operating range</summary>
		public static DcmStatus PrintManagementMinMaxDensityOutOfRange = new DcmStatus("B605", DcmState.Warning, "Requested min density or max density outside of printer's operating range");

		/// <summary>PrintManagement Warning: Image size is larger than the image box size, the Image has been cropped to fit</summary>
		public static DcmStatus PrintManagementImageCropped = new DcmStatus("B609", DcmState.Warning, "Image size is larger than the image box size, the Image has been cropped to fit");

		/// <summary>PrintManagement Warning: Image size or combined print image size is larger than the image box size, image or combined print image has been decimated to fit</summary>
		public static DcmStatus PrintManagementImageDecimated = new DcmStatus("B60A", DcmState.Warning, "Image size or combined print image size is larger than the image box size, image or combined print image has been decimated to fit");

		/// <summary>PrintManagement Failure: Film session SOP instance hierarchy does not contain film box SOP instances</summary>
		public static DcmStatus PrintManagementFilmSessionEmpty = new DcmStatus("C600", DcmState.Failure, "Film session SOP instance hierarchy does not contain film box SOP instances");

		/// <summary>PrintManagement Failure: Unable to create Print Job SOP Instance; print queue is full</summary>
		public static DcmStatus PrintManagementPrintQueueFull = new DcmStatus("C601", DcmState.Failure, "Unable to create Print Job SOP Instance; print queue is full");

		/// <summary>PrintManagement Failure: Image size is larger than image box size</summary>
		public static DcmStatus PrintManagementImageLargerThanImageBox = new DcmStatus("C603", DcmState.Failure, "Image size is larger than image box size");

		/// <summary>PrintManagement Failure: Insufficient memory in printer to store the image</summary>
		public static DcmStatus PrintManagementInsufficientMemoryInPrinter = new DcmStatus("C605", DcmState.Failure, "Insufficient memory in printer to store the image");

		/// <summary>PrintManagement Failure: Combined Print Image size is larger than the Image Box size</summary>
		public static DcmStatus PrintManagementCombinedImageLargerThanImageBox = new DcmStatus("C613", DcmState.Failure, "Combined Print Image size is larger than the Image Box size");

		/// <summary>PrintManagement Failure: There is an existing film box that has not been printed and N-ACTION at the Film Session level is not supported.</summary>
		public static DcmStatus PrintManagementExistingFilmBoxNotPrinted = new DcmStatus("C616", DcmState.Failure, "There is an existing film box that has not been printed and N-ACTION at the Film Session level is not supported.");

		/// <summary>MediaCreationManagement Failure: Refused because an Initiate Media Creation action has already been received for this SOP Instance</summary>
		public static DcmStatus MediaCreationManagementDuplicateInitiateMediaCreation = new DcmStatus("A510", DcmState.Failure, "Refused because an Initiate Media Creation action has already been received for this SOP Instance");

		/// <summary>MediaCreationManagement Failure: Media creation request already completed</summary>
		public static DcmStatus MediaCreationManagementMediaCreationRequestAlreadyCompleted = new DcmStatus("C201", DcmState.Failure, "Media creation request already completed");

		/// <summary>MediaCreationManagement Failure: Media creation request already in progress and cannot be interrupted</summary>
		public static DcmStatus MediaCreationManagementMediaCreationRequestAlreadyInProgress = new DcmStatus("C202", DcmState.Failure, "Media creation request already in progress and cannot be interrupted");

		/// <summary>MediaCreationManagement Failure: Cancellation denied for unspecified reason</summary>
		public static DcmStatus MediaCreationManagementCancellationDeniedForUnspecifiedReason = new DcmStatus("C203", DcmState.Failure, "Cancellation denied for unspecified reason");
		#endregion
		#endregion
	}
}
