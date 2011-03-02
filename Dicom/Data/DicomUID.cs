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
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Dicom.Data {
	public enum DicomUidType {
		TransferSyntax,
		SOPClass,
		MetaSOPClass,
		SOPInstance,
		ApplicationContextName,
		CodingScheme,
		FrameOfReference,
		LDAP,
		Unknown
	}

	public enum DicomStorageCategory {
		None,
		Image,
		PresentationState,
		StructuredReport,
		Waveform,
		Document,
		Raw,
		Other
	}

	public class DicomUID {
		public readonly string UID;
		public readonly string Description;
		public readonly DicomUidType Type;

		private DicomUID() { }

		internal DicomUID(string uid, string desc, DicomUidType type) {
			UID = uid;
			Description = desc;
			Type = type;
		}

		public override string ToString() {
			if (Type == DicomUidType.Unknown)
				return UID;
			return Description;
		}

		public override bool Equals(object obj) {
			if (obj is DicomUID)
				return ((DicomUID)obj).UID.Equals(UID);
			if (obj is String)
				return (String)obj == UID;
			return false;
		}

		public override int GetHashCode() {
			return UID.GetHashCode();
		}

		private static DicomUID _instanceRootUid = null;
		private static DicomUID InstanceRootUID {
			get {
				if (_instanceRootUid == null) {
					lock (GenerateUidLock) {
						if (_instanceRootUid == null) {
							NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
							for (int i = 0; i < interfaces.Length; i++) {
								if (NetworkInterface.LoopbackInterfaceIndex != i && interfaces[i].OperationalStatus == OperationalStatus.Up) {
									string hex = interfaces[i].GetPhysicalAddress().ToString();
									if (!String.IsNullOrEmpty(hex)) {
										try {
											long mac = long.Parse(hex, NumberStyles.HexNumber);
											return Generate(Implementation.ClassUID, mac);
										} catch {
										}
									}
								}
							}
							_instanceRootUid = Generate(Implementation.ClassUID, Environment.TickCount);
						}
					}
				}
				return _instanceRootUid;
			}
		}

		private static long LastTicks = 0;
		private static object GenerateUidLock = new object();
		private static DateTime Y2K = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		public static DicomUID Generate() {
			lock (GenerateUidLock) {
				long ticks = DateTime.UtcNow.Subtract(Y2K).Ticks;
				while (ticks == LastTicks) {
					Thread.Sleep(1);
					ticks = DateTime.UtcNow.Subtract(Y2K).Ticks;
				}
				LastTicks = ticks;

				string str = ticks.ToString();
				if (str.EndsWith("0000"))
					str = str.Substring(0, str.Length - 4);

				StringBuilder uid = new StringBuilder();
				uid.Append(InstanceRootUID.UID).Append('.').Append(str);
				return new DicomUID(uid.ToString(), "SOP Instance UID", DicomUidType.SOPInstance);
			}
		}

		public static DicomUID Generate(DicomUID baseUid, long nextSeq) {
			StringBuilder uid = new StringBuilder();
			uid.Append(baseUid.UID).Append('.').Append(nextSeq);
			return new DicomUID(uid.ToString(), "SOP Instance UID", DicomUidType.SOPInstance);
		}

		public static bool IsValid(DicomUID uid) {
			if (uid == null)
				return false;
			return IsValid(uid.UID);
		}
		public static bool IsValid(string uid) {
			if (String.IsNullOrEmpty(uid))
				return false;
			// only checks that the UID contains valid characters
			foreach (char c in uid) {
				if (c != '.' && !Char.IsDigit(c))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Compare UIDs for sorting in numeric order
		/// </summary>
		public static int CompareNumeric(DicomUID uid0, DicomUID uid1) {
			try {
				string[] parts0 = uid0.UID.Split('.');
				string[] parts1 = uid1.UID.Split('.');

				int count = Math.Min(parts0.Length, parts1.Length);

				for (int i = 0; i < count; i++) {
					if (parts0[i] == parts1[i])
						continue;

					int i0 = int.Parse(parts0[i]);
					int i1 = int.Parse(parts1[i]);

					if (i0 == i1)
						return 0;
					else if (i0 < i1)
						return -1;
					else
						return 1;
				}

				if (parts0.Length == parts1.Length)
					return 0;
				else if (parts0.Length < parts1.Length)
					return -1;
				else
					return 1;
			}
			catch {
				return 0;
			}
		}

		#region Static Methods
		public static Dictionary<string, DicomUID> Entries = new Dictionary<string, DicomUID>();

		static DicomUID() {
			#region Load Internal UIDs
			Entries.Add(DicomUID.VerificationSOPClass.UID, DicomUID.VerificationSOPClass);
			Entries.Add(DicomUID.ImplicitVRLittleEndian.UID, DicomUID.ImplicitVRLittleEndian);
			Entries.Add(DicomUID.ExplicitVRLittleEndian.UID, DicomUID.ExplicitVRLittleEndian);
			Entries.Add(DicomUID.DeflatedExplicitVRLittleEndian.UID, DicomUID.DeflatedExplicitVRLittleEndian);
			Entries.Add(DicomUID.ExplicitVRBigEndian.UID, DicomUID.ExplicitVRBigEndian);
			Entries.Add(DicomUID.MPEG2MainProfileMainLevel.UID, DicomUID.MPEG2MainProfileMainLevel);
			Entries.Add(DicomUID.JPEGBaselineProcess1.UID, DicomUID.JPEGBaselineProcess1);
			Entries.Add(DicomUID.JPEGExtendedProcess2_4.UID, DicomUID.JPEGExtendedProcess2_4);
			Entries.Add(DicomUID.JPEGExtendedProcess3_5RETIRED.UID, DicomUID.JPEGExtendedProcess3_5RETIRED);
			Entries.Add(DicomUID.JPEGSpectralSelectionNonHierarchicalProcess6_8RETIRED.UID, DicomUID.JPEGSpectralSelectionNonHierarchicalProcess6_8RETIRED);
			Entries.Add(DicomUID.JPEGSpectralSelectionNonHierarchicalProcess7_9RETIRED.UID, DicomUID.JPEGSpectralSelectionNonHierarchicalProcess7_9RETIRED);
			Entries.Add(DicomUID.JPEGFullProgressionNonHierarchicalProcess10_12RETIRED.UID, DicomUID.JPEGFullProgressionNonHierarchicalProcess10_12RETIRED);
			Entries.Add(DicomUID.JPEGFullProgressionNonHierarchicalProcess11_13RETIRED.UID, DicomUID.JPEGFullProgressionNonHierarchicalProcess11_13RETIRED);
			Entries.Add(DicomUID.JPEGLosslessNonHierarchicalProcess14.UID, DicomUID.JPEGLosslessNonHierarchicalProcess14);
			Entries.Add(DicomUID.JPEGLosslessNonHierarchicalProcess15RETIRED.UID, DicomUID.JPEGLosslessNonHierarchicalProcess15RETIRED);
			Entries.Add(DicomUID.JPEGExtendedHierarchicalProcess16_18RETIRED.UID, DicomUID.JPEGExtendedHierarchicalProcess16_18RETIRED);
			Entries.Add(DicomUID.JPEGExtendedHierarchicalProcess17_19RETIRED.UID, DicomUID.JPEGExtendedHierarchicalProcess17_19RETIRED);
			Entries.Add(DicomUID.JPEGSpectralSelectionHierarchicalProcess20_22RETIRED.UID, DicomUID.JPEGSpectralSelectionHierarchicalProcess20_22RETIRED);
			Entries.Add(DicomUID.JPEGSpectralSelectionHierarchicalProcess21_23RETIRED.UID, DicomUID.JPEGSpectralSelectionHierarchicalProcess21_23RETIRED);
			Entries.Add(DicomUID.JPEGFullProgressionHierarchicalProcess24_26RETIRED.UID, DicomUID.JPEGFullProgressionHierarchicalProcess24_26RETIRED);
			Entries.Add(DicomUID.JPEGFullProgressionHierarchicalProcess25_27RETIRED.UID, DicomUID.JPEGFullProgressionHierarchicalProcess25_27RETIRED);
			Entries.Add(DicomUID.JPEGLosslessHierarchicalProcess28RETIRED.UID, DicomUID.JPEGLosslessHierarchicalProcess28RETIRED);
			Entries.Add(DicomUID.JPEGLosslessHierarchicalProcess29RETIRED.UID, DicomUID.JPEGLosslessHierarchicalProcess29RETIRED);
			Entries.Add(DicomUID.JPEGLosslessProcess14SV1.UID, DicomUID.JPEGLosslessProcess14SV1);
			Entries.Add(DicomUID.JPEGLSLosslessImageCompression.UID, DicomUID.JPEGLSLosslessImageCompression);
			Entries.Add(DicomUID.JPEGLSLossyNearLosslessImageCompression.UID, DicomUID.JPEGLSLossyNearLosslessImageCompression);
			Entries.Add(DicomUID.JPEG2000ImageCompressionLosslessOnly.UID, DicomUID.JPEG2000ImageCompressionLosslessOnly);
			Entries.Add(DicomUID.JPEG2000ImageCompression.UID, DicomUID.JPEG2000ImageCompression);
			Entries.Add(DicomUID.JPEG2000Part2MulticomponentImageCompressionLosslessOnly.UID, DicomUID.JPEG2000Part2MulticomponentImageCompressionLosslessOnly);
			Entries.Add(DicomUID.JPEG2000Part2MulticomponentImageCompression.UID, DicomUID.JPEG2000Part2MulticomponentImageCompression);
			Entries.Add(DicomUID.JPIPReferenced.UID, DicomUID.JPIPReferenced);
			Entries.Add(DicomUID.JPIPReferencedDeflate.UID, DicomUID.JPIPReferencedDeflate);
			Entries.Add(DicomUID.RLELossless.UID, DicomUID.RLELossless);
			Entries.Add(DicomUID.RFC2557MIMEEncapsulation.UID, DicomUID.RFC2557MIMEEncapsulation);
			Entries.Add(DicomUID.XMLEncoding.UID, DicomUID.XMLEncoding);
			Entries.Add(DicomUID.StorageCommitmentPushModelSOPClass.UID, DicomUID.StorageCommitmentPushModelSOPClass);
			Entries.Add(DicomUID.StorageCommitmentPushModelSOPInstance.UID, DicomUID.StorageCommitmentPushModelSOPInstance);
			Entries.Add(DicomUID.StorageCommitmentPullModelSOPClassRETIRED.UID, DicomUID.StorageCommitmentPullModelSOPClassRETIRED);
			Entries.Add(DicomUID.StorageCommitmentPullModelSOPInstanceRETIRED.UID, DicomUID.StorageCommitmentPullModelSOPInstanceRETIRED);
			Entries.Add(DicomUID.MediaStorageDirectoryStorage.UID, DicomUID.MediaStorageDirectoryStorage);
			Entries.Add(DicomUID.TalairachBrainAtlasFrameOfReference.UID, DicomUID.TalairachBrainAtlasFrameOfReference);
			Entries.Add(DicomUID.SPM2GRAYFrameOfReference.UID, DicomUID.SPM2GRAYFrameOfReference);
			Entries.Add(DicomUID.SPM2WHITEFrameOfReference.UID, DicomUID.SPM2WHITEFrameOfReference);
			Entries.Add(DicomUID.SPM2CSFFrameOfReference.UID, DicomUID.SPM2CSFFrameOfReference);
			Entries.Add(DicomUID.SPM2BRAINMASKFrameOfReference.UID, DicomUID.SPM2BRAINMASKFrameOfReference);
			Entries.Add(DicomUID.SPM2AVG305T1FrameOfReference.UID, DicomUID.SPM2AVG305T1FrameOfReference);
			Entries.Add(DicomUID.SPM2AVG152T1FrameOfReference.UID, DicomUID.SPM2AVG152T1FrameOfReference);
			Entries.Add(DicomUID.SPM2AVG152T2FrameOfReference.UID, DicomUID.SPM2AVG152T2FrameOfReference);
			Entries.Add(DicomUID.SPM2AVG152PDFrameOfReference.UID, DicomUID.SPM2AVG152PDFrameOfReference);
			Entries.Add(DicomUID.SPM2SINGLESUBJT1FrameOfReference.UID, DicomUID.SPM2SINGLESUBJT1FrameOfReference);
			Entries.Add(DicomUID.SPM2T1FrameOfReference.UID, DicomUID.SPM2T1FrameOfReference);
			Entries.Add(DicomUID.SPM2T2FrameOfReference.UID, DicomUID.SPM2T2FrameOfReference);
			Entries.Add(DicomUID.SPM2PDFrameOfReference.UID, DicomUID.SPM2PDFrameOfReference);
			Entries.Add(DicomUID.SPM2EPIFrameOfReference.UID, DicomUID.SPM2EPIFrameOfReference);
			Entries.Add(DicomUID.SPM2FILT1FrameOfReference.UID, DicomUID.SPM2FILT1FrameOfReference);
			Entries.Add(DicomUID.SPM2PETFrameOfReference.UID, DicomUID.SPM2PETFrameOfReference);
			Entries.Add(DicomUID.SPM2TRANSMFrameOfReference.UID, DicomUID.SPM2TRANSMFrameOfReference);
			Entries.Add(DicomUID.SPM2SPECTFrameOfReference.UID, DicomUID.SPM2SPECTFrameOfReference);
			Entries.Add(DicomUID.ICBM452T1FrameOfReference.UID, DicomUID.ICBM452T1FrameOfReference);
			Entries.Add(DicomUID.ICBMSingleSubjectMRIFrameOfReference.UID, DicomUID.ICBMSingleSubjectMRIFrameOfReference);
			Entries.Add(DicomUID.ProceduralEventLoggingSOPClass.UID, DicomUID.ProceduralEventLoggingSOPClass);
			Entries.Add(DicomUID.ProceduralEventLoggingSOPInstance.UID, DicomUID.ProceduralEventLoggingSOPInstance);
			Entries.Add(DicomUID.SubstanceAdministrationLoggingSOPClass.UID, DicomUID.SubstanceAdministrationLoggingSOPClass);
			Entries.Add(DicomUID.SubstanceAdministrationLoggingSOPInstance.UID, DicomUID.SubstanceAdministrationLoggingSOPInstance);
			Entries.Add(DicomUID.BasicStudyContentNotificationSOPClassRETIRED.UID, DicomUID.BasicStudyContentNotificationSOPClassRETIRED);
			Entries.Add(DicomUID.LDAPDicomDeviceName.UID, DicomUID.LDAPDicomDeviceName);
			Entries.Add(DicomUID.LDAPDicomAssociationInitiator.UID, DicomUID.LDAPDicomAssociationInitiator);
			Entries.Add(DicomUID.LDAPDicomAssociationAcceptor.UID, DicomUID.LDAPDicomAssociationAcceptor);
			Entries.Add(DicomUID.LDAPDicomHostname.UID, DicomUID.LDAPDicomHostname);
			Entries.Add(DicomUID.LDAPDicomPort.UID, DicomUID.LDAPDicomPort);
			Entries.Add(DicomUID.LDAPDicomSOPClass.UID, DicomUID.LDAPDicomSOPClass);
			Entries.Add(DicomUID.LDAPDicomTransferRole.UID, DicomUID.LDAPDicomTransferRole);
			Entries.Add(DicomUID.LDAPDicomTransferSyntax.UID, DicomUID.LDAPDicomTransferSyntax);
			Entries.Add(DicomUID.LDAPDicomPrimaryDeviceType.UID, DicomUID.LDAPDicomPrimaryDeviceType);
			Entries.Add(DicomUID.LDAPDicomRelatedDeviceReference.UID, DicomUID.LDAPDicomRelatedDeviceReference);
			Entries.Add(DicomUID.LDAPDicomPreferredCalledAETitle.UID, DicomUID.LDAPDicomPreferredCalledAETitle);
			Entries.Add(DicomUID.LDAPDicomDescription.UID, DicomUID.LDAPDicomDescription);
			Entries.Add(DicomUID.LDAPDicomTLSCyphersuite.UID, DicomUID.LDAPDicomTLSCyphersuite);
			Entries.Add(DicomUID.LDAPDicomAuthorizedNodeCertificateReference.UID, DicomUID.LDAPDicomAuthorizedNodeCertificateReference);
			Entries.Add(DicomUID.LDAPDicomThisNodeCertificateReference.UID, DicomUID.LDAPDicomThisNodeCertificateReference);
			Entries.Add(DicomUID.LDAPDicomInstalled.UID, DicomUID.LDAPDicomInstalled);
			Entries.Add(DicomUID.LDAPDicomStationName.UID, DicomUID.LDAPDicomStationName);
			Entries.Add(DicomUID.LDAPDicomDeviceSerialNumber.UID, DicomUID.LDAPDicomDeviceSerialNumber);
			Entries.Add(DicomUID.LDAPDicomInstitutionName.UID, DicomUID.LDAPDicomInstitutionName);
			Entries.Add(DicomUID.LDAPDicomInstitutionAddress.UID, DicomUID.LDAPDicomInstitutionAddress);
			Entries.Add(DicomUID.LDAPDicomInstitutionDepartmentName.UID, DicomUID.LDAPDicomInstitutionDepartmentName);
			Entries.Add(DicomUID.LDAPDicomIssuerOfPatientID.UID, DicomUID.LDAPDicomIssuerOfPatientID);
			Entries.Add(DicomUID.LDAPDicomManufacturer.UID, DicomUID.LDAPDicomManufacturer);
			Entries.Add(DicomUID.LDAPDicomPreferredCallingAETitle.UID, DicomUID.LDAPDicomPreferredCallingAETitle);
			Entries.Add(DicomUID.LDAPDicomSupportedCharacterSet.UID, DicomUID.LDAPDicomSupportedCharacterSet);
			Entries.Add(DicomUID.LDAPDicomManufacturerModelName.UID, DicomUID.LDAPDicomManufacturerModelName);
			Entries.Add(DicomUID.LDAPDicomSoftwareVersion.UID, DicomUID.LDAPDicomSoftwareVersion);
			Entries.Add(DicomUID.LDAPDicomVendorData.UID, DicomUID.LDAPDicomVendorData);
			Entries.Add(DicomUID.LDAPDicomAETitle.UID, DicomUID.LDAPDicomAETitle);
			Entries.Add(DicomUID.LDAPDicomNetworkConnectionReference.UID, DicomUID.LDAPDicomNetworkConnectionReference);
			Entries.Add(DicomUID.LDAPDicomApplicationCluster.UID, DicomUID.LDAPDicomApplicationCluster);
			Entries.Add(DicomUID.LDAPDicomConfigurationRoot.UID, DicomUID.LDAPDicomConfigurationRoot);
			Entries.Add(DicomUID.LDAPDicomDevicesRoot.UID, DicomUID.LDAPDicomDevicesRoot);
			Entries.Add(DicomUID.LDAPDicomUniqueAETitlesRegistryRoot.UID, DicomUID.LDAPDicomUniqueAETitlesRegistryRoot);
			Entries.Add(DicomUID.LDAPDicomDevice.UID, DicomUID.LDAPDicomDevice);
			Entries.Add(DicomUID.LDAPDicomNetworkAE.UID, DicomUID.LDAPDicomNetworkAE);
			Entries.Add(DicomUID.LDAPDicomNetworkConnection.UID, DicomUID.LDAPDicomNetworkConnection);
			Entries.Add(DicomUID.LDAPDicomUniqueAETitle.UID, DicomUID.LDAPDicomUniqueAETitle);
			Entries.Add(DicomUID.LDAPDicomTransferCapability.UID, DicomUID.LDAPDicomTransferCapability);
			Entries.Add(DicomUID.DICOMControlledTerminology.UID, DicomUID.DICOMControlledTerminology);
			Entries.Add(DicomUID.DICOMUIDRegistry.UID, DicomUID.DICOMUIDRegistry);
			Entries.Add(DicomUID.DICOMApplicationContextName.UID, DicomUID.DICOMApplicationContextName);
			Entries.Add(DicomUID.DetachedPatientManagementSOPClassRETIRED.UID, DicomUID.DetachedPatientManagementSOPClassRETIRED);
			Entries.Add(DicomUID.DetachedPatientManagementMetaSOPClassRETIRED.UID, DicomUID.DetachedPatientManagementMetaSOPClassRETIRED);
			Entries.Add(DicomUID.DetachedVisitManagementSOPClassRETIRED.UID, DicomUID.DetachedVisitManagementSOPClassRETIRED);
			Entries.Add(DicomUID.DetachedStudyManagementSOPClassRETIRED.UID, DicomUID.DetachedStudyManagementSOPClassRETIRED);
			Entries.Add(DicomUID.StudyComponentManagementSOPClassRETIRED.UID, DicomUID.StudyComponentManagementSOPClassRETIRED);
			Entries.Add(DicomUID.ModalityPerformedProcedureStepSOPClass.UID, DicomUID.ModalityPerformedProcedureStepSOPClass);
			Entries.Add(DicomUID.ModalityPerformedProcedureStepRetrieveSOPClass.UID, DicomUID.ModalityPerformedProcedureStepRetrieveSOPClass);
			Entries.Add(DicomUID.ModalityPerformedProcedureStepNotificationSOPClass.UID, DicomUID.ModalityPerformedProcedureStepNotificationSOPClass);
			Entries.Add(DicomUID.DetachedResultsManagementSOPClassRETIRED.UID, DicomUID.DetachedResultsManagementSOPClassRETIRED);
			Entries.Add(DicomUID.DetachedResultsManagementMetaSOPClassRETIRED.UID, DicomUID.DetachedResultsManagementMetaSOPClassRETIRED);
			Entries.Add(DicomUID.DetachedStudyManagementMetaSOPClassRETIRED.UID, DicomUID.DetachedStudyManagementMetaSOPClassRETIRED);
			Entries.Add(DicomUID.DetachedInterpretationManagementSOPClassRETIRED.UID, DicomUID.DetachedInterpretationManagementSOPClassRETIRED);
			Entries.Add(DicomUID.BasicFilmSessionSOPClass.UID, DicomUID.BasicFilmSessionSOPClass);
			Entries.Add(DicomUID.PrintJobSOPClass.UID, DicomUID.PrintJobSOPClass);
			Entries.Add(DicomUID.BasicAnnotationBoxSOPClass.UID, DicomUID.BasicAnnotationBoxSOPClass);
			Entries.Add(DicomUID.PrinterSOPClass.UID, DicomUID.PrinterSOPClass);
			Entries.Add(DicomUID.PrinterConfigurationRetrievalSOPClass.UID, DicomUID.PrinterConfigurationRetrievalSOPClass);
			Entries.Add(DicomUID.PrinterSOPInstance.UID, DicomUID.PrinterSOPInstance);
			Entries.Add(DicomUID.PrinterConfigurationRetrievalSOPInstance.UID, DicomUID.PrinterConfigurationRetrievalSOPInstance);
			Entries.Add(DicomUID.BasicColorPrintManagementMetaSOPClass.UID, DicomUID.BasicColorPrintManagementMetaSOPClass);
			Entries.Add(DicomUID.ReferencedColorPrintManagementMetaSOPClassRETIRED.UID, DicomUID.ReferencedColorPrintManagementMetaSOPClassRETIRED);
			Entries.Add(DicomUID.BasicFilmBoxSOPClass.UID, DicomUID.BasicFilmBoxSOPClass);
			Entries.Add(DicomUID.VOILUTBoxSOPClass.UID, DicomUID.VOILUTBoxSOPClass);
			Entries.Add(DicomUID.PresentationLUTSOPClass.UID, DicomUID.PresentationLUTSOPClass);
			Entries.Add(DicomUID.ImageOverlayBoxSOPClassRETIRED.UID, DicomUID.ImageOverlayBoxSOPClassRETIRED);
			Entries.Add(DicomUID.BasicPrintImageOverlayBoxSOPClassRETIRED.UID, DicomUID.BasicPrintImageOverlayBoxSOPClassRETIRED);
			Entries.Add(DicomUID.PrintQueueSOPInstanceRETIRED.UID, DicomUID.PrintQueueSOPInstanceRETIRED);
			Entries.Add(DicomUID.PrintQueueManagementSOPClassRETIRED.UID, DicomUID.PrintQueueManagementSOPClassRETIRED);
			Entries.Add(DicomUID.StoredPrintStorageSOPClassRETIRED.UID, DicomUID.StoredPrintStorageSOPClassRETIRED);
			Entries.Add(DicomUID.HardcopyGrayscaleImageStorageSOPClassRETIRED.UID, DicomUID.HardcopyGrayscaleImageStorageSOPClassRETIRED);
			Entries.Add(DicomUID.HardcopyColorImageStorageSOPClassRETIRED.UID, DicomUID.HardcopyColorImageStorageSOPClassRETIRED);
			Entries.Add(DicomUID.PullPrintRequestSOPClassRETIRED.UID, DicomUID.PullPrintRequestSOPClassRETIRED);
			Entries.Add(DicomUID.PullStoredPrintManagementMetaSOPClassRETIRED.UID, DicomUID.PullStoredPrintManagementMetaSOPClassRETIRED);
			Entries.Add(DicomUID.MediaCreationManagementSOPClassUID.UID, DicomUID.MediaCreationManagementSOPClassUID);
			Entries.Add(DicomUID.BasicGrayscaleImageBoxSOPClass.UID, DicomUID.BasicGrayscaleImageBoxSOPClass);
			Entries.Add(DicomUID.BasicColorImageBoxSOPClass.UID, DicomUID.BasicColorImageBoxSOPClass);
			Entries.Add(DicomUID.ReferencedImageBoxSOPClassRETIRED.UID, DicomUID.ReferencedImageBoxSOPClassRETIRED);
			Entries.Add(DicomUID.BasicGrayscalePrintManagementMetaSOPClass.UID, DicomUID.BasicGrayscalePrintManagementMetaSOPClass);
			Entries.Add(DicomUID.ReferencedGrayscalePrintManagementMetaSOPClassRETIRED.UID, DicomUID.ReferencedGrayscalePrintManagementMetaSOPClassRETIRED);
			Entries.Add(DicomUID.ComputedRadiographyImageStorage.UID, DicomUID.ComputedRadiographyImageStorage);
			Entries.Add(DicomUID.DigitalXRayImageStorageForPresentation.UID, DicomUID.DigitalXRayImageStorageForPresentation);
			Entries.Add(DicomUID.DigitalXRayImageStorageForProcessing.UID, DicomUID.DigitalXRayImageStorageForProcessing);
			Entries.Add(DicomUID.DigitalMammographyXRayImageStorageForPresentation.UID, DicomUID.DigitalMammographyXRayImageStorageForPresentation);
			Entries.Add(DicomUID.DigitalMammographyXRayImageStorageForProcessing.UID, DicomUID.DigitalMammographyXRayImageStorageForProcessing);
			Entries.Add(DicomUID.DigitalIntraoralXRayImageStorageForPresentation.UID, DicomUID.DigitalIntraoralXRayImageStorageForPresentation);
			Entries.Add(DicomUID.DigitalIntraoralXRayImageStorageForProcessing.UID, DicomUID.DigitalIntraoralXRayImageStorageForProcessing);
			Entries.Add(DicomUID.StandaloneModalityLUTStorageRETIRED.UID, DicomUID.StandaloneModalityLUTStorageRETIRED);
			Entries.Add(DicomUID.EncapsulatedPDFStorage.UID, DicomUID.EncapsulatedPDFStorage);
			Entries.Add(DicomUID.EncapsulatedCDAStorage.UID, DicomUID.EncapsulatedCDAStorage);
			Entries.Add(DicomUID.StandaloneVOILUTStorageRETIRED.UID, DicomUID.StandaloneVOILUTStorageRETIRED);
			Entries.Add(DicomUID.GrayscaleSoftcopyPresentationStateStorageSOPClass.UID, DicomUID.GrayscaleSoftcopyPresentationStateStorageSOPClass);
			Entries.Add(DicomUID.ColorSoftcopyPresentationStateStorageSOPClass.UID, DicomUID.ColorSoftcopyPresentationStateStorageSOPClass);
			Entries.Add(DicomUID.PseudoColorSoftcopyPresentationStateStorageSOPClass.UID, DicomUID.PseudoColorSoftcopyPresentationStateStorageSOPClass);
			Entries.Add(DicomUID.BlendingSoftcopyPresentationStateStorageSOPClass.UID, DicomUID.BlendingSoftcopyPresentationStateStorageSOPClass);
			Entries.Add(DicomUID.XRayAngiographicImageStorage.UID, DicomUID.XRayAngiographicImageStorage);
			Entries.Add(DicomUID.EnhancedXAImageStorage.UID, DicomUID.EnhancedXAImageStorage);
			Entries.Add(DicomUID.XRayRadiofluoroscopicImageStorage.UID, DicomUID.XRayRadiofluoroscopicImageStorage);
			Entries.Add(DicomUID.EnhancedXRFImageStorage.UID, DicomUID.EnhancedXRFImageStorage);
			Entries.Add(DicomUID.XRayAngiographicBiPlaneImageStorageRETIRED.UID, DicomUID.XRayAngiographicBiPlaneImageStorageRETIRED);
			Entries.Add(DicomUID.PositronEmissionTomographyImageStorage.UID, DicomUID.PositronEmissionTomographyImageStorage);
			Entries.Add(DicomUID.StandalonePETCurveStorageRETIRED.UID, DicomUID.StandalonePETCurveStorageRETIRED);
			Entries.Add(DicomUID.XRay3DAngiographicImageStorage.UID, DicomUID.XRay3DAngiographicImageStorage);
			Entries.Add(DicomUID.XRay3DCraniofacialImageStorage.UID, DicomUID.XRay3DCraniofacialImageStorage);
			Entries.Add(DicomUID.CTImageStorage.UID, DicomUID.CTImageStorage);
			Entries.Add(DicomUID.EnhancedCTImageStorage.UID, DicomUID.EnhancedCTImageStorage);
			Entries.Add(DicomUID.NuclearMedicineImageStorage.UID, DicomUID.NuclearMedicineImageStorage);
			Entries.Add(DicomUID.UltrasoundMultiframeImageStorageRETIRED.UID, DicomUID.UltrasoundMultiframeImageStorageRETIRED);
			Entries.Add(DicomUID.UltrasoundMultiframeImageStorage.UID, DicomUID.UltrasoundMultiframeImageStorage);
			Entries.Add(DicomUID.MRImageStorage.UID, DicomUID.MRImageStorage);
			Entries.Add(DicomUID.EnhancedMRImageStorage.UID, DicomUID.EnhancedMRImageStorage);
			Entries.Add(DicomUID.MRSpectroscopyStorage.UID, DicomUID.MRSpectroscopyStorage);
			Entries.Add(DicomUID.RTImageStorage.UID, DicomUID.RTImageStorage);
			Entries.Add(DicomUID.RTDoseStorage.UID, DicomUID.RTDoseStorage);
			Entries.Add(DicomUID.RTStructureSetStorage.UID, DicomUID.RTStructureSetStorage);
			Entries.Add(DicomUID.RTBeamsTreatmentRecordStorage.UID, DicomUID.RTBeamsTreatmentRecordStorage);
			Entries.Add(DicomUID.RTPlanStorage.UID, DicomUID.RTPlanStorage);
			Entries.Add(DicomUID.RTBrachyTreatmentRecordStorage.UID, DicomUID.RTBrachyTreatmentRecordStorage);
			Entries.Add(DicomUID.RTTreatmentSummaryRecordStorage.UID, DicomUID.RTTreatmentSummaryRecordStorage);
			Entries.Add(DicomUID.RTIonPlanStorage.UID, DicomUID.RTIonPlanStorage);
			Entries.Add(DicomUID.RTIonBeamsTreatmentRecordStorage.UID, DicomUID.RTIonBeamsTreatmentRecordStorage);
			Entries.Add(DicomUID.NuclearMedicineImageStorageRETIRED.UID, DicomUID.NuclearMedicineImageStorageRETIRED);
			Entries.Add(DicomUID.UltrasoundImageStorageRETIRED.UID, DicomUID.UltrasoundImageStorageRETIRED);
			Entries.Add(DicomUID.UltrasoundImageStorage.UID, DicomUID.UltrasoundImageStorage);
			Entries.Add(DicomUID.RawDataStorage.UID, DicomUID.RawDataStorage);
			Entries.Add(DicomUID.SpatialRegistrationStorage.UID, DicomUID.SpatialRegistrationStorage);
			Entries.Add(DicomUID.SpatialFiducialsStorage.UID, DicomUID.SpatialFiducialsStorage);
			Entries.Add(DicomUID.DeformableSpatialRegistrationStorage.UID, DicomUID.DeformableSpatialRegistrationStorage);
			Entries.Add(DicomUID.SegmentationStorage.UID, DicomUID.SegmentationStorage);
			Entries.Add(DicomUID.RealWorldValueMappingStorage.UID, DicomUID.RealWorldValueMappingStorage);
			Entries.Add(DicomUID.SecondaryCaptureImageStorage.UID, DicomUID.SecondaryCaptureImageStorage);
			Entries.Add(DicomUID.MultiframeSingleBitSecondaryCaptureImageStorage.UID, DicomUID.MultiframeSingleBitSecondaryCaptureImageStorage);
			Entries.Add(DicomUID.MultiframeGrayscaleByteSecondaryCaptureImageStorage.UID, DicomUID.MultiframeGrayscaleByteSecondaryCaptureImageStorage);
			Entries.Add(DicomUID.MultiframeGrayscaleWordSecondaryCaptureImageStorage.UID, DicomUID.MultiframeGrayscaleWordSecondaryCaptureImageStorage);
			Entries.Add(DicomUID.MultiframeTrueColorSecondaryCaptureImageStorage.UID, DicomUID.MultiframeTrueColorSecondaryCaptureImageStorage);
			Entries.Add(DicomUID.VLImageStorageTrialRETIRED.UID, DicomUID.VLImageStorageTrialRETIRED);
			Entries.Add(DicomUID.VLEndoscopicImageStorage.UID, DicomUID.VLEndoscopicImageStorage);
			Entries.Add(DicomUID.VideoEndoscopicImageStorage.UID, DicomUID.VideoEndoscopicImageStorage);
			Entries.Add(DicomUID.VLMicroscopicImageStorage.UID, DicomUID.VLMicroscopicImageStorage);
			Entries.Add(DicomUID.VideoMicroscopicImageStorage.UID, DicomUID.VideoMicroscopicImageStorage);
			Entries.Add(DicomUID.VLSlideCoordinatesMicroscopicImageStorage.UID, DicomUID.VLSlideCoordinatesMicroscopicImageStorage);
			Entries.Add(DicomUID.VLPhotographicImageStorage.UID, DicomUID.VLPhotographicImageStorage);
			Entries.Add(DicomUID.VideoPhotographicImageStorage.UID, DicomUID.VideoPhotographicImageStorage);
			Entries.Add(DicomUID.OphthalmicPhotography8BitImageStorage.UID, DicomUID.OphthalmicPhotography8BitImageStorage);
			Entries.Add(DicomUID.OphthalmicPhotography16BitImageStorage.UID, DicomUID.OphthalmicPhotography16BitImageStorage);
			Entries.Add(DicomUID.StereometricRelationshipStorage.UID, DicomUID.StereometricRelationshipStorage);
			Entries.Add(DicomUID.OphthalmicTomographyImageStorage.UID, DicomUID.OphthalmicTomographyImageStorage);
			Entries.Add(DicomUID.VLMultiframeImageStorageTrialRETIRED.UID, DicomUID.VLMultiframeImageStorageTrialRETIRED);
			Entries.Add(DicomUID.StandaloneOverlayStorageRETIRED.UID, DicomUID.StandaloneOverlayStorageRETIRED);
			Entries.Add(DicomUID.TextSRStorageTrialRETIRED.UID, DicomUID.TextSRStorageTrialRETIRED);
			Entries.Add(DicomUID.BasicTextSRStorage.UID, DicomUID.BasicTextSRStorage);
			Entries.Add(DicomUID.AudioSRStorageTrialRETIRED.UID, DicomUID.AudioSRStorageTrialRETIRED);
			Entries.Add(DicomUID.EnhancedSRStorage.UID, DicomUID.EnhancedSRStorage);
			Entries.Add(DicomUID.DetailSRStorageTrialRETIRED.UID, DicomUID.DetailSRStorageTrialRETIRED);
			Entries.Add(DicomUID.ComprehensiveSRStorage.UID, DicomUID.ComprehensiveSRStorage);
			Entries.Add(DicomUID.ComprehensiveSRStorageTrialRETIRED.UID, DicomUID.ComprehensiveSRStorageTrialRETIRED);
			Entries.Add(DicomUID.ProcedureLogStorage.UID, DicomUID.ProcedureLogStorage);
			Entries.Add(DicomUID.MammographyCADSRStorage.UID, DicomUID.MammographyCADSRStorage);
			Entries.Add(DicomUID.KeyObjectSelectionDocumentStorage.UID, DicomUID.KeyObjectSelectionDocumentStorage);
			Entries.Add(DicomUID.ChestCADSRStorage.UID, DicomUID.ChestCADSRStorage);
			Entries.Add(DicomUID.XRayRadiationDoseSRStorage.UID, DicomUID.XRayRadiationDoseSRStorage);
			Entries.Add(DicomUID.StandaloneCurveStorageRETIRED.UID, DicomUID.StandaloneCurveStorageRETIRED);
			Entries.Add(DicomUID.WaveformStorageTrialRETIRED.UID, DicomUID.WaveformStorageTrialRETIRED);
			Entries.Add(DicomUID.TwelveLeadECGWaveformStorage.UID, DicomUID.TwelveLeadECGWaveformStorage);
			Entries.Add(DicomUID.GeneralECGWaveformStorage.UID, DicomUID.GeneralECGWaveformStorage);
			Entries.Add(DicomUID.AmbulatoryECGWaveformStorage.UID, DicomUID.AmbulatoryECGWaveformStorage);
			Entries.Add(DicomUID.HemodynamicWaveformStorage.UID, DicomUID.HemodynamicWaveformStorage);
			Entries.Add(DicomUID.CardiacElectrophysiologyWaveformStorage.UID, DicomUID.CardiacElectrophysiologyWaveformStorage);
			Entries.Add(DicomUID.BasicVoiceAudioWaveformStorage.UID, DicomUID.BasicVoiceAudioWaveformStorage);
			Entries.Add(DicomUID.PatientRootQueryRetrieveInformationModelFIND.UID, DicomUID.PatientRootQueryRetrieveInformationModelFIND);
			Entries.Add(DicomUID.PatientRootQueryRetrieveInformationModelMOVE.UID, DicomUID.PatientRootQueryRetrieveInformationModelMOVE);
			Entries.Add(DicomUID.PatientRootQueryRetrieveInformationModelGET.UID, DicomUID.PatientRootQueryRetrieveInformationModelGET);
			Entries.Add(DicomUID.StudyRootQueryRetrieveInformationModelFIND.UID, DicomUID.StudyRootQueryRetrieveInformationModelFIND);
			Entries.Add(DicomUID.StudyRootQueryRetrieveInformationModelMOVE.UID, DicomUID.StudyRootQueryRetrieveInformationModelMOVE);
			Entries.Add(DicomUID.StudyRootQueryRetrieveInformationModelGET.UID, DicomUID.StudyRootQueryRetrieveInformationModelGET);
			Entries.Add(DicomUID.PatientStudyOnlyQueryRetrieveInformationModelFINDRETIRED.UID, DicomUID.PatientStudyOnlyQueryRetrieveInformationModelFINDRETIRED);
			Entries.Add(DicomUID.PatientStudyOnlyQueryRetrieveInformationModelMOVERETIRED.UID, DicomUID.PatientStudyOnlyQueryRetrieveInformationModelMOVERETIRED);
			Entries.Add(DicomUID.PatientStudyOnlyQueryRetrieveInformationModelGETRETIRED.UID, DicomUID.PatientStudyOnlyQueryRetrieveInformationModelGETRETIRED);
			Entries.Add(DicomUID.ModalityWorklistInformationModelFIND.UID, DicomUID.ModalityWorklistInformationModelFIND);
			Entries.Add(DicomUID.GeneralPurposeWorklistManagementMetaSOPClass.UID, DicomUID.GeneralPurposeWorklistManagementMetaSOPClass);
			Entries.Add(DicomUID.GeneralPurposeWorklistInformationModelFIND.UID, DicomUID.GeneralPurposeWorklistInformationModelFIND);
			Entries.Add(DicomUID.GeneralPurposeScheduledProcedureStepSOPClass.UID, DicomUID.GeneralPurposeScheduledProcedureStepSOPClass);
			Entries.Add(DicomUID.GeneralPurposePerformedProcedureStepSOPClass.UID, DicomUID.GeneralPurposePerformedProcedureStepSOPClass);
			Entries.Add(DicomUID.InstanceAvailabilityNotificationSOPClass.UID, DicomUID.InstanceAvailabilityNotificationSOPClass);
			Entries.Add(DicomUID.RTBeamsDeliveryInstructionStorageSupplement74FrozenDraft.UID, DicomUID.RTBeamsDeliveryInstructionStorageSupplement74FrozenDraft);
			Entries.Add(DicomUID.RTConventionalMachineVerificationSupplement74FrozenDraft.UID, DicomUID.RTConventionalMachineVerificationSupplement74FrozenDraft);
			Entries.Add(DicomUID.RTIonMachineVerificationSupplement74FrozenDraft.UID, DicomUID.RTIonMachineVerificationSupplement74FrozenDraft);
			Entries.Add(DicomUID.UnifiedWorklistAndProcedureStepSOPClass.UID, DicomUID.UnifiedWorklistAndProcedureStepSOPClass);
			Entries.Add(DicomUID.UnifiedProcedureStepPushSOPClass.UID, DicomUID.UnifiedProcedureStepPushSOPClass);
			Entries.Add(DicomUID.UnifiedProcedureStepWatchSOPClass.UID, DicomUID.UnifiedProcedureStepWatchSOPClass);
			Entries.Add(DicomUID.UnifiedProcedureStepPullSOPClass.UID, DicomUID.UnifiedProcedureStepPullSOPClass);
			Entries.Add(DicomUID.UnifiedProcedureStepEventSOPClass.UID, DicomUID.UnifiedProcedureStepEventSOPClass);
			Entries.Add(DicomUID.UnifiedWorklistAndProcedureStepSOPInstance.UID, DicomUID.UnifiedWorklistAndProcedureStepSOPInstance);
			Entries.Add(DicomUID.GeneralRelevantPatientInformationQuery.UID, DicomUID.GeneralRelevantPatientInformationQuery);
			Entries.Add(DicomUID.BreastImagingRelevantPatientInformationQuery.UID, DicomUID.BreastImagingRelevantPatientInformationQuery);
			Entries.Add(DicomUID.CardiacRelevantPatientInformationQuery.UID, DicomUID.CardiacRelevantPatientInformationQuery);
			Entries.Add(DicomUID.HangingProtocolStorage.UID, DicomUID.HangingProtocolStorage);
			Entries.Add(DicomUID.HangingProtocolInformationModelFIND.UID, DicomUID.HangingProtocolInformationModelFIND);
			Entries.Add(DicomUID.HangingProtocolInformationModelMOVE.UID, DicomUID.HangingProtocolInformationModelMOVE);
			Entries.Add(DicomUID.ProductCharacteristicsQuerySOPClass.UID, DicomUID.ProductCharacteristicsQuerySOPClass);
			Entries.Add(DicomUID.SubstanceApprovalQuerySOPClass.UID, DicomUID.SubstanceApprovalQuerySOPClass);

			#endregion
		}

		public static DicomUID Lookup(string uid) {
			DicomUID o = null;
			Entries.TryGetValue(uid, out o);
			if (o == null) {
				o = new DicomUID(uid, "Unknown UID", DicomUidType.Unknown);
			}
			return o;
		}

		public static bool IsImageStorage(DicomUID uid) {
			return GetStorageCategory(uid) == DicomStorageCategory.Image;
		}

		public static DicomStorageCategory GetStorageCategory(DicomUID uid) {
			if (uid.Type != DicomUidType.SOPClass || !uid.Description.Contains("Storage"))
				return DicomStorageCategory.None;

			if (uid.Description.Contains("Image Storage"))
				return DicomStorageCategory.Image;

			if (uid == DicomUID.BlendingSoftcopyPresentationStateStorageSOPClass ||
				uid == DicomUID.ColorSoftcopyPresentationStateStorageSOPClass ||
				uid == DicomUID.GrayscaleSoftcopyPresentationStateStorageSOPClass ||
				uid == DicomUID.PseudoColorSoftcopyPresentationStateStorageSOPClass)
				return DicomStorageCategory.PresentationState;

			if (uid == DicomUID.AudioSRStorageTrialRETIRED ||
				uid == DicomUID.BasicTextSRStorage ||
				uid == DicomUID.ChestCADSRStorage ||
				uid == DicomUID.ComprehensiveSRStorage ||
				uid == DicomUID.ComprehensiveSRStorageTrialRETIRED ||
				uid == DicomUID.DetailSRStorageTrialRETIRED ||
				uid == DicomUID.EnhancedSRStorage ||
				uid == DicomUID.MammographyCADSRStorage ||
				uid == DicomUID.TextSRStorageTrialRETIRED ||
				uid == DicomUID.XRayRadiationDoseSRStorage)
				return DicomStorageCategory.StructuredReport;

			if (uid == DicomUID.AmbulatoryECGWaveformStorage ||
				uid == DicomUID.BasicVoiceAudioWaveformStorage ||
				uid == DicomUID.CardiacElectrophysiologyWaveformStorage ||
				uid == DicomUID.GeneralECGWaveformStorage ||
				uid == DicomUID.HemodynamicWaveformStorage ||
				uid == DicomUID.TwelveLeadECGWaveformStorage ||
				uid == DicomUID.WaveformStorageTrialRETIRED)
				return DicomStorageCategory.Waveform;

			if (uid == DicomUID.EncapsulatedCDAStorage ||
				uid == DicomUID.EncapsulatedPDFStorage)
				return DicomStorageCategory.Document;

			if (uid == DicomUID.RawDataStorage)
				return DicomStorageCategory.Raw;

			return DicomStorageCategory.Other;
		}
		#endregion

		#region Dicom UIDs
		/// <summary>SOP Class: Verification SOP Class [PS 3.4]</summary>
		public static DicomUID VerificationSOPClass = new DicomUID("1.2.840.10008.1.1", "Verification SOP Class", DicomUidType.SOPClass);

		/// <summary>Transfer Syntax: Implicit VR Little Endian: Default Transfer Syntax for DICOM [PS 3.5]</summary>
		public static DicomUID ImplicitVRLittleEndian = new DicomUID("1.2.840.10008.1.2", "Implicit VR Little Endian", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: Explicit VR Little Endian [PS 3.5]</summary>
		public static DicomUID ExplicitVRLittleEndian = new DicomUID("1.2.840.10008.1.2.1", "Explicit VR Little Endian", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: Deflated Explicit VR Little Endian [PS 3.5]</summary>
		public static DicomUID DeflatedExplicitVRLittleEndian = new DicomUID("1.2.840.10008.1.2.1.99", "Deflated Explicit VR Little Endian", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: Explicit VR Big Endian [PS 3.5]</summary>
		public static DicomUID ExplicitVRBigEndian = new DicomUID("1.2.840.10008.1.2.2", "Explicit VR Big Endian", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: MPEG2 Main Profile @ Main Level [PS 3.5]</summary>
		public static DicomUID MPEG2MainProfileMainLevel = new DicomUID("1.2.840.10008.1.2.4.100", "MPEG2 Main Profile @ Main Level", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Baseline (Process 1): Default Transfer Syntax for Lossy JPEG 8 Bit Image Compression [PS 3.5]</summary>
		public static DicomUID JPEGBaselineProcess1 = new DicomUID("1.2.840.10008.1.2.4.50", "JPEG Baseline (Process 1)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Extended (Process 2 &amp; 4): Default Transfer Syntax for Lossy JPEG 12 Bit Image Compression (Process 4 only) [PS 3.5]</summary>
		public static DicomUID JPEGExtendedProcess2_4 = new DicomUID("1.2.840.10008.1.2.4.51", "JPEG Extended (Process 2 & 4)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Extended (Process 3 &amp; 5) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGExtendedProcess3_5RETIRED = new DicomUID("1.2.840.10008.1.2.4.52", "JPEG Extended (Process 3 & 5)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Spectral Selection, Non-Hierarchical (Process 6 &amp; 8) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGSpectralSelectionNonHierarchicalProcess6_8RETIRED = new DicomUID("1.2.840.10008.1.2.4.53", "JPEG Spectral Selection, Non-Hierarchical (Process 6 & 8)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Spectral Selection, Non-Hierarchical (Process 7 &amp; 9) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGSpectralSelectionNonHierarchicalProcess7_9RETIRED = new DicomUID("1.2.840.10008.1.2.4.54", "JPEG Spectral Selection, Non-Hierarchical (Process 7 & 9)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Full Progression, Non-Hierarchical (Process 10 &amp; 12) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGFullProgressionNonHierarchicalProcess10_12RETIRED = new DicomUID("1.2.840.10008.1.2.4.55", "JPEG Full Progression, Non-Hierarchical (Process 10 & 12)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Full Progression, Non-Hierarchical (Process 11 &amp; 13) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGFullProgressionNonHierarchicalProcess11_13RETIRED = new DicomUID("1.2.840.10008.1.2.4.56", "JPEG Full Progression, Non-Hierarchical (Process 11 & 13)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Lossless, Non-Hierarchical (Process 14) [PS 3.5]</summary>
		public static DicomUID JPEGLosslessNonHierarchicalProcess14 = new DicomUID("1.2.840.10008.1.2.4.57", "JPEG Lossless, Non-Hierarchical (Process 14)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Lossless, Non-Hierarchical (Process 15) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGLosslessNonHierarchicalProcess15RETIRED = new DicomUID("1.2.840.10008.1.2.4.58", "JPEG Lossless, Non-Hierarchical (Process 15)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Extended, Hierarchical (Process 16 &amp; 18) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGExtendedHierarchicalProcess16_18RETIRED = new DicomUID("1.2.840.10008.1.2.4.59", "JPEG Extended, Hierarchical (Process 16 & 18)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Extended, Hierarchical (Process 17 &amp; 19) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGExtendedHierarchicalProcess17_19RETIRED = new DicomUID("1.2.840.10008.1.2.4.60", "JPEG Extended, Hierarchical (Process 17 & 19)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Spectral Selection, Hierarchical (Process 20 &amp; 22) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGSpectralSelectionHierarchicalProcess20_22RETIRED = new DicomUID("1.2.840.10008.1.2.4.61", "JPEG Spectral Selection, Hierarchical (Process 20 & 22)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Spectral Selection, Hierarchical (Process 21 &amp; 23) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGSpectralSelectionHierarchicalProcess21_23RETIRED = new DicomUID("1.2.840.10008.1.2.4.62", "JPEG Spectral Selection, Hierarchical (Process 21 & 23)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Full Progression, Hierarchical (Process 24 &amp; 26) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGFullProgressionHierarchicalProcess24_26RETIRED = new DicomUID("1.2.840.10008.1.2.4.63", "JPEG Full Progression, Hierarchical (Process 24 & 26)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Full Progression, Hierarchical (Process 25 &amp; 27) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGFullProgressionHierarchicalProcess25_27RETIRED = new DicomUID("1.2.840.10008.1.2.4.64", "JPEG Full Progression, Hierarchical (Process 25 & 27)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Lossless, Hierarchical (Process 28) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGLosslessHierarchicalProcess28RETIRED = new DicomUID("1.2.840.10008.1.2.4.65", "JPEG Lossless, Hierarchical (Process 28)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Lossless, Hierarchical (Process 29) [PS 3.5] (Retired)</summary>
		public static DicomUID JPEGLosslessHierarchicalProcess29RETIRED = new DicomUID("1.2.840.10008.1.2.4.66", "JPEG Lossless, Hierarchical (Process 29)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG Lossless, Non-Hierarchical, First-Order Prediction (Process 14 [Selection Value 1]): Default Transfer Syntax for Lossless JPEG Image Compression [PS 3.5]</summary>
		public static DicomUID JPEGLosslessProcess14SV1 = new DicomUID("1.2.840.10008.1.2.4.70", "JPEG Lossless, Non-Hierarchical, First-Order Prediction (Process 14 [Selection Value 1])", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG-LS Lossless Image Compression [PS 3.5]</summary>
		public static DicomUID JPEGLSLosslessImageCompression = new DicomUID("1.2.840.10008.1.2.4.80", "JPEG-LS Lossless Image Compression", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG-LS Lossy (Near-Lossless) Image Compression [PS 3.5]</summary>
		public static DicomUID JPEGLSLossyNearLosslessImageCompression = new DicomUID("1.2.840.10008.1.2.4.81", "JPEG-LS Lossy (Near-Lossless) Image Compression", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG 2000 Image Compression (Lossless Only) [PS 3.5]</summary>
		public static DicomUID JPEG2000ImageCompressionLosslessOnly = new DicomUID("1.2.840.10008.1.2.4.90", "JPEG 2000 Image Compression (Lossless Only)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG 2000 Image Compression [PS 3.5]</summary>
		public static DicomUID JPEG2000ImageCompression = new DicomUID("1.2.840.10008.1.2.4.91", "JPEG 2000 Image Compression", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG 2000 Part 2 Multi-component Image Compression (Lossless Only) [PS 3.5]</summary>
		public static DicomUID JPEG2000Part2MulticomponentImageCompressionLosslessOnly = new DicomUID("1.2.840.10008.1.2.4.92", "JPEG 2000 Part 2 Multi-component Image Compression (Lossless Only)", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPEG 2000 Part 2 Multi-component Image Compression [PS 3.5]</summary>
		public static DicomUID JPEG2000Part2MulticomponentImageCompression = new DicomUID("1.2.840.10008.1.2.4.93", "JPEG 2000 Part 2 Multi-component Image Compression", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPIP Referenced [PS 3.5]</summary>
		public static DicomUID JPIPReferenced = new DicomUID("1.2.840.10008.1.2.4.94", "JPIP Referenced", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: JPIP Referenced Deflate [PS 3.5]</summary>
		public static DicomUID JPIPReferencedDeflate = new DicomUID("1.2.840.10008.1.2.4.95", "JPIP Referenced Deflate", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: RLE Lossless [PS 3.5]</summary>
		public static DicomUID RLELossless = new DicomUID("1.2.840.10008.1.2.5", "RLE Lossless", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: RFC 2557 MIME encapsulation [PS 3.10]</summary>
		public static DicomUID RFC2557MIMEEncapsulation = new DicomUID("1.2.840.10008.1.2.6.1", "RFC 2557 MIME encapsulation", DicomUidType.TransferSyntax);

		/// <summary>Transfer Syntax: XML Encoding [PS 3.10]</summary>
		public static DicomUID XMLEncoding = new DicomUID("1.2.840.10008.1.2.6.2", "XML Encoding", DicomUidType.TransferSyntax);

		/// <summary>SOP Class: Storage Commitment Push Model SOP Class [PS 3.4]</summary>
		public static DicomUID StorageCommitmentPushModelSOPClass = new DicomUID("1.2.840.10008.1.20.1", "Storage Commitment Push Model SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known SOP Instance: Storage Commitment Push Model SOP Instance [PS 3.4]</summary>
		public static DicomUID StorageCommitmentPushModelSOPInstance = new DicomUID("1.2.840.10008.1.20.1.1", "Storage Commitment Push Model SOP Instance", DicomUidType.SOPInstance);

		/// <summary>SOP Class: Storage Commitment Pull Model SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID StorageCommitmentPullModelSOPClassRETIRED = new DicomUID("1.2.840.10008.1.20.2", "Storage Commitment Pull Model SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known SOP Instance: Storage Commitment Pull Model SOP Instance [PS 3.4] (Retired)</summary>
		public static DicomUID StorageCommitmentPullModelSOPInstanceRETIRED = new DicomUID("1.2.840.10008.1.20.2.1", "Storage Commitment Pull Model SOP Instance", DicomUidType.SOPInstance);

		/// <summary>SOP Class: Media Storage Directory Storage [PS 3.4]</summary>
		public static DicomUID MediaStorageDirectoryStorage = new DicomUID("1.2.840.10008.1.3.10", "Media Storage Directory Storage", DicomUidType.SOPClass);

		/// <summary>Well-known frame of reference: Talairach Brain Atlas Frame of Reference []</summary>
		public static DicomUID TalairachBrainAtlasFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.1", "Talairach Brain Atlas Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 GRAY Frame of Reference []</summary>
		public static DicomUID SPM2GRAYFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.10", "SPM2 GRAY Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 WHITE Frame of Reference []</summary>
		public static DicomUID SPM2WHITEFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.11", "SPM2 WHITE Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 CSF Frame of Reference []</summary>
		public static DicomUID SPM2CSFFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.12", "SPM2 CSF Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 BRAINMASK Frame of Reference []</summary>
		public static DicomUID SPM2BRAINMASKFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.13", "SPM2 BRAINMASK Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 AVG305T1 Frame of Reference []</summary>
		public static DicomUID SPM2AVG305T1FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.14", "SPM2 AVG305T1 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 AVG152T1 Frame of Reference []</summary>
		public static DicomUID SPM2AVG152T1FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.15", "SPM2 AVG152T1 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 AVG152T2 Frame of Reference []</summary>
		public static DicomUID SPM2AVG152T2FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.16", "SPM2 AVG152T2 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 AVG152PD Frame of Reference []</summary>
		public static DicomUID SPM2AVG152PDFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.17", "SPM2 AVG152PD Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 SINGLESUBJT1 Frame of Reference []</summary>
		public static DicomUID SPM2SINGLESUBJT1FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.18", "SPM2 SINGLESUBJT1 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 T1 Frame of Reference []</summary>
		public static DicomUID SPM2T1FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.2", "SPM2 T1 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 T2 Frame of Reference []</summary>
		public static DicomUID SPM2T2FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.3", "SPM2 T2 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 PD Frame of Reference []</summary>
		public static DicomUID SPM2PDFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.4", "SPM2 PD Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 EPI Frame of Reference []</summary>
		public static DicomUID SPM2EPIFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.5", "SPM2 EPI Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 FIL T1 Frame of Reference []</summary>
		public static DicomUID SPM2FILT1FrameOfReference = new DicomUID("1.2.840.10008.1.4.1.6", "SPM2 FIL T1 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 PET Frame of Reference []</summary>
		public static DicomUID SPM2PETFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.7", "SPM2 PET Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 TRANSM Frame of Reference []</summary>
		public static DicomUID SPM2TRANSMFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.8", "SPM2 TRANSM Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: SPM2 SPECT Frame of Reference []</summary>
		public static DicomUID SPM2SPECTFrameOfReference = new DicomUID("1.2.840.10008.1.4.1.9", "SPM2 SPECT Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: ICBM 452 T1 Frame of Reference []</summary>
		public static DicomUID ICBM452T1FrameOfReference = new DicomUID("1.2.840.10008.1.4.2.1", "ICBM 452 T1 Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>Well-known frame of reference: ICBM Single Subject MRI Frame of Reference []</summary>
		public static DicomUID ICBMSingleSubjectMRIFrameOfReference = new DicomUID("1.2.840.10008.1.4.2.2", "ICBM Single Subject MRI Frame of Reference", DicomUidType.FrameOfReference);

		/// <summary>SOP Class: Procedural Event Logging SOP Class [PS 3.4]</summary>
		public static DicomUID ProceduralEventLoggingSOPClass = new DicomUID("1.2.840.10008.1.40", "Procedural Event Logging SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known SOP Instance: Procedural Event Logging SOP Instance [PS 3.4]</summary>
		public static DicomUID ProceduralEventLoggingSOPInstance = new DicomUID("1.2.840.10008.1.40.1", "Procedural Event Logging SOP Instance", DicomUidType.SOPInstance);

		/// <summary>SOP Class: Substance Administration Logging SOP Class [PS 3.4]</summary>
		public static DicomUID SubstanceAdministrationLoggingSOPClass = new DicomUID("1.2.840.10008.1.42", "Substance Administration Logging SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known SOP Instance: Substance Administration Logging SOP Instance [PS 3.4]</summary>
		public static DicomUID SubstanceAdministrationLoggingSOPInstance = new DicomUID("1.2.840.10008.1.42.1", "Substance Administration Logging SOP Instance", DicomUidType.SOPInstance);

		/// <summary>SOP Class: Basic Study Content Notification SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID BasicStudyContentNotificationSOPClassRETIRED = new DicomUID("1.2.840.10008.1.9", "Basic Study Content Notification SOP Class", DicomUidType.SOPClass);

		/// <summary>LDAP OID: dicomDeviceName [PS 3.15]</summary>
		public static DicomUID LDAPDicomDeviceName = new DicomUID("1.2.840.10008.15.0.3.1", "dicomDeviceName", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomAssociationInitiator [PS 3.15]</summary>
		public static DicomUID LDAPDicomAssociationInitiator = new DicomUID("1.2.840.10008.15.0.3.10", "dicomAssociationInitiator", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomAssociationAcceptor [PS 3.15]</summary>
		public static DicomUID LDAPDicomAssociationAcceptor = new DicomUID("1.2.840.10008.15.0.3.11", "dicomAssociationAcceptor", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomHostname [PS 3.15]</summary>
		public static DicomUID LDAPDicomHostname = new DicomUID("1.2.840.10008.15.0.3.12", "dicomHostname", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomPort [PS 3.15]</summary>
		public static DicomUID LDAPDicomPort = new DicomUID("1.2.840.10008.15.0.3.13", "dicomPort", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomSOPClass [PS 3.15]</summary>
		public static DicomUID LDAPDicomSOPClass = new DicomUID("1.2.840.10008.15.0.3.14", "dicomSOPClass", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomTransferRole [PS 3.15]</summary>
		public static DicomUID LDAPDicomTransferRole = new DicomUID("1.2.840.10008.15.0.3.15", "dicomTransferRole", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomTransferSyntax [PS 3.15]</summary>
		public static DicomUID LDAPDicomTransferSyntax = new DicomUID("1.2.840.10008.15.0.3.16", "dicomTransferSyntax", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomPrimaryDeviceType [PS 3.15]</summary>
		public static DicomUID LDAPDicomPrimaryDeviceType = new DicomUID("1.2.840.10008.15.0.3.17", "dicomPrimaryDeviceType", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomRelatedDeviceReference [PS 3.15]</summary>
		public static DicomUID LDAPDicomRelatedDeviceReference = new DicomUID("1.2.840.10008.15.0.3.18", "dicomRelatedDeviceReference", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomPreferredCalledAETitle [PS 3.15]</summary>
		public static DicomUID LDAPDicomPreferredCalledAETitle = new DicomUID("1.2.840.10008.15.0.3.19", "dicomPreferredCalledAETitle", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomDescription [PS 3.15]</summary>
		public static DicomUID LDAPDicomDescription = new DicomUID("1.2.840.10008.15.0.3.2", "dicomDescription", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomTLSCyphersuite [PS 3.15]</summary>
		public static DicomUID LDAPDicomTLSCyphersuite = new DicomUID("1.2.840.10008.15.0.3.20", "dicomTLSCyphersuite", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomAuthorizedNodeCertificateReference [PS 3.15]</summary>
		public static DicomUID LDAPDicomAuthorizedNodeCertificateReference = new DicomUID("1.2.840.10008.15.0.3.21", "dicomAuthorizedNodeCertificateReference", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomThisNodeCertificateReference [PS 3.15]</summary>
		public static DicomUID LDAPDicomThisNodeCertificateReference = new DicomUID("1.2.840.10008.15.0.3.22", "dicomThisNodeCertificateReference", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomInstalled [PS 3.15]</summary>
		public static DicomUID LDAPDicomInstalled = new DicomUID("1.2.840.10008.15.0.3.23", "dicomInstalled", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomStationName [PS 3.15]</summary>
		public static DicomUID LDAPDicomStationName = new DicomUID("1.2.840.10008.15.0.3.24", "dicomStationName", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomDeviceSerialNumber [PS 3.15]</summary>
		public static DicomUID LDAPDicomDeviceSerialNumber = new DicomUID("1.2.840.10008.15.0.3.25", "dicomDeviceSerialNumber", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomInstitutionName [PS 3.15]</summary>
		public static DicomUID LDAPDicomInstitutionName = new DicomUID("1.2.840.10008.15.0.3.26", "dicomInstitutionName", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomInstitutionAddress [PS 3.15]</summary>
		public static DicomUID LDAPDicomInstitutionAddress = new DicomUID("1.2.840.10008.15.0.3.27", "dicomInstitutionAddress", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomInstitutionDepartmentName [PS 3.15]</summary>
		public static DicomUID LDAPDicomInstitutionDepartmentName = new DicomUID("1.2.840.10008.15.0.3.28", "dicomInstitutionDepartmentName", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomIssuerOfPatientID [PS 3.15]</summary>
		public static DicomUID LDAPDicomIssuerOfPatientID = new DicomUID("1.2.840.10008.15.0.3.29", "dicomIssuerOfPatientID", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomManufacturer [PS 3.15]</summary>
		public static DicomUID LDAPDicomManufacturer = new DicomUID("1.2.840.10008.15.0.3.3", "dicomManufacturer", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomPreferredCallingAETitle [PS 3.15]</summary>
		public static DicomUID LDAPDicomPreferredCallingAETitle = new DicomUID("1.2.840.10008.15.0.3.30", "dicomPreferredCallingAETitle", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomSupportedCharacterSet [PS 3.15]</summary>
		public static DicomUID LDAPDicomSupportedCharacterSet = new DicomUID("1.2.840.10008.15.0.3.31", "dicomSupportedCharacterSet", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomManufacturerModelName [PS 3.15]</summary>
		public static DicomUID LDAPDicomManufacturerModelName = new DicomUID("1.2.840.10008.15.0.3.4", "dicomManufacturerModelName", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomSoftwareVersion [PS 3.15]</summary>
		public static DicomUID LDAPDicomSoftwareVersion = new DicomUID("1.2.840.10008.15.0.3.5", "dicomSoftwareVersion", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomVendorData [PS 3.15]</summary>
		public static DicomUID LDAPDicomVendorData = new DicomUID("1.2.840.10008.15.0.3.6", "dicomVendorData", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomAETitle [PS 3.15]</summary>
		public static DicomUID LDAPDicomAETitle = new DicomUID("1.2.840.10008.15.0.3.7", "dicomAETitle", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomNetworkConnectionReference [PS 3.15]</summary>
		public static DicomUID LDAPDicomNetworkConnectionReference = new DicomUID("1.2.840.10008.15.0.3.8", "dicomNetworkConnectionReference", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomApplicationCluster [PS 3.15]</summary>
		public static DicomUID LDAPDicomApplicationCluster = new DicomUID("1.2.840.10008.15.0.3.9", "dicomApplicationCluster", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomConfigurationRoot [PS 3.15]</summary>
		public static DicomUID LDAPDicomConfigurationRoot = new DicomUID("1.2.840.10008.15.0.4.1", "dicomConfigurationRoot", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomDevicesRoot [PS 3.15]</summary>
		public static DicomUID LDAPDicomDevicesRoot = new DicomUID("1.2.840.10008.15.0.4.2", "dicomDevicesRoot", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomUniqueAETitlesRegistryRoot [PS 3.15]</summary>
		public static DicomUID LDAPDicomUniqueAETitlesRegistryRoot = new DicomUID("1.2.840.10008.15.0.4.3", "dicomUniqueAETitlesRegistryRoot", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomDevice [PS 3.15]</summary>
		public static DicomUID LDAPDicomDevice = new DicomUID("1.2.840.10008.15.0.4.4", "dicomDevice", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomNetworkAE [PS 3.15]</summary>
		public static DicomUID LDAPDicomNetworkAE = new DicomUID("1.2.840.10008.15.0.4.5", "dicomNetworkAE", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomNetworkConnection [PS 3.15]</summary>
		public static DicomUID LDAPDicomNetworkConnection = new DicomUID("1.2.840.10008.15.0.4.6", "dicomNetworkConnection", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomUniqueAETitle [PS 3.15]</summary>
		public static DicomUID LDAPDicomUniqueAETitle = new DicomUID("1.2.840.10008.15.0.4.7", "dicomUniqueAETitle", DicomUidType.LDAP);

		/// <summary>LDAP OID: dicomTransferCapability [PS 3.15]</summary>
		public static DicomUID LDAPDicomTransferCapability = new DicomUID("1.2.840.10008.15.0.4.8", "dicomTransferCapability", DicomUidType.LDAP);

		/// <summary>Coding Scheme: DICOM Controlled Terminology [PS 3.16]</summary>
		public static DicomUID DICOMControlledTerminology = new DicomUID("1.2.840.10008.2.16.4", "DICOM Controlled Terminology", DicomUidType.CodingScheme);

		/// <summary>DICOM UIDs as a Coding Scheme: DICOM UID Registry [PS 3.6]</summary>
		public static DicomUID DICOMUIDRegistry = new DicomUID("1.2.840.10008.2.6.1", "DICOM UID Registry", DicomUidType.CodingScheme);

		/// <summary>Application Context Name: DICOM Application Context Name [PS 3.7]</summary>
		public static DicomUID DICOMApplicationContextName = new DicomUID("1.2.840.10008.3.1.1.1", "DICOM Application Context Name", DicomUidType.ApplicationContextName);

		/// <summary>SOP Class: Detached Patient Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedPatientManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.1.1", "Detached Patient Management SOP Class", DicomUidType.SOPClass);

		/// <summary>Meta SOP Class: Detached Patient Management Meta SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedPatientManagementMetaSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.1.4", "Detached Patient Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>SOP Class: Detached Visit Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedVisitManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.2.1", "Detached Visit Management SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Detached Study Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedStudyManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.3.1", "Detached Study Management SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Study Component Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID StudyComponentManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.3.2", "Study Component Management SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Modality Performed Procedure Step SOP Class [PS 3.4]</summary>
		public static DicomUID ModalityPerformedProcedureStepSOPClass = new DicomUID("1.2.840.10008.3.1.2.3.3", "Modality Performed Procedure Step SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Modality Performed Procedure Step Retrieve SOP Class [PS 3.4]</summary>
		public static DicomUID ModalityPerformedProcedureStepRetrieveSOPClass = new DicomUID("1.2.840.10008.3.1.2.3.4", "Modality Performed Procedure Step Retrieve SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Modality Performed Procedure Step Notification SOP Class [PS 3.4]</summary>
		public static DicomUID ModalityPerformedProcedureStepNotificationSOPClass = new DicomUID("1.2.840.10008.3.1.2.3.5", "Modality Performed Procedure Step Notification SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Detached Results Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedResultsManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.5.1", "Detached Results Management SOP Class", DicomUidType.SOPClass);

		/// <summary>Meta SOP Class: Detached Results Management Meta SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedResultsManagementMetaSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.5.4", "Detached Results Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>Meta SOP Class: Detached Study Management Meta SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedStudyManagementMetaSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.5.5", "Detached Study Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>SOP Class: Detached Interpretation Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID DetachedInterpretationManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.3.1.2.6.1", "Detached Interpretation Management SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Film Session SOP Class [PS 3.4]</summary>
		public static DicomUID BasicFilmSessionSOPClass = new DicomUID("1.2.840.10008.5.1.1.1", "Basic Film Session SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Print Job SOP Class [PS 3.4]</summary>
		public static DicomUID PrintJobSOPClass = new DicomUID("1.2.840.10008.5.1.1.14", "Print Job SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Annotation Box SOP Class [PS 3.4]</summary>
		public static DicomUID BasicAnnotationBoxSOPClass = new DicomUID("1.2.840.10008.5.1.1.15", "Basic Annotation Box SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Printer SOP Class [PS 3.4]</summary>
		public static DicomUID PrinterSOPClass = new DicomUID("1.2.840.10008.5.1.1.16", "Printer SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Printer Configuration Retrieval SOP Class [PS 3.4]</summary>
		public static DicomUID PrinterConfigurationRetrievalSOPClass = new DicomUID("1.2.840.10008.5.1.1.16.376", "Printer Configuration Retrieval SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known Printer SOP Instance: Printer SOP Instance [PS 3.4]</summary>
		public static DicomUID PrinterSOPInstance = new DicomUID("1.2.840.10008.5.1.1.17", "Printer SOP Instance", DicomUidType.SOPInstance);

		/// <summary>Well-known Printer SOP Instance: Printer Configuration Retrieval SOP Instance [PS 3.4]</summary>
		public static DicomUID PrinterConfigurationRetrievalSOPInstance = new DicomUID("1.2.840.10008.5.1.1.17.376", "Printer Configuration Retrieval SOP Instance", DicomUidType.SOPInstance);

		/// <summary>Meta SOP Class: Basic Color Print Management Meta SOP Class [PS 3.4]</summary>
		public static DicomUID BasicColorPrintManagementMetaSOPClass = new DicomUID("1.2.840.10008.5.1.1.18", "Basic Color Print Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>Meta SOP Class: Referenced Color Print Management Meta SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID ReferencedColorPrintManagementMetaSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.18.1", "Referenced Color Print Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>SOP Class: Basic Film Box SOP Class [PS 3.4]</summary>
		public static DicomUID BasicFilmBoxSOPClass = new DicomUID("1.2.840.10008.5.1.1.2", "Basic Film Box SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: VOI LUT Box SOP Class [PS 3.4]</summary>
		public static DicomUID VOILUTBoxSOPClass = new DicomUID("1.2.840.10008.5.1.1.22", "VOI LUT Box SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Presentation LUT SOP Class [PS 3.4]</summary>
		public static DicomUID PresentationLUTSOPClass = new DicomUID("1.2.840.10008.5.1.1.23", "Presentation LUT SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Image Overlay Box SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID ImageOverlayBoxSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.24", "Image Overlay Box SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Print Image Overlay Box SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID BasicPrintImageOverlayBoxSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.24.1", "Basic Print Image Overlay Box SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known Print Queue SOP Instance: Print Queue SOP Instance [PS 3.4] (Retired)</summary>
		public static DicomUID PrintQueueSOPInstanceRETIRED = new DicomUID("1.2.840.10008.5.1.1.25", "Print Queue SOP Instance", DicomUidType.SOPInstance);

		/// <summary>SOP Class: Print Queue Management SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID PrintQueueManagementSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.26", "Print Queue Management SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Stored Print Storage SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID StoredPrintStorageSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.27", "Stored Print Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Hardcopy Grayscale Image Storage SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID HardcopyGrayscaleImageStorageSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.29", "Hardcopy Grayscale Image Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Hardcopy Color Image Storage SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID HardcopyColorImageStorageSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.30", "Hardcopy Color Image Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Pull Print Request SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID PullPrintRequestSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.31", "Pull Print Request SOP Class", DicomUidType.SOPClass);

		/// <summary>Meta SOP Class: Pull Stored Print Management Meta SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID PullStoredPrintManagementMetaSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.32", "Pull Stored Print Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>SOP Class: Media Creation Management SOP Class UID [PS3.4]</summary>
		public static DicomUID MediaCreationManagementSOPClassUID = new DicomUID("1.2.840.10008.5.1.1.33", "Media Creation Management SOP Class UID", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Grayscale Image Box SOP Class [PS 3.4]</summary>
		public static DicomUID BasicGrayscaleImageBoxSOPClass = new DicomUID("1.2.840.10008.5.1.1.4", "Basic Grayscale Image Box SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Color Image Box SOP Class [PS 3.4]</summary>
		public static DicomUID BasicColorImageBoxSOPClass = new DicomUID("1.2.840.10008.5.1.1.4.1", "Basic Color Image Box SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Referenced Image Box SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID ReferencedImageBoxSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.4.2", "Referenced Image Box SOP Class", DicomUidType.SOPClass);

		/// <summary>Meta SOP Class: Basic Grayscale Print Management Meta SOP Class [PS 3.4]</summary>
		public static DicomUID BasicGrayscalePrintManagementMetaSOPClass = new DicomUID("1.2.840.10008.5.1.1.9", "Basic Grayscale Print Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>Meta SOP Class: Referenced Grayscale Print Management Meta SOP Class [PS 3.4] (Retired)</summary>
		public static DicomUID ReferencedGrayscalePrintManagementMetaSOPClassRETIRED = new DicomUID("1.2.840.10008.5.1.1.9.1", "Referenced Grayscale Print Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>SOP Class: Computed Radiography Image Storage [PS 3.4]</summary>
		public static DicomUID ComputedRadiographyImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.1", "Computed Radiography Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Digital X-Ray Image Storage – For Presentation [PS 3.4]</summary>
		public static DicomUID DigitalXRayImageStorageForPresentation = new DicomUID("1.2.840.10008.5.1.4.1.1.1.1", "Digital X-Ray Image Storage – For Presentation", DicomUidType.SOPClass);

		/// <summary>SOP Class: Digital X-Ray Image Storage – For Processing [PS 3.4]</summary>
		public static DicomUID DigitalXRayImageStorageForProcessing = new DicomUID("1.2.840.10008.5.1.4.1.1.1.1.1", "Digital X-Ray Image Storage – For Processing", DicomUidType.SOPClass);

		/// <summary>SOP Class: Digital Mammography X-Ray Image Storage – For Presentation [PS 3.4]</summary>
		public static DicomUID DigitalMammographyXRayImageStorageForPresentation = new DicomUID("1.2.840.10008.5.1.4.1.1.1.2", "Digital Mammography X-Ray Image Storage – For Presentation", DicomUidType.SOPClass);

		/// <summary>SOP Class: Digital Mammography X-Ray Image Storage – For Processing [PS 3.4]</summary>
		public static DicomUID DigitalMammographyXRayImageStorageForProcessing = new DicomUID("1.2.840.10008.5.1.4.1.1.1.2.1", "Digital Mammography X-Ray Image Storage – For Processing", DicomUidType.SOPClass);

		/// <summary>SOP Class: Digital Intra-oral X-Ray Image Storage – For Presentation [PS 3.4]</summary>
		public static DicomUID DigitalIntraoralXRayImageStorageForPresentation = new DicomUID("1.2.840.10008.5.1.4.1.1.1.3", "Digital Intra-oral X-Ray Image Storage – For Presentation", DicomUidType.SOPClass);

		/// <summary>SOP Class: Digital Intra-oral X-Ray Image Storage – For Processing [PS 3.4]</summary>
		public static DicomUID DigitalIntraoralXRayImageStorageForProcessing = new DicomUID("1.2.840.10008.5.1.4.1.1.1.3.1", "Digital Intra-oral X-Ray Image Storage – For Processing", DicomUidType.SOPClass);

		/// <summary>SOP Class: Standalone Modality LUT Storage [PS 3.4] (Retired)</summary>
		public static DicomUID StandaloneModalityLUTStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.10", "Standalone Modality LUT Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Encapsulated PDF Storage [PS 3.4]</summary>
		public static DicomUID EncapsulatedPDFStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.104.1", "Encapsulated PDF Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Encapsulated CDA Storage [PS 3.4]</summary>
		public static DicomUID EncapsulatedCDAStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.104.2", "Encapsulated CDA Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Standalone VOI LUT Storage [PS 3.4] (Retired)</summary>
		public static DicomUID StandaloneVOILUTStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.11", "Standalone VOI LUT Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Grayscale Softcopy Presentation State Storage SOP Class [PS 3.4]</summary>
		public static DicomUID GrayscaleSoftcopyPresentationStateStorageSOPClass = new DicomUID("1.2.840.10008.5.1.4.1.1.11.1", "Grayscale Softcopy Presentation State Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Color Softcopy Presentation State Storage SOP Class [PS 3.4]</summary>
		public static DicomUID ColorSoftcopyPresentationStateStorageSOPClass = new DicomUID("1.2.840.10008.5.1.4.1.1.11.2", "Color Softcopy Presentation State Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Pseudo-Color Softcopy Presentation State Storage SOP Class [PS 3.4]</summary>
		public static DicomUID PseudoColorSoftcopyPresentationStateStorageSOPClass = new DicomUID("1.2.840.10008.5.1.4.1.1.11.3", "Pseudo-Color Softcopy Presentation State Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Blending Softcopy Presentation State Storage SOP Class [PS 3.4]</summary>
		public static DicomUID BlendingSoftcopyPresentationStateStorageSOPClass = new DicomUID("1.2.840.10008.5.1.4.1.1.11.4", "Blending Softcopy Presentation State Storage SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: X-Ray Angiographic Image Storage [PS 3.4]</summary>
		public static DicomUID XRayAngiographicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.12.1", "X-Ray Angiographic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Enhanced XA Image Storage [PS 3.4]</summary>
		public static DicomUID EnhancedXAImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.12.1.1", "Enhanced XA Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: X-Ray Radiofluoroscopic Image Storage [PS 3.4]</summary>
		public static DicomUID XRayRadiofluoroscopicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.12.2", "X-Ray Radiofluoroscopic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Enhanced XRF Image Storage [PS 3.4]</summary>
		public static DicomUID EnhancedXRFImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.12.2.1", "Enhanced XRF Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: X-Ray Angiographic Bi-Plane Image Storage [PS 3.4] (Retired)</summary>
		public static DicomUID XRayAngiographicBiPlaneImageStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.12.3", "X-Ray Angiographic Bi-Plane Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Positron Emission Tomography Image Storage [PS 3.4]</summary>
		public static DicomUID PositronEmissionTomographyImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.128", "Positron Emission Tomography Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Standalone PET Curve Storage [PS 3.4] (Retired)</summary>
		public static DicomUID StandalonePETCurveStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.129", "Standalone PET Curve Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: X-Ray 3D Angiographic Image Storage [PS 3.4]</summary>
		public static DicomUID XRay3DAngiographicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.13.1.1", "X-Ray 3D Angiographic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: X-Ray 3D Craniofacial Image Storage [PS 3.4]</summary>
		public static DicomUID XRay3DCraniofacialImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.13.1.2", "X-Ray 3D Craniofacial Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: CT Image Storage [PS 3.4]</summary>
		public static DicomUID CTImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.2", "CT Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Enhanced CT Image Storage [PS 3.4]</summary>
		public static DicomUID EnhancedCTImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.2.1", "Enhanced CT Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Nuclear Medicine Image Storage [PS 3.4]</summary>
		public static DicomUID NuclearMedicineImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.20", "Nuclear Medicine Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ultrasound Multi-frame Image Storage [PS 3.4] (Retired)</summary>
		public static DicomUID UltrasoundMultiframeImageStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.3", "Ultrasound Multi-frame Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ultrasound Multi-frame Image Storage [PS 3.4]</summary>
		public static DicomUID UltrasoundMultiframeImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.3.1", "Ultrasound Multi-frame Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: MR Image Storage [PS 3.4]</summary>
		public static DicomUID MRImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.4", "MR Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Enhanced MR Image Storage [PS 3.4]</summary>
		public static DicomUID EnhancedMRImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.4.1", "Enhanced MR Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: MR Spectroscopy Storage [PS 3.4]</summary>
		public static DicomUID MRSpectroscopyStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.4.2", "MR Spectroscopy Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Image Storage [PS 3.4]</summary>
		public static DicomUID RTImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.1", "RT Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Dose Storage [PS 3.4]</summary>
		public static DicomUID RTDoseStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.2", "RT Dose Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Structure Set Storage [PS 3.4]</summary>
		public static DicomUID RTStructureSetStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.3", "RT Structure Set Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Beams Treatment Record Storage [PS 3.4]</summary>
		public static DicomUID RTBeamsTreatmentRecordStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.4", "RT Beams Treatment Record Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Plan Storage [PS 3.4]</summary>
		public static DicomUID RTPlanStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.5", "RT Plan Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Brachy Treatment Record Storage [PS 3.4]</summary>
		public static DicomUID RTBrachyTreatmentRecordStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.6", "RT Brachy Treatment Record Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Treatment Summary Record Storage [PS 3.4]</summary>
		public static DicomUID RTTreatmentSummaryRecordStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.7", "RT Treatment Summary Record Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Ion Plan Storage [PS 3.4]</summary>
		public static DicomUID RTIonPlanStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.8", "RT Ion Plan Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Ion Beams Treatment Record Storage [PS 3.4]</summary>
		public static DicomUID RTIonBeamsTreatmentRecordStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.481.9", "RT Ion Beams Treatment Record Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Nuclear Medicine Image Storage [PS 3.4] (Retired)</summary>
		public static DicomUID NuclearMedicineImageStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.5", "Nuclear Medicine Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ultrasound Image Storage [PS 3.4] (Retired)</summary>
		public static DicomUID UltrasoundImageStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.6", "Ultrasound Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ultrasound Image Storage [PS 3.4]</summary>
		public static DicomUID UltrasoundImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.6.1", "Ultrasound Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Raw Data Storage [PS 3.4]</summary>
		public static DicomUID RawDataStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.66", "Raw Data Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Spatial Registration Storage [PS 3.4]</summary>
		public static DicomUID SpatialRegistrationStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.66.1", "Spatial Registration Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Spatial Fiducials Storage [PS 3.4]</summary>
		public static DicomUID SpatialFiducialsStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.66.2", "Spatial Fiducials Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Deformable Spatial Registration Storage [PS 3.4]</summary>
		public static DicomUID DeformableSpatialRegistrationStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.66.3", "Deformable Spatial Registration Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Segmentation Storage [PS 3.4]</summary>
		public static DicomUID SegmentationStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.66.4", "Segmentation Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Real World Value Mapping Storage [PS 3.4]</summary>
		public static DicomUID RealWorldValueMappingStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.67", "Real World Value Mapping Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Secondary Capture Image Storage [PS 3.4]</summary>
		public static DicomUID SecondaryCaptureImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.7", "Secondary Capture Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Multi-frame Single Bit Secondary Capture Image Storage [PS 3.4]</summary>
		public static DicomUID MultiframeSingleBitSecondaryCaptureImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.7.1", "Multi-frame Single Bit Secondary Capture Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Multi-frame Grayscale Byte Secondary Capture Image Storage [PS 3.4]</summary>
		public static DicomUID MultiframeGrayscaleByteSecondaryCaptureImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.7.2", "Multi-frame Grayscale Byte Secondary Capture Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Multi-frame Grayscale Word Secondary Capture Image Storage [PS 3.4]</summary>
		public static DicomUID MultiframeGrayscaleWordSecondaryCaptureImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.7.3", "Multi-frame Grayscale Word Secondary Capture Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Multi-frame True Color Secondary Capture Image Storage [PS 3.4]</summary>
		public static DicomUID MultiframeTrueColorSecondaryCaptureImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.7.4", "Multi-frame True Color Secondary Capture Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: VL Image Storage - Trial [PS 3.4] (Retired)</summary>
		public static DicomUID VLImageStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1", "VL Image Storage - Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: VL Endoscopic Image Storage [PS 3.4]</summary>
		public static DicomUID VLEndoscopicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.1", "VL Endoscopic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Video Endoscopic Image Storage [PS 3.4]</summary>
		public static DicomUID VideoEndoscopicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.1.1", "Video Endoscopic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: VL Microscopic Image Storage [PS 3.4]</summary>
		public static DicomUID VLMicroscopicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.2", "VL Microscopic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Video Microscopic Image Storage [PS 3.4]</summary>
		public static DicomUID VideoMicroscopicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.2.1", "Video Microscopic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: VL Slide-Coordinates Microscopic Image Storage [PS 3.4]</summary>
		public static DicomUID VLSlideCoordinatesMicroscopicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.3", "VL Slide-Coordinates Microscopic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: VL Photographic Image Storage [PS 3.4]</summary>
		public static DicomUID VLPhotographicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.4", "VL Photographic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Video Photographic Image Storage [PS 3.4]</summary>
		public static DicomUID VideoPhotographicImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.4.1", "Video Photographic Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ophthalmic Photography 8 Bit Image Storage [PS 3.4]</summary>
		public static DicomUID OphthalmicPhotography8BitImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.5.1", "Ophthalmic Photography 8 Bit Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ophthalmic Photography 16 Bit Image Storage [PS 3.4]</summary>
		public static DicomUID OphthalmicPhotography16BitImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.5.2", "Ophthalmic Photography 16 Bit Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Stereometric Relationship Storage [PS 3.4]</summary>
		public static DicomUID StereometricRelationshipStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.5.3", "Stereometric Relationship Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ophthalmic Tomography Image Storage [PS 3.4]</summary>
		public static DicomUID OphthalmicTomographyImageStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.77.1.5.4", "Ophthalmic Tomography Image Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: VL Multi-frame Image Storage – Trial [PS 3.4] (Retired)</summary>
		public static DicomUID VLMultiframeImageStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.77.2", "VL Multi-frame Image Storage – Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: Standalone Overlay Storage [PS 3.4] (Retired)</summary>
		public static DicomUID StandaloneOverlayStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.8", "Standalone Overlay Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Text SR Storage – Trial [PS 3.4] (Retired)</summary>
		public static DicomUID TextSRStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.88.1", "Text SR Storage – Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Text SR Storage [PS 3.4]</summary>
		public static DicomUID BasicTextSRStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.11", "Basic Text SR Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Audio SR Storage – Trial [PS 3.4] (Retired)</summary>
		public static DicomUID AudioSRStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.88.2", "Audio SR Storage – Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: Enhanced SR Storage [PS 3.4]</summary>
		public static DicomUID EnhancedSRStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.22", "Enhanced SR Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Detail SR Storage – Trial [PS 3.4] (Retired)</summary>
		public static DicomUID DetailSRStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.88.3", "Detail SR Storage – Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: Comprehensive SR Storage [PS 3.4]</summary>
		public static DicomUID ComprehensiveSRStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.33", "Comprehensive SR Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Comprehensive SR Storage – Trial [PS 3.4] (Retired)</summary>
		public static DicomUID ComprehensiveSRStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.88.4", "Comprehensive SR Storage – Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: Procedure Log Storage [PS 3.4]</summary>
		public static DicomUID ProcedureLogStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.40", "Procedure Log Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Mammography CAD SR Storage [PS 3.4]</summary>
		public static DicomUID MammographyCADSRStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.50", "Mammography CAD SR Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Key Object Selection Document Storage [PS 3.4]</summary>
		public static DicomUID KeyObjectSelectionDocumentStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.59", "Key Object Selection Document Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Chest CAD SR Storage [PS 3.4]</summary>
		public static DicomUID ChestCADSRStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.65", "Chest CAD SR Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: X-Ray Radiation Dose SR Storage [PS 3.4]</summary>
		public static DicomUID XRayRadiationDoseSRStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.88.67", "X-Ray Radiation Dose SR Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Standalone Curve Storage [PS 3.4] (Retired)</summary>
		public static DicomUID StandaloneCurveStorageRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.9", "Standalone Curve Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Waveform Storage - Trial [PS 3.4] (Retired)</summary>
		public static DicomUID WaveformStorageTrialRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.1.9.1", "Waveform Storage - Trial", DicomUidType.SOPClass);

		/// <summary>SOP Class: 12-lead ECG Waveform Storage [PS 3.4]</summary>
		public static DicomUID TwelveLeadECGWaveformStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.9.1.1", "12-lead ECG Waveform Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: General ECG Waveform Storage [PS 3.4]</summary>
		public static DicomUID GeneralECGWaveformStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.9.1.2", "General ECG Waveform Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Ambulatory ECG Waveform Storage [PS 3.4]</summary>
		public static DicomUID AmbulatoryECGWaveformStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.9.1.3", "Ambulatory ECG Waveform Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Hemodynamic Waveform Storage [PS 3.4]</summary>
		public static DicomUID HemodynamicWaveformStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.9.2.1", "Hemodynamic Waveform Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Cardiac Electrophysiology Waveform Storage [PS 3.4]</summary>
		public static DicomUID CardiacElectrophysiologyWaveformStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.9.3.1", "Cardiac Electrophysiology Waveform Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Basic Voice Audio Waveform Storage [PS 3.4]</summary>
		public static DicomUID BasicVoiceAudioWaveformStorage = new DicomUID("1.2.840.10008.5.1.4.1.1.9.4.1", "Basic Voice Audio Waveform Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Patient Root Query/Retrieve Information Model – FIND [PS 3.4]</summary>
		public static DicomUID PatientRootQueryRetrieveInformationModelFIND = new DicomUID("1.2.840.10008.5.1.4.1.2.1.1", "Patient Root Query/Retrieve Information Model – FIND", DicomUidType.SOPClass);

		/// <summary>SOP Class: Patient Root Query/Retrieve Information Model – MOVE [PS 3.4]</summary>
		public static DicomUID PatientRootQueryRetrieveInformationModelMOVE = new DicomUID("1.2.840.10008.5.1.4.1.2.1.2", "Patient Root Query/Retrieve Information Model – MOVE", DicomUidType.SOPClass);

		/// <summary>SOP Class: Patient Root Query/Retrieve Information Model – GET [PS 3.4]</summary>
		public static DicomUID PatientRootQueryRetrieveInformationModelGET = new DicomUID("1.2.840.10008.5.1.4.1.2.1.3", "Patient Root Query/Retrieve Information Model – GET", DicomUidType.SOPClass);

		/// <summary>SOP Class: Study Root Query/Retrieve Information Model – FIND [PS 3.4]</summary>
		public static DicomUID StudyRootQueryRetrieveInformationModelFIND = new DicomUID("1.2.840.10008.5.1.4.1.2.2.1", "Study Root Query/Retrieve Information Model – FIND", DicomUidType.SOPClass);

		/// <summary>SOP Class: Study Root Query/Retrieve Information Model – MOVE [PS 3.4]</summary>
		public static DicomUID StudyRootQueryRetrieveInformationModelMOVE = new DicomUID("1.2.840.10008.5.1.4.1.2.2.2", "Study Root Query/Retrieve Information Model – MOVE", DicomUidType.SOPClass);

		/// <summary>SOP Class: Study Root Query/Retrieve Information Model – GET [PS 3.4]</summary>
		public static DicomUID StudyRootQueryRetrieveInformationModelGET = new DicomUID("1.2.840.10008.5.1.4.1.2.2.3", "Study Root Query/Retrieve Information Model – GET", DicomUidType.SOPClass);

		/// <summary>SOP Class: Patient/Study Only Query/Retrieve Information Model - FIND [PS 3.4] (Retired)</summary>
		public static DicomUID PatientStudyOnlyQueryRetrieveInformationModelFINDRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.2.3.1", "Patient/Study Only Query/Retrieve Information Model - FIND", DicomUidType.SOPClass);

		/// <summary>SOP Class: Patient/Study Only Query/Retrieve Information Model - MOVE [PS 3.4] (Retired)</summary>
		public static DicomUID PatientStudyOnlyQueryRetrieveInformationModelMOVERETIRED = new DicomUID("1.2.840.10008.5.1.4.1.2.3.2", "Patient/Study Only Query/Retrieve Information Model - MOVE", DicomUidType.SOPClass);

		/// <summary>SOP Class: Patient/Study Only Query/Retrieve Information Model - GET [PS 3.4] (Retired)</summary>
		public static DicomUID PatientStudyOnlyQueryRetrieveInformationModelGETRETIRED = new DicomUID("1.2.840.10008.5.1.4.1.2.3.3", "Patient/Study Only Query/Retrieve Information Model - GET", DicomUidType.SOPClass);

		/// <summary>SOP Class: Modality Worklist Information Model – FIND [PS 3.4]</summary>
		public static DicomUID ModalityWorklistInformationModelFIND = new DicomUID("1.2.840.10008.5.1.4.31", "Modality Worklist Information Model – FIND", DicomUidType.SOPClass);

		/// <summary>Meta SOP Class: General Purpose Worklist Management Meta SOP Class [PS 3.4]</summary>
		public static DicomUID GeneralPurposeWorklistManagementMetaSOPClass = new DicomUID("1.2.840.10008.5.1.4.32", "General Purpose Worklist Management Meta SOP Class", DicomUidType.MetaSOPClass);

		/// <summary>SOP Class: General Purpose Worklist Information Model – FIND [PS 3.4]</summary>
		public static DicomUID GeneralPurposeWorklistInformationModelFIND = new DicomUID("1.2.840.10008.5.1.4.32.1", "General Purpose Worklist Information Model – FIND", DicomUidType.SOPClass);

		/// <summary>SOP Class: General Purpose Scheduled Procedure Step SOP Class [PS 3.4]</summary>
		public static DicomUID GeneralPurposeScheduledProcedureStepSOPClass = new DicomUID("1.2.840.10008.5.1.4.32.2", "General Purpose Scheduled Procedure Step SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: General Purpose Performed Procedure Step SOP Class [PS 3.4]</summary>
		public static DicomUID GeneralPurposePerformedProcedureStepSOPClass = new DicomUID("1.2.840.10008.5.1.4.32.3", "General Purpose Performed Procedure Step SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Instance Availability Notification SOP Class [PS 3.4]</summary>
		public static DicomUID InstanceAvailabilityNotificationSOPClass = new DicomUID("1.2.840.10008.5.1.4.33", "Instance Availability Notification SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Beams Delivery Instruction Storage (Supplement 74 Frozen Draft) [PS 3.4]</summary>
		public static DicomUID RTBeamsDeliveryInstructionStorageSupplement74FrozenDraft = new DicomUID("1.2.840.10008.5.1.4.34.1", "RT Beams Delivery Instruction Storage (Supplement 74 Frozen Draft)", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Conventional Machine Verification (Supplement 74 Frozen Draft) [PS 3.4]</summary>
		public static DicomUID RTConventionalMachineVerificationSupplement74FrozenDraft = new DicomUID("1.2.840.10008.5.1.4.34.2", "RT Conventional Machine Verification (Supplement 74 Frozen Draft)", DicomUidType.SOPClass);

		/// <summary>SOP Class: RT Ion Machine Verification (Supplement 74 Frozen Draft) [PS 3.4]</summary>
		public static DicomUID RTIonMachineVerificationSupplement74FrozenDraft = new DicomUID("1.2.840.10008.5.1.4.34.3", "RT Ion Machine Verification (Supplement 74 Frozen Draft)", DicomUidType.SOPClass);

		/// <summary>Service Class: Unified Worklist and Procedure Step Service Class [PS 3.4]</summary>
		public static DicomUID UnifiedWorklistAndProcedureStepSOPClass = new DicomUID("1.2.840.10008.5.1.4.34.4", "Unified Worklist and Procedure Step Service Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Unified Procedure Step – Push SOP Class [PS 3.4]</summary>
		public static DicomUID UnifiedProcedureStepPushSOPClass = new DicomUID("1.2.840.10008.5.1.4.34.4.1", "Unified Procedure Step – Push SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Unified Procedure Step – Watch SOP Class [PS 3.4]</summary>
		public static DicomUID UnifiedProcedureStepWatchSOPClass = new DicomUID("1.2.840.10008.5.1.4.34.4.2", "Unified Procedure Step – Watch SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Unified Procedure Step – Pull SOP Class [PS 3.4]</summary>
		public static DicomUID UnifiedProcedureStepPullSOPClass = new DicomUID("1.2.840.10008.5.1.4.34.4.3", "Unified Procedure Step – Pull SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Unified Procedure Step – Event SOP Class [PS 3.4]</summary>
		public static DicomUID UnifiedProcedureStepEventSOPClass = new DicomUID("1.2.840.10008.5.1.4.34.4.4", "Unified Procedure Step – Event SOP Class", DicomUidType.SOPClass);

		/// <summary>Well-known SOP Instance: Unified Worklist and Procedure Step SOP Instance [PS 3.4]</summary>
		public static DicomUID UnifiedWorklistAndProcedureStepSOPInstance = new DicomUID("1.2.840.10008.5.1.4.34.5", "Unified Worklist and Procedure Step SOP Instance", DicomUidType.SOPInstance);

		/// <summary>SOP Class: General Relevant Patient Information Query [PS 3.4]</summary>
		public static DicomUID GeneralRelevantPatientInformationQuery = new DicomUID("1.2.840.10008.5.1.4.37.1", "General Relevant Patient Information Query", DicomUidType.SOPClass);

		/// <summary>SOP Class: Breast Imaging Relevant Patient Information Query [PS 3.4]</summary>
		public static DicomUID BreastImagingRelevantPatientInformationQuery = new DicomUID("1.2.840.10008.5.1.4.37.2", "Breast Imaging Relevant Patient Information Query", DicomUidType.SOPClass);

		/// <summary>SOP Class: Cardiac Relevant Patient Information Query [PS 3.4]</summary>
		public static DicomUID CardiacRelevantPatientInformationQuery = new DicomUID("1.2.840.10008.5.1.4.37.3", "Cardiac Relevant Patient Information Query", DicomUidType.SOPClass);

		/// <summary>SOP Class: Hanging Protocol Storage [PS 3.4]</summary>
		public static DicomUID HangingProtocolStorage = new DicomUID("1.2.840.10008.5.1.4.38.1", "Hanging Protocol Storage", DicomUidType.SOPClass);

		/// <summary>SOP Class: Hanging Protocol Information Model – FIND [PS 3.4]</summary>
		public static DicomUID HangingProtocolInformationModelFIND = new DicomUID("1.2.840.10008.5.1.4.38.2", "Hanging Protocol Information Model – FIND", DicomUidType.SOPClass);

		/// <summary>SOP Class: Hanging Protocol Information Model – MOVE [PS 3.4]</summary>
		public static DicomUID HangingProtocolInformationModelMOVE = new DicomUID("1.2.840.10008.5.1.4.38.3", "Hanging Protocol Information Model – MOVE", DicomUidType.SOPClass);

		/// <summary>SOP Class: Product Characteristics Query SOP Class [PS 3.4]</summary>
		public static DicomUID ProductCharacteristicsQuerySOPClass = new DicomUID("1.2.840.10008.5.1.4.41", "Product Characteristics Query SOP Class", DicomUidType.SOPClass);

		/// <summary>SOP Class: Substance Approval Query SOP Class [PS 3.4]</summary>
		public static DicomUID SubstanceApprovalQuerySOPClass = new DicomUID("1.2.840.10008.5.1.4.42", "Substance Approval Query SOP Class", DicomUidType.SOPClass);
		#endregion
	}
}
