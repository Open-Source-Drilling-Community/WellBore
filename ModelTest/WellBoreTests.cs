using NUnit.Framework;
using NORCE.Drilling.WellBore.Model;
using System;

namespace NORCE.Drilling.WellBore.ModelTest
{
    public class WellBoreTests
    {
        [Test]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var wb = new Model.WellBore();

            Assert.That(wb.MetaInfo, Is.Null);
            Assert.That(wb.Name, Is.Null);
            Assert.That(wb.Description, Is.Null);
            Assert.That(wb.CreationDate, Is.Null);
            Assert.That(wb.LastModificationDate, Is.Null);
            Assert.That(wb.WellID, Is.Null);
            Assert.That(wb.IsSidetrack, Is.False);
            Assert.That(wb.ParentWellBoreID, Is.Null);
            Assert.That(wb.TieInPointAlongHoleDepth, Is.Null);
            Assert.That(wb.SidetrackType, Is.EqualTo(SidetrackType.Undefined));
        }
    }
}

