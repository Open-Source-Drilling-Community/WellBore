using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using OSDC.Drilling.WellBore.ModelShared;

string localHostName = "https://localhost:5001/";
string devHostName = "https://dev.digiwells.no/";
string wellHostBase = "WellBore/api/";

// Create clients to access databases from dev/ environment
Client wellBoreClient = ClientSetup(devHostName, wellHostBase);
// Create clients to access databases from local/ environment
Client wellBoreLocalClient = ClientSetup(localHostName, wellHostBase);
// Get all wells and cartographic projection sets from the APIs
// This explicit administrative copy reads full records individually after light discovery.
List<WellBore> wells = [];
foreach (var summary in await wellBoreClient.ListWellBoreLightAsync())
    wells.Add(await wellBoreClient.GetWellBoreByIdAsync(summary.MetaInfo!.ID));

// Update local database with data from dev/ database
foreach (var well in wells)
{
    // Update each well in local database with the one from dev database
    Console.WriteLine($"Updating well with ID {well.MetaInfo!.ID} in local database...");
    await wellBoreLocalClient.PostWellBoreAsync(well);
}
// Test if the new method works...
Guid wellID = wells[0].WellID!.Value;
List<WellBoreLight> wellBoresByWellId = (List<WellBoreLight>) (await wellBoreLocalClient.ListWellBoreLightAsync(wellId: wellID)).ToList();
Console.WriteLine($"Number of wells with slot ID {wellID} in local database: {wellBoresByWellId.Count}");

List<WellBoreLight> sidetrackedWellBores = (List<WellBoreLight>) (await wellBoreLocalClient.ListWellBoreLightAsync(isSidetrack: true)).ToList();
Console.WriteLine($"Number of sidetracked wells in local database: {sidetrackedWellBores.Count}");


// Functions
Client ClientSetup(string _hostName, string _hostBase)
{
    HttpClient httpClient;
    Client api;
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(_hostName + _hostBase)
    };
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    api = new Client(httpClient.BaseAddress.ToString(), httpClient);
    return api;
}
