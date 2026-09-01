using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.WellBore.Model
{
    public class WellBoreIdentity : IIdentity
    {
        /// <summary>
        /// a MetaInfo for the WellBoreIdentity
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// symbolic name of the identity
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// the date when the data was created
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        public DateTimeOffset? LastModificationDate { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public WellBoreIdentity() : base()
        {
        }
    }
}


