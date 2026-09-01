using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.WellBore.Model
{
    public class WellBoreIdentityAssignment : IIdentityAssignment
    {
        /// <summary>
        /// unique ID of the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// reference to the selected WellBoreIdentity
        /// </summary>
        public Guid? IdentityID { get; set; }

        /// <summary>
        /// wellbore-specific identity value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public WellBoreIdentityAssignment() : base()
        {
        }
    }
}


