using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.Protocol;
using OSDC.Drilling.WellBore.Service;
using OSDC.Drilling.WellBore.Service.Managers;
using OSDC.Drilling.WellBore.Service.Mcp;
using OSDC.Drilling.WellBore.Service.Mcp.Tools;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// registering the manager of SQLite connections through dependency injection
builder.Services.AddSingleton(sp =>
    new SqlConnectionManager(
        $"Data Source={SqlConnectionManager.HOME_DIRECTORY}{SqlConnectionManager.DATABASE_FILENAME}",
        sp.GetRequiredService<ILogger<SqlConnectionManager>>()));

// serialization settings (using System.Json)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        JsonSettings.ApplyTo(options.JsonSerializerOptions);
    });

// serialize using short name rather than full names
builder.Services.AddSwaggerGen(config =>
{
    config.CustomSchemaIds(type => type.FullName);
});

builder.Services.Configure<McpHubOptions>(builder.Configuration.GetSection(McpHubOptions.SectionName));
builder.Services.AddHttpClient(nameof(McpHubRegistrationService));
builder.Services.AddHostedService<McpHubRegistrationService>();

// MCP server registrations
var serverVersion = typeof(SqlConnectionManager).Assembly.GetName().Version?.ToString() ?? "1.0.0";

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "WellBoreService",
        Version = serverVersion
    };
    options.Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability()
    };
}).WithHttpTransport();

builder.Services.AddLegacyMcpTool<PingMcpTool>();
builder.Services.AddWellBoreRestMcpTools();

// end MCP server

var app = builder.Build();

var basePath = "/wellbore/api";

app.UsePathBase(basePath);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    string path = context.Request.Path.Value ?? string.Empty;
    if (path.Contains("/.well-known/oauth-protected-resource", System.StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/.well-known/oauth-authorization-server", System.StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        string body = "{\"error\":\"oauth_not_configured\",\"error_description\":\"This MCP server does not require OAuth. Connect directly to the MCP endpoint.\",\"authentication\":\"none\"}";
        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(body));
        return;
    }

    await next();
});

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

string relativeSwaggerPath = "/swagger/merged/swagger.json";
string fullSwaggerPath = $"{basePath}{relativeSwaggerPath}";
string customVersion = "Merged API Version 1";

var mergedDoc = SwaggerMiddlewareExtensions.ReadOpenApiDocument("wwwroot/json-schema/WellBoreMergedModel.json");
app.UseCustomSwagger(mergedDoc, relativeSwaggerPath);
app.UseSwaggerUI(c =>
{
    //c.SwaggerEndpoint("v1/swagger.json", "API Version 1");
    c.SwaggerEndpoint(fullSwaggerPath, customVersion);
});

app.UseCors(cors => cors
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(origin => true)
                        .AllowCredentials()
           );

app.MapMcp("/mcp");
app.MapMcpWebSocket("/mcp/ws");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
