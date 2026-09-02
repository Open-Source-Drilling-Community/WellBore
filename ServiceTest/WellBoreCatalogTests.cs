using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service.Controllers;
using OSDC.Drilling.WellBore.Service.Managers;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public sealed class WellBoreCatalogTests
{
    [Test]
    public void Defaults_assignments_and_reference_safe_deletion_work_together()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"WellBoreCatalog_{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={path}";
        ILoggerFactory factory = LoggerFactory.Create(builder => builder.ClearProviders());
        try
        {
            var connections = new SqlConnectionManager(connectionString, factory.CreateLogger<SqlConnectionManager>());
            var identityManager = WellBoreIdentityManager.GetInstance(factory.CreateLogger<WellBoreIdentityManager>(), connections);
            var featureManager = WellBoreFeatureCategoryManager.GetInstance(factory.CreateLogger<WellBoreFeatureCategoryManager>(), connections);

            List<WellBoreIdentity> identities = identityManager.GetAllWellBoreIdentity()!.Cast<WellBoreIdentity>().ToList();
            List<WellBoreFeatureCategory> categories = featureManager.GetAllWellBoreFeatureCategory()!.Cast<WellBoreFeatureCategory>().ToList();

            Assert.That(identities.Select(value => value.Name), Is.EquivalentTo(new[]
            {
                "OfficialAuthorityName", "OperatorName", "CompanyInternalName", "PlanningName",
                "DataManagementName", "HistoricalName", "ShortName", "DisplayName",
                "ReportingName", "LegacyName", "ImportedName"
            }));
            Assert.That(categories.Select(value => value.Name), Is.EquivalentTo(new[]
            {
                "WellboreRole", "WellboreOrigin", "SidetrackReason", "WellboreGeometryClass",
                "WellboreTrajectoryIntent", "WellboreConstructionStatus", "WellboreSectionContext",
                "WellboreCompletionContext", "WellboreDataAvailability", "WellboreHazard", "SidetrackClassification"
            }));
            Assert.That(categories.Single(value => value.Name == "SidetrackReason").IsExclusive, Is.False);
            Assert.That(categories.Single(value => value.Name == "WellboreConstructionStatus").HasValidityPeriod, Is.True);
            WellBoreFeatureCategory classification = categories.Single(value => value.Name == "SidetrackClassification");
            Assert.That(classification.IsExclusive, Is.True);
            Assert.That(classification.HasValidityPeriod, Is.False);
            Assert.That(classification.Options!.Select(value => value.Name),
                Is.EquivalentTo(new[] { "Technical", "Production", "Appraisal", "Lateral", "Unknown" }));

            WellBoreIdentity identity = identities.Single(value => value.Name == "OfficialAuthorityName");
            WellBoreFeatureCategory category = categories.Single(value => value.Name == "WellboreRole");
            WellBoreFeatureOption option = category.Options!.Single(value => value.Name == "MainBore");
            Guid wellBoreId = Guid.NewGuid();
            var wellBore = new Model.WellBore
            {
                MetaInfo = new MetaInfo { ID = wellBoreId },
                Name = "catalog-reference-test",
                WellBoreIdentityAssignments =
                [
                    new WellBoreIdentityAssignment { ID = Guid.NewGuid(), IdentityID = identity.MetaInfo!.ID, Value = "NPD O'Brien-1" }
                ],
                WellBoreFeatureAssignments =
                [
                    new WellBoreFeatureAssignment { ID = Guid.NewGuid(), FeatureCategoryID = category.MetaInfo!.ID, FeatureOptionID = option.ID }
                ]
            };
            WellBoreManager wellBoreManager = WellBoreManager.GetInstance(factory.CreateLogger<WellBoreManager>(), connections);
            Assert.That(wellBoreManager.AddWellBore(wellBore), Is.True);
            Assert.That(wellBoreManager.GetWellBoreById(wellBoreId)!.WellBoreIdentityAssignments, Has.Count.EqualTo(1));
            Assert.That(wellBoreManager.GetWellBoreById(wellBoreId)!.WellBoreIdentityAssignments![0].Value,
                Is.EqualTo("NPD O'Brien-1"));
            Assert.That(wellBoreManager.GetWellBoreById(wellBoreId)!.WellBoreFeatureAssignments, Has.Count.EqualTo(1));

            var identityController = new WellBoreIdentityController(factory.CreateLogger<WellBoreIdentityManager>(), connections);
            var featureController = new WellBoreFeatureCategoryController(factory.CreateLogger<WellBoreFeatureCategoryManager>(), connections);
            Assert.That(identityController.PutWellBoreIdentityById(identity.MetaInfo.ID,
                identity.LastModificationDate!.Value.AddMinutes(-1), identity),
                Is.TypeOf<ConflictObjectResult>());
            var categoryWithoutReferencedOption = new WellBoreFeatureCategory
            {
                MetaInfo = category.MetaInfo,
                Name = category.Name,
                IsExclusive = category.IsExclusive,
                HasValidityPeriod = category.HasValidityPeriod,
                Options = category.Options!.Where(value => value.ID != option.ID).ToList(),
                CreationDate = category.CreationDate,
                LastModificationDate = category.LastModificationDate
            };
            Assert.That(featureController.PutWellBoreFeatureCategoryById(category.MetaInfo.ID,
                category.LastModificationDate!.Value, categoryWithoutReferencedOption), Is.TypeOf<ConflictObjectResult>());
            Assert.That(identityController.DeleteWellBoreIdentityById(identity.MetaInfo.ID,
                identity.LastModificationDate.Value), Is.TypeOf<ConflictObjectResult>());
            Assert.That(featureController.DeleteWellBoreFeatureCategoryById(category.MetaInfo.ID,
                category.LastModificationDate.Value), Is.TypeOf<ConflictObjectResult>());
        }
        finally
        {
            factory.Dispose();
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
