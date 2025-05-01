using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace Infrastructure.Extensions
{
    public static class SwaggerExtensions
    {
        public static void UseCustomSwagger(this IApplicationBuilder app,IConfiguration configuration)
        {
            app.UseSwagger(options =>
            {
                var pathPrefix = configuration.GetValue<string>("Config:PathPrefix");
                if (!string.IsNullOrEmpty(pathPrefix))
                {
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

        }
    }
}
