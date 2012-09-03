// http://wyday.com/blog/2010/how-to-detect-net-assemblies-x86-x64-anycpu/
using System.IO;

namespace Dicom.Utility {
	public enum CPUVersion {
		/// <summary>
		/// A .NET assembly that is compiled with the AnyCPU flag
		/// </summary>
		AnyCPU = 0,

		/// <summary>
		/// A file that's built for x86 machines
		/// </summary>
		x86 = 1,

		/// <summary>
		/// A file that's built for x64 machines
		/// </summary>
		x64 = 2
	}

	public enum FrameworkVersion {
		/// <summary>
		/// The .NET Framework version was unrecognized
		/// </summary>
		Unknown = -1,

		/// <summary>
		/// .NET Framework 2.0 (includes .NET 3.0 & 3.5)
		/// </summary>
		Net2_0 = 0,

		/// <summary>
		/// .NET Framework 4.0
		/// </summary>
		Net4_0 = 1
	}

	/// <summary>
	/// AssemblyDetails is used in the update builder, wyBuild (http://wyday.com/wybuild/), 
	/// to automagically detect .NET assemblies properties, namely:
	///  - if they are compiled with a Strong Name,
	///  - CPUVersion the assembly was compiled as,
	///  - .NET Framework the assembly was compiled for (.NET 2.0 or 4.0)
	/// </summary>
	public class AssemblyDetails {
		/// <summary>
		/// The CPUVersion the assembly was compiled with.
		/// </summary>
		public CPUVersion CPUVersion;

		/// <summary>
		/// The .NET Framework required for the assembly.
		/// </summary>
		public FrameworkVersion FrameworkVersion;

		/// <summary>
		/// True if the assembly has a strong name. (Neccessary for GAC installations)
		/// </summary>
		public bool StrongName;


