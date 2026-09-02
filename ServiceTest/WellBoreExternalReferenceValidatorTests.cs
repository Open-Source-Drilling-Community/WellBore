using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public sealed class WellBoreExternalReferenceValidatorTests
{
    [Test]
    public async Task Existing_references_are_valid_and_distinct_reads_are_cached_per_batch()
    {
        Guid wellId = Guid.NewGuid();
        Guid rigId = Guid.NewGuid();
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("/Well/")
            ? Resource(wellId) : Resource(rigId));
        WellBoreExternalReferenceValidator validator = CreateValidator(handler);

        IReadOnlyList<WellBoreExternalReferenceValidation> results = await validator.ValidateAsync(
            [WellBore(wellId, rigId), WellBore(wellId, rigId)], CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(results.All(value => value.Status == WellBoreExternalReferenceValidationStatus.Valid), Is.True);
            Assert.That(results.All(value => value.WellExists == true && value.RigExists == true), Is.True);
            Assert.That(handler.CallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Missing_well_is_invalid_while_existing_rig_remains_confirmed()
    {
        Guid wellId = Guid.NewGuid();
        Guid rigId = Guid.NewGuid();
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("/Well/")
            ? new HttpResponseMessage(HttpStatusCode.NotFound) : Resource(rigId));

        WellBoreExternalReferenceValidation result = (await CreateValidator(handler)
            .ValidateAsync([WellBore(wellId, rigId)], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WellBoreExternalReferenceValidationStatus.Invalid));
            Assert.That(result.WellExists, Is.False);
            Assert.That(result.RigExists, Is.True);
            Assert.That(result.Issues.Select(value => value.Code), Does.Contain("well_not_found"));
        });
    }

    [Test]
    public async Task Dependency_failure_is_unavailable_not_invalid()
    {
        Guid wellId = Guid.NewGuid();
        Guid rigId = Guid.NewGuid();
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("/Rig/")
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Resource(wellId));

        WellBoreExternalReferenceValidation result = (await CreateValidator(handler)
            .ValidateAsync([WellBore(wellId, rigId)], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WellBoreExternalReferenceValidationStatus.Unavailable));
            Assert.That(result.WellExists, Is.True);
            Assert.That(result.RigExists, Is.Null);
            Assert.That(result.Issues.Select(value => value.Code), Does.Contain("rig_service_error"));
        });
    }

    [Test]
    public async Task Mismatched_dependency_payload_is_unavailable_not_missing()
    {
        Guid wellId = Guid.NewGuid();
        var handler = new StubHandler(_ => Resource(Guid.NewGuid()));

        WellBoreExternalReferenceValidation result = (await CreateValidator(handler)
            .ValidateAsync([WellBore(wellId, Guid.NewGuid())], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WellBoreExternalReferenceValidationStatus.Unavailable));
            Assert.That(result.Issues.Select(value => value.Code), Does.Contain("well_response_invalid"));
            Assert.That(result.Issues.Select(value => value.Code), Does.Contain("rig_response_invalid"));
        });
    }

    [Test]
    public async Task WellBore_without_external_references_is_valid_without_http_calls()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var wellBore = new WellBoreModel { MetaInfo = new MetaInfo { ID = Guid.NewGuid() } };

        WellBoreExternalReferenceValidation result = (await CreateValidator(handler)
            .ValidateAsync([wellBore], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WellBoreExternalReferenceValidationStatus.Valid));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static WellBoreExternalReferenceValidator CreateValidator(HttpMessageHandler handler)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WellHostURL"] = "https://well.test/", ["RigHostURL"] = "https://rig.test/"
        }).Build();
        return new WellBoreExternalReferenceValidator(new StubClientFactory(handler), configuration);
    }

    private static WellBoreModel WellBore(Guid wellId, Guid rigId) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, WellID = wellId, RigID = rigId
    };

    private static HttpResponseMessage Resource(Guid id) => new(HttpStatusCode.OK)
    {
        Content = new StringContent($"{{\"MetaInfo\":{{\"ID\":\"{id}\"}}}}", Encoding.UTF8, "application/json")
    };

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(response(request));
        }
    }
}
