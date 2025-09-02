using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System;
using System.Collections.Generic;
using System.IO;

public static class SwaggerMiddlewareExtensions
{
    public static void UseCustomSwagger(this IApplicationBuilder app, OpenApiDocument mergedDoc, string relativePath)
    {
        app.Map(relativePath, builder =>
        {
            builder.Run(async context =>
            {
                // Dynamically compute scheme and host from request or reverse proxy headers
                var req = context.Request;

                var scheme = req.Headers.ContainsKey("X-Forwarded-Host")
                    ? "https"
                    : req.Scheme;

                var host = req.Headers.ContainsKey("X-Forwarded-Host")
                    ? req.Headers["X-Forwarded-Host"].ToString()
                    : req.Host.Value;

                var pathBase = req.PathBase.HasValue ? req.PathBase.Value : string.Empty;

                // Update the servers list on-the-fly (like PreSerializeFilters would)
                mergedDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{scheme}://{host}{pathBase}" }
                };

                context.Response.ContentType = "application/json";
                var outputString = mergedDoc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
                // temporary fix waiting for swaggerUI tooling to actually implement latest OpenApi 3.0.4 patch, which is limited to 3.0.3 so far (June 2025)
                // same fix applied in ModelSharedIn/Program.cs and ModelSharedOut/Program.cs
                outputString = outputString.Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.3\"");
                await context.Response.WriteAsync(outputString);
            });
        });
    }

    public static OpenApiDocument ReadOpenApiDocument(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var reader = new OpenApiStreamReader();
        var document = reader.Read(stream, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            Console.WriteLine("Warnings or errors while reading OpenAPI document:");
            foreach (var error in diagnostic.Errors)
                Console.WriteLine($"- {error.Message}");
        }

        return document;
    }
}