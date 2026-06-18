using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace Infrastructure.Extensions
{
    public static class SwaggerExtensions
    {
        public static void UseCustomSwagger(this IApplicationBuilder app,IConfiguration configuration)
        {
            var pathPrefix = configuration.GetValue<string>("Config:PathPrefix");
            
            app.UseSwagger(options =>
            {
                if (!string.IsNullOrEmpty(pathPrefix))
                {
                    options.RoutePrefix = $"{pathPrefix.TrimStart('/')}/swagger";
                    options.PreSerializeFilters.Add(
                        (doc, req) =>
                        {
                            doc.Servers = new List<OpenApiServer>
                            {
                                new() {Url = pathPrefix}
                            };
                        });
                }
            });

            app.UseSwaggerUI(options =>
            {
                if (!string.IsNullOrEmpty(pathPrefix))
                {
                    options.RoutePrefix = $"{pathPrefix.TrimStart('/')}/swagger";
                    options.SwaggerEndpoint($"{pathPrefix}/swagger/v1/swagger.json", "API V1");
                }
                else
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                }
            });

        }
    }
}
