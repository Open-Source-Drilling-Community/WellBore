using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.WellBore.Service;
using NORCE.Drilling.WellBore.Service.Managers;
using NORCE.Drilling.WellBore.ModelShared;
using NUnit.Framework;

namespace NORCE.Drilling.WellBore.ServiceTest
{
    [TestFixture]
    public class WellBoreControllerTests
    {
        private HttpClient _http = null!;
        private Client _client = null!;

        public static HttpClient SetHttpClient(string host, string microServiceUri)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }; // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
            HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri(host + microServiceUri)
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            string HostNameWellBore = "https://localhost:5001/";
            string HostBasePathWellBore = "WellBore/api/";
            _http = SetHttpClient(HostNameWellBore, HostBasePathWellBore);
            var baseUrl = new Uri(_http.BaseAddress!, "/WellBore/api/").ToString();
            _client = new Client(baseUrl, _http);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _http?.Dispose();
        }

        [Test]
        public async Task GetAllWellBoreId_ReturnsArray()
        {
            var ids = await _client.GetAllWellBoreIdAsync();
            Assert.That(ids, Is.Not.Null);
        }

        [Test]
        public async Task GetAllWellBoreMetaInfo_ReturnsArray()
        {
            var meta = await _client.GetAllWellBoreMetaInfoAsync();
            Assert.That(meta, Is.Not.Null);
        }

        [Test]
        public async Task GetAllWellBore_HeavyData_ReturnsArray()
        {
            var vals = await _client.GetAllWellBoreAsync();
            Assert.That(vals, Is.Not.Null);
        }

        [Test]
        public void GetWellBoreById_Unknown_Throws404()
        {
            var ex = Assert.ThrowsAsync<ApiException>(async () =>
                await _client.GetWellBoreByIdAsync(Guid.NewGuid()));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
        }

        [Test]
        public void Post_NullBody_Throws400()
        {
            var ex = Assert.ThrowsAsync<ApiException>(async () =>
                await _client.PostWellBoreAsync(null!));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        }

        [Test]
        public void Put_MismatchedId_Throws400()
        {
            var idUrl = Guid.NewGuid();
            var bodyId = Guid.NewGuid();
            var wb = new ModelShared.WellBore { MetaInfo = new MetaInfo { ID = bodyId } };
            var ex = Assert.ThrowsAsync<ApiException>(async () =>
                await _client.PutWellBoreByIdAsync(idUrl, wb));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        }

        [Test]
        public void Delete_Unknown_Throws404()
        {
            var ex = Assert.ThrowsAsync<ApiException>(async () =>
                await _client.DeleteWellBoreByIdAsync(Guid.NewGuid()));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
        }

        [Test]
        public async Task Post_Get_Put_Delete_Flow_Works()
        {
            var id = Guid.NewGuid();

            var create = new ModelShared.WellBore
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "TestWB",
                Description = "Integration test",
                IsSidetrack = false
            };
            await _client.PostWellBoreAsync(create);

            var got = await _client.GetWellBoreByIdAsync(id);
            Assert.That(got, Is.Not.Null);

            var update = new ModelShared.WellBore
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "UpdatedWB",
                Description = "Updated",
                IsSidetrack = true
            };
            await _client.PutWellBoreByIdAsync(id, update);

            await _client.DeleteWellBoreByIdAsync(id);

            var ex = Assert.ThrowsAsync<ApiException>(async () => await _client.GetWellBoreByIdAsync(id));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
        }
    }
}

