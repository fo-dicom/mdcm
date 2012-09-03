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
using System.Reflection;

using Dicom.Data;
using Dicom.Utility;

namespace Dicom.Codec {
	#region IDcmCodec
	public interface IDcmCodec {
		string GetName();
		DicomTransferSyntax GetTransferSyntax();
		DcmCodecParameters GetDefaultParameters();
		void Encode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters);
		void Decode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters);
	}
	#endregion

	#region DicomCodecAttribute
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DicomCodecAttribute : Attribute {
		public DicomCodecAttribute() {
		}
	}
	#endregion

	#region DicomCodec
	public static class DicomCodec {
		private static Dictionary<DicomTransferSyntax, Type> _codecs;
		private static List<string> _codecNames = new List<string>();

		public static List<DicomTransferSyntax> GetRegisteredCodecs() {
			if (_codecs == null)
				RegisterCodecs();
			List<DicomTransferSyntax> codecs = new List<DicomTransferSyntax>();
			codecs.AddRange(_codecs.Keys);
			return codecs;
		}

		public static string[] GetRegisteredCodecNames() {
			return _codecNames.ToArray();
		}

		public static bool HasCodec(DicomTransferSyntax ts) {
			if (ts == DicomTransferSyntax.ImplicitVRLittleEndian ||
				ts == DicomTransferSyntax.ExplicitVRLittleEndian ||
				ts == DicomTransferSyntax.ExplicitVRBigEndian)
				return true;
			if (_codecs == null)
				return false;
			return _codecs.ContainsKey(ts);
		}

		public static IDcmCodec GetCodec(DicomTransferSyntax ts) {
			if (_codecs == null)
				RegisterCodecs();
			Type cType;
			if (_codecs.TryGetValue(ts, out cType)) {
				return (IDcmCodec)Activator.CreateInstance(cType);
			}
			throw new DicomCodecException("No registered codec for transfer syntax!");
		}

		public static void RegisterCodec(DicomTransferSyntax ts, Type type) {
			if (_codecs == null)
				_codecs = new Dictionary<DicomTransferSyntax, Type>();
			if (type.IsDefined(typeof(DicomCodecAttribute), false))
				_codecs.Add(ts, type);
		}

		public static void RegisterCodecs() {
#if SILVERLIGHT
            RegisterCodecs(null);
#else
			Assembly main = Assembly.GetEntryAssembly();
			AssemblyName[] referenced = main.GetReferencedAssemblies();

			RegisterCodecs(main);

			foreach (AssemblyName an in referenced) {
				Assembly asm = Assembly.Load(an);
				RegisterCodecs(asm);
			}
#endif
        }

		public static void RegisterExternalCodecs(string path, string pattern) {
#if !SILVERLIGHT
			DirectoryInfo dir = new DirectoryInfo(path);
			FileInfo[] files = dir.GetFiles(pattern);
			foreach (FileInfo file in files) {
				Debug.Log.Info("Codec File: {0}", file.FullName);
				try {
					//AssemblyDetails details = AssemblyDetails.FromFile(file.FullName);
					//if (details.CPUVersion == CPUVersion.x64 && IntPtr.Size != 8) continue;
					//if (details.CPUVersion == CPUVersion.x86 && IntPtr.Size != 4) continue;

					Assembly asm = Assembly.LoadFile(file.FullName);
					RegisterCodecs(asm);
				} catch (BadImageFormatException) {
					// incorrect CPU version
				} catch (Exception e) {
					Debug.Log.Error("Unable to load codecs from file [{0}]: {1}", file.FullName, e.ToString());
				}
			}
#endif
		}

		private static void RegisterCodecs(Assembly asm) {
			if (_codecs == null)
				_codecs = new Dictionary<DicomTransferSyntax, Type>();
#if !SILVERLIGHT
			bool x64 = (IntPtr.Size == 8);
			string m = String.Empty;

			PortableExecutableKinds kind;
			ImageFileMachine machine;
			asm.ManifestModule.GetPEKind(out kind, out machine);

			if ((kind & PortableExecutableKinds.PE32Plus) != 0)
				m = " [x64]";
			else if ((kind & PortableExecutableKinds.Required32Bit) != 0)
				m = " [x86]";

			Type[] types = asm.GetExportedTypes();
			for (int i = 0; i < types.Length; i++) {
				if (types[i].IsDefined(typeof(DicomCodecAttribute), false)) {
					IDcmCodec codec = (IDcmCodec)Activator.CreateInstance(types[i]);
					_codecs.Add(codec.GetTransferSyntax(), types[i]);
					_codecNames.Add(codec.GetName() + m);
					Debug.Log.Info("Codec: {0}", codec.GetName() + m);
				}
			}
#endif
        }
	}
	#endregion
}
