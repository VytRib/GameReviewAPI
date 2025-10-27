using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace GameReviewsAPI.Swagger
{
    public class SwaggerRequestExamplesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDesc = context.ApiDescription;
            if (apiDesc == null || operation.RequestBody == null) return;

            var routeValues = apiDesc.ActionDescriptor.RouteValues;
            routeValues.TryGetValue("controller", out var controller);
            var method = apiDesc.HttpMethod?.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(method)) return;

            if (!operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
                return;

            void SetExample(Microsoft.OpenApi.Any.IOpenApiAny example)
            {
                mediaType.Example = example;
            }

            if (controller.Equals("Games", System.StringComparison.OrdinalIgnoreCase))
            {
                if (method == "POST")
                {
                    SetExample(new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(100),
                        ["title"] = new OpenApiString("TEST"),
                        ["description"] = new OpenApiString("Open-world action RPG with challenging combat"),
                        ["imageUrl"] = new OpenApiString(string.Empty),
                        ["genreId"] = new OpenApiInteger(2)
                    });
                }
                else if (method == "PUT")
                {
                    SetExample(new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(1),
                        ["title"] = new OpenApiString("Elden Ring - Updated"),
                        ["description"] = new OpenApiString("Open-world action RPG with improved features"),
                        ["imageUrl"] = new OpenApiString(string.Empty),
                        ["genreId"] = new OpenApiInteger(2)
                    });
                }
            }

            if (controller.Equals("Genres", System.StringComparison.OrdinalIgnoreCase))
            {
                if (method == "POST")
                {
                    SetExample(new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(6),
                        ["name"] = new OpenApiString("MMORPG")
                    });
                }
                else if (method == "PUT")
                {
                    SetExample(new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(1),
                        ["name"] = new OpenApiString("Action-Adventure")
                    });
                }
            }

            if (controller.Equals("Reviews", System.StringComparison.OrdinalIgnoreCase))
            {
                if (method == "POST")
                {
                    SetExample(new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(500),
                        ["rating"] = new OpenApiInteger(5),
                        ["comment"] = new OpenApiString("Amazing gameplay with stunning graphics!"),
                        ["gameId"] = new OpenApiInteger(1),
                        ["userId"] = new OpenApiInteger(3)
                    });
                }
                else if (method == "PUT")
                {
                    SetExample(new OpenApiObject
                    {
                        ["id"] = new OpenApiInteger(11),
                        ["rating"] = new OpenApiInteger(4),
                        ["comment"] = new OpenApiString("Still a great game, but some bugs remain."),
                        ["gameId"] = new OpenApiInteger(1),
                        ["userId"] = new OpenApiInteger(3)
                    });
                }
            }
        }
    }
}
