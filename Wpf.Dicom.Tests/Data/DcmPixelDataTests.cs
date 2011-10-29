/*
 * Copyright (c) 2011  Anders Gustafsson, Cureos AB.
 * This file is part of mdcm.
 *
 * mdcm is free software: you can redistribute it and/or 
 * modify it under the terms of the GNU Lesser General Public 
 * License as published by the Free Software Foundation, either 
 * version 3 of the License, or (at your option) any later version.
 *
 * mdcm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public 
 * License along with mdcm.  
 * If not, see <http://www.gnu.org/licenses/>.
 */

using NUnit.Framework;

namespace Dicom.Data
{
    [TestFixture]
    public class DcmPixelDataTests
    {
        private DcmPixelData _instance;

        [SetUp]
        public void Init()
        {
            var dicomFile = new DicomFileFormat();
            dicomFile.Load(@"Testdata\CT-MONO2-16-chest", DicomReadOptions.Default);
            _instance = new DcmPixelData(dicomFile.Dataset);
        }

        [Test]
        public void GetFrameDataU8_CompareOldWithNewImpl_ReturnsSameResults()
        {
            var expected = _instance.GetFrameDataU8_(0);
            var actual = _instance.GetFrameDataU8(0);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void GetFrameDataU16_CompareOldWithNewImpl_ReturnsSameResults()
        {
            var expected = _instance.GetFrameDataU16_(0);
            var actual = _instance.GetFrameDataU16(0);
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
