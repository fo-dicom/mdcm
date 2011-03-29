using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Data {
	public static class DcmEncoding {
		private static Dictionary<string, int> EncodingCodePageMap;

		static DcmEncoding() {
			EncodingCodePageMap = new Dictionary<string, int>();
			EncodingCodePageMap.Add("ISO_IR 100", 28591); // Latin Alphabet No. 1 Unextended
            EncodingCodePageMap.Add("ISO_IR 101", 28592); // Latin Alphabet No. 2 Unextended
            EncodingCodePageMap.Add("ISO_IR 109", 28593); // Latin Alphabet No. 3 Unextended
            EncodingCodePageMap.Add("ISO_IR 110", 28594); // Latin Alphabet No. 4 Unextended
            EncodingCodePageMap.Add("ISO_IR 144", 28595); // Cyrillic Unextended
            EncodingCodePageMap.Add("ISO_IR 127", 28596); // Arabic Unextended
            EncodingCodePageMap.Add("ISO_IR 126", 28597); // Greek Unextended
            EncodingCodePageMap.Add("ISO_IR 138", 28598); // Hebrew Unextended
            EncodingCodePageMap.Add("ISO_IR 148", 28599); // Latin Alphabet No. 5 (Turkish) Unextended
            EncodingCodePageMap.Add("ISO_IR 13", 932); // JIS X 0201 (Shift JIS) Unextended
            EncodingCodePageMap.Add("ISO_IR 166", 874); // TIS 620-2533 (Thai) Unextended
            EncodingCodePageMap.Add("ISO_IR 192", 65001); // Unicode in UTF-8
            EncodingCodePageMap.Add("ISO 2022 IR 6", 20127); // ASCII
            EncodingCodePageMap.Add("ISO 2022 IR 100", 28591); // Latin Alphabet No. 1 Extended
            EncodingCodePageMap.Add("ISO 2022 IR 101", 28592); // Latin Alphabet No. 2 Extended
            EncodingCodePageMap.Add("ISO 2022 IR 109", 28593); // Latin Alphabet No. 3 Extended
            EncodingCodePageMap.Add("ISO 2022 IR 110", 28594); // Latin Alphabet No. 4 Extended
            EncodingCodePageMap.Add("ISO 2022 IR 144", 28595); // Cyrillic Extended
            EncodingCodePageMap.Add("ISO 2022 IR 127", 28596); // Arabic Extended
            EncodingCodePageMap.Add("ISO 2022 IR 126", 28597); // Greek Extended
            EncodingCodePageMap.Add("ISO 2022 IR 138", 28598); // Hebrew Extended
            EncodingCodePageMap.Add("ISO 2022 IR 148", 28599); // Latin Alphabet No. 5 (Turkish) Extended
            EncodingCodePageMap.Add("ISO 2022 IR 13", 50222); // JIS X 0201 (Shift JIS) Extended
            EncodingCodePageMap.Add("ISO 2022 IR 166", 874); // TIS 620-2533 (Thai) Extended
            EncodingCodePageMap.Add("ISO 2022 IR 87", 50222); // JIS X 0208 (Kanji) Extended
            EncodingCodePageMap.Add("ISO 2022 IR 159", 50222); // JIS X 0212 (Kanji) Extended
            EncodingCodePageMap.Add("ISO 2022 IR 149", 20949); // KS X 1001 (Hangul and Hanja) Extended
			EncodingCodePageMap.Add("GB18030", 54936); // Chinese (Simplified) Extended
		}

		/// <summary>
		/// Default charset encoding (ISO 2022 IR 6)
		/// </summary>
		public static Encoding Default {
			get
			{
#if SILVERLIGHT
			    return Encoding.UTF8;
#else
			    return Encoding.ASCII;
#endif
			}
		}

		public static Encoding GetEncodingForSpecificCharacterSet(string encoding) {
#if SILVERLIGHT
		    return Encoding.GetEncoding(encoding);
#else
			int codePage;
			if (EncodingCodePageMap.TryGetValue(encoding, out codePage))
				return Encoding.GetEncoding(codePage);
			return Default;
#endif
		}

		public static string GetSpecificCharacterSetForEncoding(Encoding encoding) {
#if SILVERLIGHT
		    return encoding != null ? encoding.WebName : Encoding.UTF8.WebName;
#else
			if (encoding == null)
				encoding = Encoding.ASCII;

			foreach (KeyValuePair<string, int> codePage in EncodingCodePageMap) {
				if (codePage.Value == encoding.CodePage)
					return codePage.Key;
			}

			throw new DicomDataException(
				String.Format("Unable to find specific character set value for encoding: {0} ({1})", 
					encoding.EncodingName, encoding.CodePage));
#endif
        }
	}
}
