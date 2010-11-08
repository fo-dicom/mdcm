using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using Dicom.Data;

namespace Dicom.Tests.Data {
	[TestFixture]
	public class DicomTagTest {
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