		/// <summary>
		/// Get the AssemblyDetails of a .NET 2.0+ assembly.
		/// </summary>
		/// <param name="file">The file to get the AssemblyDetails of.</param>
		/// <returns>An AssemblyDetails object for .NET 2.0+ assemblies. Null otherwise.</returns>
		public static AssemblyDetails FromFile(string file) {
			AssemblyDetails assembInfo = new AssemblyDetails();

			using (FileStream s = new FileStream(file, FileMode.Open, FileAccess.Read)) {
				using (BinaryReader r = new BinaryReader(s)) {
					byte[] bytes = r.ReadBytes(2);

					// Verify file starts with "MZ" signature.
					if ((bytes[0] != 0x4d) || (bytes[1] != 0x5a)) {
						// Not a PE file.
						return null;
					}

					// Partion II, 25.2.1

					// OFFSET_TO_PE_HEADER_OFFSET = 0x3c
					s.Seek(0x3c, SeekOrigin.Begin);

					// read the offset to the PE Header
					uint offset = r.ReadUInt32();

					// go to the beginning of the PE Header
					s.Seek(offset, SeekOrigin.Begin);

					bytes = r.ReadBytes(4);

					// Verify PE header starts with 'PE\0\0'.
					if ((bytes[0] != 0x50) || (bytes[1] != 0x45) || (bytes[2] != 0) || (bytes[3] != 0)) {
						// Not a PE file.
						return null;
					}


					// It's a PE file, verify that it has the right "machine" code.
					// Partion II, 25.2.2
					//
					ushort machineCode = r.ReadUInt16();

					// IMAGE_FILE_MACHINE_AMD64 (aka x64) = 0x8664
					// IMAGE_FILE_MACHINE_I386 (aka x86) = 0x14c
					if (!(machineCode == 0x014c || machineCode == 0x8664)) {
						// Invalid or unrecognized PE file of some kind.
						return null;
					}


					// Locate the PE_OPTIONAL_HEADER


					// The PE_FILE_HEADER is 20bytes long we already
					// read the 2 byte machine code (hence 18byte seek)
					s.Seek(18, SeekOrigin.Current);

					ushort magic = r.ReadUInt16();

					switch (magic) {
					case 0x10b: // PE32

						// set to AnyCPU for now - we'll check later if image is x86 specific
						assembInfo.CPUVersion = CPUVersion.AnyCPU;

						break;

					case 0x20b: // PE32+ (aka x64)
						assembInfo.CPUVersion = CPUVersion.x64;
						break;

					default: // unknown assembly type
						return null;
					}


					// Read the SectionAlignment & FileAlignment for
					// conversion from RVA to file address
					s.Seek(30, SeekOrigin.Current);

					uint sectionAlignment = r.ReadUInt32();
					uint fileAlignment = r.ReadUInt32();


					// go to 'NumberOfRvaAndSizes' in the PE Header
					// at 92/108 from start of PE Header for PE32/PE32+
					s.Seek(magic == 0x10b ? 52 : 68, SeekOrigin.Current);

					// verify that the number of data directories is 0x10.

					uint numDataDirs = r.ReadUInt32();  // Partition II, 25.2.3.2

					if (numDataDirs != 0x10) // Partition II, 25.2.3.2
                {
						// Invalid or unrecognized PE file of some kind.
						return null;
					}


					// Go to the CLR Runtime Header
					// at 208/224 from start of PE Header for PE32/PE32+
					s.Seek(112, SeekOrigin.Current);


					// Check for the existence of a non-null CLI header.
					// If found, this is an assembly of some kind, otherwise
					// it's a native PE file of one kind or another.

					uint rvaCLIHeader = r.ReadUInt32();  // Partition II, 25.2.3.3, CLI Header (rva)
					// uint cliHeaderSize = UIntFromBytes(pPEOptionalHeader + 212); // Partition II, 25.2.3.3, CLI Header (size)

					if (rvaCLIHeader == 0) {
						// Not an assembly.
						return null;
					}


					// Partition II, 25.3.3 (CLI Header)


					// Go to the begginning of the CLI header (RVA -> file address)

					/*
					-> Converting from Relative Virtual Address to File Address:

					   FA = RVA - sectionAlignment + fileAlignment
                 
						   The section alignment in memory is sectionAlignment (usually 0x2000),
					   and since the RVA for the CLR header is 2008, on subtracting 2000 from
					   2008, the difference comes to 8. Thus, the CLR header is placed 8 bytes
					   away from the start of the section.
 
						   A file on disk has the alignment of fileAlignment (usually 512 bytes).
					   Therefore, the first section would start at position 512 from the start
					   of the file. As the CLR is 8 bytes away from the section start, 8 is added
					   to 512, (section start for a file on disk), thereby arriving at a value of 520.
					   The next 72 bytes (0x48) are picked up from this position, since they
					   constitute the CLR header, and they are loaded at location 0x4002008.

					*/

					// Also, skip the CLI header size = 4 bytes
					s.Seek((rvaCLIHeader - sectionAlignment + fileAlignment) + 4, SeekOrigin.Begin);

					ushort majorVersion = r.ReadUInt16();
					ushort minorVersion = r.ReadUInt16();

					// 2.5 means the file is a .NET Framework 2.0+ assembly
					if (!(majorVersion == 2 && minorVersion == 5))
						return null;


					// RVA for the MetaData (we'll read the metadata later)
					uint rvaMetaData = r.ReadUInt32();
					s.Seek(4, SeekOrigin.Current); // skip the size

					// Partition II, 25.3.3.1

					// read the CLI flags
					uint cliFlags = r.ReadUInt32();

					// Detect if compiled with Platform Target of "x86"
					// COMIMAGE_FLAGS_32BITREQUIRED = 0x2;
					if (assembInfo.CPUVersion == CPUVersion.AnyCPU && (cliFlags & 0x2) == 0x2) {
						assembInfo.CPUVersion = CPUVersion.x86;
					}

					// Detect if the assembly is built with a strong name
					// CLI_FLAG_STRONG_NAME_SIGNED = 0x8;
					assembInfo.StrongName = ((cliFlags & 0x8) == 0x8);

					s.Seek((rvaMetaData - sectionAlignment + fileAlignment) + 12, SeekOrigin.Begin);


					// Read the framework version required (meta data - Partition II, 24.2.1 - pg 200)

					// read the version string length
					int versionLen = r.ReadInt32();

					char[] versionStr = r.ReadChars(versionLen);

					// read the .NET framework version required from the meta-data
					//Note: we only read the first 2 numbers of the version - if you want to
					//      detect beta vs. rc vs. rtm, then read the whole version.
					// We assume no one will be stupid enough to use beta created exes/dlls in the wild.
					if (versionStr[1] == '2' && versionStr[3] == '0')
						assembInfo.FrameworkVersion = FrameworkVersion.Net2_0;
					else if (versionStr[1] == '4' && versionStr[3] == '0')
						assembInfo.FrameworkVersion = FrameworkVersion.Net4_0;
					else
						assembInfo.FrameworkVersion = FrameworkVersion.Unknown;
				}
			}

			return assembInfo;
		}
	}
}
