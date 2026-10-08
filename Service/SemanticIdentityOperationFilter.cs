using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.WellBore.Service;

public sealed class SemanticIdentityOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        if(context.MethodInfo.DeclaringType!=typeof(Controllers.WellBoreController) || context.MethodInfo.Name!="GetWellBoreById")return;
        var identity=Model.ProviderSemantics.Metadata(Concepts.ResourceIdentifier);identity["resourceType"]=Concepts.WellBore;
        foreach(var parameter in operation.Parameters.Where(p=>p.Name=="id"))
            parameter.Schema.Extensions[SemanticMetadata.ExtensionName]=OpenApiAnyFactory.CreateFromJson(identity.ToJsonString());
    }
}
