using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.WellBore.Model
{
    public class WellBoreFeatureCategory : IFeatureCategory
    {
        /// <summary>
        /// a MetaInfo for the WellBoreFeatureCategory
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// user-defined name of the category
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// whether options from this category are mutually exclusive when assigned to a wellbore
        /// </summary>
        public bool IsExclusive { get; set; }

        /// <summary>
        /// whether wellbore assignments from this category carry a validity period
        /// </summary>
        public bool HasValidityPeriod { get; set; }

        /// <summary>
        /// the possible options for this category
        /// </summary>
        public List<WellBoreFeatureOption>? Options { get; set; }

        List<IFeatureOption>? IFeatureCategory.Options
        {
            get => Options?.Cast<IFeatureOption>().ToList();
            set => Options = value?.Select(option => option is WellBoreFeatureOption wellOption
                ? wellOption
                : new WellBoreFeatureOption
                {
                    ID = option.ID,
                    Name = option.Name
                }).ToList();
        }

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
        public WellBoreFeatureCategory() : base()
        {
        }
    }
}


