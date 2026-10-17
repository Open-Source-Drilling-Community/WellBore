using System.Net;
using System.Text;
using OSDC.Drilling.WellBore.ModelShared;

namespace OSDC.Drilling.WellBore.ServiceTest;

public class RigClientSerializationTests
{
    [Test]
    public async Task FullRigPayloadAcceptsStringValuedStationKeepingModes()
    {
        const string payload = """
            {"Name":"Deepsea Stavanger","StationKeepingSystem":{"Modes":["DynamicPositioning","Moored"]}}
            """;
        using var httpClient = new HttpClient(new JsonHandler(payload));
        var client = new Client("http://rig.test/Rig/api/", httpClient);

        RigReadResponse rig = await client.GetRigByIdAsync(Guid.Parse("723a0751-445a-42f2-878e-ac4374386ff4"));

        Assert.That(rig.StationKeepingSystem.Modes,
            Is.EqualTo(new[] { StationKeepingMode.DynamicPositioning, StationKeepingMode.Moored }));
    }

    private sealed class JsonHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/Rig/api/Rig/723a0751-445a-42f2-878e-ac4374386ff4"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }
}
