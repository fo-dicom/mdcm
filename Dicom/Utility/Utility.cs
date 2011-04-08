using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

#if SILVERLIGHT
using Ionic.Zlib;
using MD5 = System.Security.Cryptography.SHA1;
using MD5CryptoServiceProvider = System.Security.Cryptography.SHA1Managed;
using SHA1CryptoServiceProvider = System.Security.Cryptography.SHA1Managed;
#else
using System.IO.Compression;
#endif

namespace Dicom.Utility {
	/// <summary>
	/// Utility methods for manipulating a stream
	/// </summary>
	public static class StreamUtility {
		/// <summary>
		/// Copies data from source stream to destination stream
		/// </summary>
		/// <param name="src">Source stream</param>
		/// <param name="dst">Destination stream</param>
		public static void Copy(Stream src, Stream dst) {
			byte[] buffer = new byte[65536];
			int read = 0;
			do {
				read = src.Read(buffer, 0, buffer.Length);
				dst.Write(buffer, 0, read);
			} while (read == buffer.Length);
		}

		/// <summary>
		/// Compresses or decompressed a stream using the Deflate algorithm
		/// </summary>
		/// <param name="src">Source stream</param>
		/// <param name="compress">Compress or decompress</param>
		/// <returns>Output stream</returns>
		public static MemoryStream Deflate(Stream src, bool compress) {
			MemoryStream ms = new MemoryStream();
			Deflate(src, ms, compress);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		/// <summary>
		/// Compresses or decompressed a stream using the Deflate algorithm
		/// </summary>
		/// <param name="src">Source stream</param>
		/// <param name="dst">Destination stream</param>
		/// <param name="compress">Compress or decompress</param>
		public static void Deflate(Stream src, Stream dst, bool compress) {
			DeflateStream ds = new DeflateStream(src, compress ? CompressionMode.Compress : CompressionMode.Decompress);
			Copy(ds, dst);
        }
	}

	/// <summary>
	/// Utility methods for manipulating an array
	/// </summary>
	public static class ArrayUtility {
		/// <summary>
		/// Shuffles an array
		/// </summary>
		/// <param name="array">Array</param>
		public static void Shuffle(Array array) {
			Random rand = new Random();
			int n = array.Length;
			while (--n > 0) {
				int k = rand.Next(n + 1);
				Swap(array, n, k);
			}
		}

		/// <summary>
		/// Swaps 2 items in an array
		/// </summary>
		/// <param name="array">Array</param>
		/// <param name="source">Source item</param>
		/// <param name="destination">Destination item</param>
		public static void Swap(Array array, int source, int destination) {
			object obj = array.GetValue(source);
			array.SetValue(array.GetValue(destination), source);
			array.SetValue(obj, destination);
		}
	}

	/// <summary>
	/// Utility methods for manipulating a byte array
	/// </summary>
	public static class ByteUtility {
		/// <summary>
		/// Gets MD5 hash of a byte array
		/// </summary>
		/// <param name="buffer">Byte array</param>
		/// <returns>MD5 hash as string</returns>
		public static string MD5(byte[] buffer) {
			MD5 md5 = new MD5CryptoServiceProvider();
			return BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", "");
		}

		/// <summary>
		/// Gets SHA1 hash of a byte array
		/// </summary>
		/// <param name="buffer">Byte array</param>
		/// <returns>SHA1 hash as string</returns>
		public static string SHA1(byte[] buffer) {
			SHA1 sha1 = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(sha1.ComputeHash(buffer)).Replace("-", "");
		}
	}

	/// <summary>
	/// Utility methods for manipulating a string
	/// </summary>
	public static class StringUtility {
		/// <summary>
		/// Gets MD5 hash of a string
		/// </summary>
		/// <param name="str">String</param>
		/// <returns>MD5 hash as string</returns>
        public static string MD5(string str)
		{
#if SILVERLIGHT
		    byte[] bytes = Encoding.UTF8.GetBytes(str);
#else
			byte[] bytes = ASCIIEncoding.Default.GetBytes(str);
#endif
		    return ByteUtility.MD5(bytes);
		}

	    /// <summary>
		/// Gets SHA1 hash of a string
		/// </summary>
		/// <param name="str">String</param>
		/// <returns>SHA1 hash as string</returns>
		public static string SHA1(string str) {
#if SILVERLIGHT
            byte[] bytes = Encoding.UTF8.GetBytes(str);
#else
			byte[] bytes = ASCIIEncoding.Default.GetBytes(str);
#endif
            return ByteUtility.SHA1(bytes);
		}

		public static string RemoveInvalidPathChars(string path) {
#if SILVERLIGHT
            return String.Join("_", path.Split(Path.GetInvalidPathChars()));
#else
			return String.Join("_", path.Split(Path.GetInvalidFileNameChars()));
#endif
                                                                 }
	}

	public static class DateUtility {
		private static DateTime SqlMinDate = new DateTime(1900, 01, 01);
		private static DateTime SqlMaxDate = new DateTime(2079, 06, 06);

		public static DateTime ClipSqlDate(DateTime date) {
			if (date < SqlMinDate)
				return SqlMinDate;
			if (date > SqlMaxDate)
				return SqlMaxDate;
			return date;
		}

		public static DateTime? ClipSqlDate(DateTime? date) {
			if (date == null)
				return null;
			if (date < SqlMinDate)
				return SqlMinDate;
			if (date > SqlMaxDate)
				return SqlMaxDate;
			return date;
		}

		public const int SecondsInMinute = 60;

		public const int MinutesInHour = 60;
		public const int SecondsInHour = MinutesInHour * SecondsInMinute;

		public const int HoursInDay = 24;
		public const int MinutesInDay = HoursInDay * SecondsInHour;
		public const int SecondsInDay = MinutesInDay * SecondsInMinute;

		public const int DaysInWeek = 7;
		public const int HoursInWeek = DaysInWeek * HoursInDay;
		public const int MinutesInWeek = HoursInWeek * MinutesInHour;
		public const int SecondsInWeek = MinutesInWeek * SecondsInMinute;

		public static string ETA(DateTime expire) {
			int seconds = expire.Subtract(DateTime.Now).Seconds;
			if (seconds < 1)
				return String.Empty;
			if (seconds >= SecondsInWeek) {
				int weeks = seconds / SecondsInWeek;
				seconds %= SecondsInWeek;
				int days = seconds / SecondsInDay;
				if (days > 0)
					return String.Format("{0}w {1}d", weeks, days);
				return String.Format("{0}w", weeks);
			}
			if (seconds >= SecondsInDay) {
				int days = seconds / SecondsInDay;
				seconds %= SecondsInDay;
				int hours = seconds / SecondsInHour;
				if (hours > 0)
					return String.Format("{0}d {1}h", days, hours);
				return String.Format("{0}d", days);
			}
			if (seconds >= SecondsInHour) {
				int hours = seconds / SecondsInHour;
				seconds %= SecondsInHour;
				int minutes = seconds / SecondsInMinute;
				if (minutes > 0)
					return String.Format("{0}h {1}m", hours, minutes);
				return String.Format("{0}h", hours);
			}
			if (seconds >= SecondsInMinute) {
				int minutes = seconds / SecondsInMinute;
				seconds %= SecondsInMinute;
				if (seconds > 0)
					return String.Format("{0}m {1}s", minutes, seconds);
				return String.Format("{0}m", minutes);
			}
			return String.Format("{0}s", seconds);
		}
	}

	public static class MiscUtility {
		public static void Swap(ref int i1, ref int i2) {
			int it = i1;
			i1 = i2;
			i2 = it;
		}
	}
}
