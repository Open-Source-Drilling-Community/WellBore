using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using NORCE.Drilling.WellBore.ModelShared;

string localHostName = "https://localhost:5001/";
string devHostName = "https://dev.digiwells.no/";
string wellHostBase = "WellBore/api/";

// Create clients to access databases from dev/ environment
Client wellBoreClient = ClientSetup(devHostName, wellHostBase);
// Create clients to access databases from local/ environment
Client wellBoreLocalClient = ClientSetup(localHostName, wellHostBase);
// Get all wells and cartographic projection sets from the APIs
List<WellBore> wells = (List<WellBore>) (await wellBoreClient.GetAllWellBoreAsync()).ToList();

// Update local database with data from dev/ database
foreach (var well in wells)
{
    // Update each well in local database with the one from dev database
    Console.WriteLine($"Updating well with ID {well.MetaInfo!.ID} in local database...");
    await wellBoreLocalClient.PostWellBoreAsync(well);
}
// Test if the new method works...
Guid wellID = wells[0].WellID!.Value;
List<WellBore> wellBoresByWellId = (List<WellBore>) (await wellBoreLocalClient.GetAllWellBoreByWellIDAsync(wellID)).ToList();
Console.WriteLine($"Number of wells with slot ID {wellID} in local database: {wellBoresByWellId.Count}");

List<WellBore> sidetrackedWellBores = (List<WellBore>) (await wellBoreLocalClient.GetAllSideTrackedWellBoreAsync()).ToList();
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
