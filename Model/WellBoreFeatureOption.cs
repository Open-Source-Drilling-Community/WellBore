using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.WellBore.Model
{
    public class WellBoreFeatureOption : IFeatureOption
    {
        /// <summary>
        /// stable identifier for the option inside its category
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// user-defined name of the option
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public WellBoreFeatureOption() : base()
        {
        }
    }
}


