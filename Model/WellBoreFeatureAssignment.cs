using System;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.WellBore.Model
{
    public class WellBoreFeatureAssignment : IFeatureAssignment
    {
        /// <summary>
        /// stable identifier for the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// the selected well feature category
        /// </summary>
        public Guid? FeatureCategoryID { get; set; }

        /// <summary>
        /// the selected well feature option
        /// </summary>
        public Guid? FeatureOptionID { get; set; }

        /// <summary>
        /// first date for which the assignment is valid
        /// </summary>
        public DateTimeOffset? FromDate { get; set; }

        /// <summary>
        /// last date for which the assignment is valid
        /// </summary>
        public DateTimeOffset? ToDate { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public WellBoreFeatureAssignment() : base()
        {
        }
    }
}


