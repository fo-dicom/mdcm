using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using Dicom.Data;

namespace Dicom.Tests.Data {
	[TestFixture]
	public class DicomTagTest {

		[Test]
		public void ConstructorSimple() {
			var t = new DicomTag(0x0028, 0x1203);
			Assert.AreEqual(t.Group, 0x0028);
			Assert.AreEqual(t.Element, 0x1203);
			Assert.IsEmpty(t.PrivateCreator);
		}

		[Test]
		public void ConstructorPrivateCreator() {
			var t = new DicomTag(0x2000, 0x2001, "TEST CREATOR");
			Assert.AreEqual(0x2000, t.Group);
			Assert.AreEqual(0x2001, t.Element);
			Assert.AreEqual("TEST CREATOR", t.PrivateCreator);
		}

		[Test]
		public void PrivateTest() {
			var t = new DicomTag(0x1234, 0x5678);
			Assert.IsFalse(t.IsPrivate);
			t = new DicomTag(0x6789, 0x5678);
			Assert.IsTrue(t.IsPrivate);
		}

		[Test]
		public void ToString() {
			var t = new DicomTag(0x0028, 0x9145);
			Assert.AreEqual("(0028,9145)", t.ToString());
		}

		[Test]
		public void Equals() {
			var a = new DicomTag(0x0028, 0x9145);
			var b = new DicomTag(0x0028, 0x9146);
			var c = new DicomTag(0x0028, 0x9146);
			var z = default(DicomTag);
			Assert.IsFalse(a.Equals(b));
			Assert.IsFalse(b.Equals(a));
			Assert.IsTrue(b.Equals(c));
			Assert.IsTrue(c.Equals(b));
			Assert.IsFalse(a.Equals(z));
			Assert.IsFalse(b.Equals(z));
		}

		[Test]
		public void EqualsObject() {
			var a = new DicomTag(0x0028, 0x9145);
			var b = new DicomTag(0x0028, 0x9146);
			var c = new DicomTag(0x0028, 0x9146);
			var z = default(DicomTag);
			Assert.IsFalse(a.Equals((object)b));
			Assert.IsFalse(b.Equals((object)a));
			Assert.IsTrue(b.Equals((object)c));
			Assert.IsTrue(c.Equals((object)b));
			Assert.IsFalse(a.Equals((object)z));
			Assert.IsFalse(b.Equals((object)z));
		}

		[Test]
		public void OpEq() {
			var a = new DicomTag(0x0028, 0x9145);
			var b = new DicomTag(0x0028, 0x9146);
			var c = new DicomTag(0x0028, 0x9146);
			var z = default(DicomTag);
			Assert.IsFalse(a == b);
			Assert.IsFalse(b == a);
			Assert.IsTrue(b == c);
			Assert.IsTrue(c == b);
			Assert.IsFalse(z == a);
			Assert.IsFalse(a == z);
			Assert.IsFalse(z == b);
			Assert.IsFalse(b == z);
		}

		[Test]
		public void OpNEq() {
			var a = new DicomTag(0x0028, 0x9145);
			var b = new DicomTag(0x0028, 0x9146);
			var c = new DicomTag(0x0028, 0x9146);
			var z = default(DicomTag);
			Assert.IsTrue(a != b);
			Assert.IsTrue(b != a);
			Assert.IsFalse(b != c);
			Assert.IsFalse(c != b);
			Assert.IsTrue(z != a);
			Assert.IsTrue(a != z);
			Assert.IsTrue(z != b);
			Assert.IsTrue(b != z);
		}

		[Test]
		public void Parse() {
			DicomTag tag = null;

			tag = DicomTag.Parse("00100020");
			Assert.AreEqual(DicomTags.PatientID.Card, tag.Card);

			tag = DicomTag.Parse("0010,0020");
			Assert.AreEqual(DicomTags.PatientID.Card, tag.Card);

			tag = DicomTag.Parse("(0010,0020)");
			Assert.AreEqual(DicomTags.PatientID.Card, tag.Card);
		}
	}
}
