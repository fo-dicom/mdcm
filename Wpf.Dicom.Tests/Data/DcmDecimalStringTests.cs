/*
 * Copyright (c) 2012  Anders Gustafsson, Cureos AB.
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

using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Dicom.Data
{
    [TestFixture]
    public class DcmDecimalStringTests
    {
        #region Fields

        private DcmDecimalString[] _instances;

        #endregion

        #region SetUp and TearDown

        [SetUp]
        public void Setup()
        {
            var ff = new DicomFileFormat();
            ff.Load("Testdata/rtss.dcm", DicomReadOptions.DefaultWithoutDeferredLoading);
            _instances = ff.Dataset.GetSQ(DicomTags.ROIContourSequence).SequenceItems.SelectMany(
                item =>
                (item.Dataset.GetSQ(DicomTags.ContourSequence) ?? new DcmItemSequence(DicomTags.ContourSequence)).
                    SequenceItems).Select(item => item.Dataset.GetDS(DicomTags.ContourData)).ToArray();
        }

        [TearDown]
        public void Teardown()
        {
            _instances = null;
        }

        #endregion

        #region Unit tests

        [Test]
        public void GetFloats_ContourData_IndividualElementCorrectlyRead()
        {
            const float expected = -336.55f;
            var actual = _instances[0].GetFloats()[13];
            Assert.AreEqual(expected, actual, 1.0e-3);
        }

        [Test]
        public void GetFloats_ReadAllContourData_Timing()
        {
            var timer = new Stopwatch();
            timer.Restart();
            foreach (var ds in _instances)
            {
                var vertices = ds.GetFloats();
            }
            timer.Stop();

            Assert.Pass("Elapsed time: {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        public void GetFloatList_ContourData_IndividualElementCorrectlyRead()
        {
            const float expected = -336.55f;
            var actual = _instances[0].GetFloatList()[13];
            Assert.AreEqual(expected, actual, 1.0e-3);
        }

        [Test]
        public void GetFloatList_ReadAllContourData_Timing()
        {
            var timer = new Stopwatch();
            timer.Restart();
            foreach (var ds in _instances)
            {
                var vertices = ds.GetFloatList();
            }
            timer.Stop();

            Assert.Pass("Elapsed time: {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        public void GetDoubles_ContourData_IndividualElementCorrectlyRead()
        {
            const double expected = -336.55;
            var actual = _instances[0].GetDoubles()[13];
            Assert.AreEqual(expected, actual, 1.0e-7);
        }

        [Test]
        public void GetDoubles_ReadAllContourData_Timing()
        {
            var timer = new Stopwatch();
            timer.Restart();
            foreach (var ds in _instances)
            {
                var vertices = ds.GetDoubles();
            }
            timer.Stop();

            Assert.Pass("Elapsed time: {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        public void GetDoubleList_ContourData_IndividualElementCorrectlyRead()
        {
            const double expected = -336.55;
            var actual = _instances[0].GetDoubleList()[13];
            Assert.AreEqual(expected, actual, 1.0e-7);
        }

        [Test]
        public void GetDoubleList_ReadAllContourData_Timing()
        {
            var timer = new Stopwatch();
            timer.Restart();
            foreach (var ds in _instances)
            {
                var vertices = ds.GetDoubleList();
            }
            timer.Stop();

            Assert.Pass("Elapsed time: {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        public void GetDecimals_ContourData_IndividualElementCorrectlyRead()
        {
            const decimal expected = -336.55m;
            var actual = _instances[0].GetDecimals()[13];
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetDecimals_ReadAllContourData_Timing()
        {
            var timer = new Stopwatch();
            timer.Restart();
            foreach (var ds in _instances)
            {
                var vertices = ds.GetDecimals();
            }
            timer.Stop();

            Assert.Pass("Elapsed time: {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        public void GetDecimalList_ContourData_IndividualElementCorrectlyRead()
        {
            const decimal expected = -336.55m;
            var actual = _instances[0].GetDecimalList()[13];
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetDecimalList_ReadAllContourData_Timing()
        {
            var timer = new Stopwatch();
            timer.Restart();
            foreach (var ds in _instances)
            {
                var vertices = ds.GetDecimalList();
            }
            timer.Stop();

            Assert.Pass("Elapsed time: {0} ms", timer.ElapsedMilliseconds);
        }

        [Test]
        public void GetDecimal_ContourData_IndividualElementCorrectlyRead()
        {
            const decimal expected = 0.54m;
            var actual = _instances[0].GetDecimal(612);
            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
