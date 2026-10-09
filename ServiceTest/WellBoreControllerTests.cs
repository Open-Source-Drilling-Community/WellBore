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
using OSDC.Drilling.WellBore.Service;
using OSDC.Drilling.WellBore.Service.Managers;
using OSDC.Drilling.WellBore.ModelShared;
using NUnit.Framework;

namespace OSDC.Drilling.WellBore.ServiceTest
{
    [TestFixture]
    [NonParallelizable]
    public class WellBoreControllerTests
    {
        private HttpClient _http = null!;
        private Client _client = null!;
        private WebApplicationFactory<Program> _factory = null!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            ServiceTestHost.ResetManagerSingletons();
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Service")));
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
            _http = _factory.CreateClient();
            var baseUrl = new Uri(_http.BaseAddress!, "WellBore/api/").ToString();
            _client = new Client(baseUrl, _http);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _http?.Dispose();
            _factory?.Dispose();
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
                await _client.PutWellBoreByIdAsync(idUrl, DateTimeOffset.UtcNow, wb));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        }

        [Test]
        public void Delete_Unknown_Throws404()
        {
            var ex = Assert.ThrowsAsync<ApiException>(async () =>
                await _client.DeleteWellBoreByIdAsync(Guid.NewGuid(), DateTimeOffset.UtcNow));
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
                IsSidetrack = false
            };
            await _client.PutWellBoreByIdAsync(id, got.LastModificationDate!.Value, update);

            var updated = await _client.GetWellBoreByIdAsync(id);
            await _client.DeleteWellBoreByIdAsync(id, updated.LastModificationDate!.Value);

            var ex = Assert.ThrowsAsync<ApiException>(async () => await _client.GetWellBoreByIdAsync(id));
            Assert.That(ex!.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
        }
    }
}

