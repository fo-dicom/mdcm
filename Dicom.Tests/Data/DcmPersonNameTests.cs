// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

using System;
using Dicom.Data;
using NUnit.Framework;

namespace Dicom.Tests.Data
{
    [TestFixture]
    public class DcmPersonNameTests
    {
        #region Fields

        private DcmPersonName _instanceAdams;
        private DcmPersonName _instanceJones;
        private DcmPersonName _instanceDoe;

        #endregion

        #region SetUp and TearDown

        [SetUp]
        public void Setup()
        {
            _instanceAdams = new DcmPersonName(DicomTags.PatientsName);
            _instanceAdams.SetValue("Adams^John Robert Quincy^^Rev.^B.A. M.Div.");
            _instanceJones = new DcmPersonName(DicomTags.PersonName);
            _instanceJones.SetValue("Morrison-Jones^Susan^^^Ph.D., Chief Executive Officer");
            _instanceDoe = new DcmPersonName(DicomTags.ReferringPhysiciansName);
            _instanceDoe.SetValue("Doe^John");
        }

        [TearDown]
        public void Teardown()
        {
            _instanceAdams = null;
        }

        #endregion

        #region Unit tests

        [Test]
        public void FamilyNameComplex_Accessor_AllInstancesNonEmpty()
        {
            StringAssert.AreEqualIgnoringCase("Adams", _instanceAdams.FamilyNameComplex);
            StringAssert.AreEqualIgnoringCase("Morrison-Jones", _instanceJones.FamilyNameComplex);
            StringAssert.AreEqualIgnoringCase("Doe", _instanceDoe.FamilyNameComplex);
        }

        [Test]
        public void GivenNameComplex_Accessor_AllInstancesNonEmpty()
        {
            StringAssert.AreEqualIgnoringCase("John Robert Quincy", _instanceAdams.GivenNameComplex);
            StringAssert.AreEqualIgnoringCase("Susan", _instanceJones.GivenNameComplex);
            StringAssert.AreEqualIgnoringCase("John", _instanceDoe.GivenNameComplex);
        }

        [Test]
        public void MiddleName_Accessor_AllInstancesEmpty()
        {
            Assert.IsNullOrEmpty(_instanceAdams.MiddleName);
            Assert.IsNullOrEmpty(_instanceJones.MiddleName);
            Assert.IsNullOrEmpty(_instanceDoe.MiddleName);
        }

        [Test]
        public void Prefix_Accessor_AdamsInstanceNonEmpty()
        {
            StringAssert.AreEqualIgnoringCase("Rev.", _instanceAdams.NamePrefix);
            Assert.IsNullOrEmpty(_instanceJones.NamePrefix);
            Assert.IsNullOrEmpty(_instanceDoe.NamePrefix);
        }

        [Test]
        public void Suffix_Accessor_DoeInstanceEmpty()
        {
            StringAssert.AreEqualIgnoringCase("B.A. M.Div.", _instanceAdams.NameSuffix);
            StringAssert.AreEqualIgnoringCase("Ph.D., Chief Executive Officer", _instanceJones.NameSuffix);
            Assert.IsNullOrEmpty(_instanceDoe.NamePrefix);
        }

        #endregion
    }
}
