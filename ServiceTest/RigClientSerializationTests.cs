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
            [{"Name":"Deepsea Stavanger","StationKeepingSystem":{"Modes":["DynamicPositioning","Moored"]}}]
            """;
        using var httpClient = new HttpClient(new JsonHandler(payload));
        var client = new Client("http://rig.test/Rig/api/", httpClient);

        RigReadResponse rig = (await client.GetAllRigAsync()).Single();

        Assert.That(rig.StationKeepingSystem.Modes,
            Is.EqualTo(new[] { StationKeepingMode.DynamicPositioning, StationKeepingMode.Moored }));
    }

    private sealed class JsonHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
    }
}
