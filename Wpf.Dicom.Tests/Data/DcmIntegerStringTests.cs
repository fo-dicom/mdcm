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

using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace Dicom.Data
{
    [TestFixture]
    public class DcmIntegerStringTests
    {
        #region Fields

        private DcmIntegerString _instance;

        #endregion

        #region SetUp and TearDown

        [SetUp]
        public void Setup()
        {
            var rnd = new Randomizer();
            _instance = new DcmIntegerString(DicomTags.ReferencedFrameNumber);
            _instance.SetValues(
                rnd.GetInts(0, 65535, 5000).Select(i => i.ToString(CultureInfo.InvariantCulture)).
                Concat(new[] { "32767" }).ToArray());
        }

        [TearDown]
        public void Teardown()
        {
            _instance = null;
        }

        #endregion

        #region Unit tests

        [Test]
        public void GetInt32s_LastElement_ReturnsCorrectValue()
        {
            const int expected = 32767;
            var actual = _instance.GetInt32s().Last();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetInt32List_LastElement_ReturnsCorrectValue()
        {
            const int expected = 32767;
            var actual = _instance.GetInt32List().Last();
            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
