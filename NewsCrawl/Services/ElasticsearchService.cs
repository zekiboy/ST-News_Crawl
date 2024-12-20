using Microsoft.Extensions.DependencyInjection;
using Nest;
using NewsCrawl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsCrawl.Services
{
    public static class ElasticsearchService
    {
        public static void AddElasticsearch(
                    this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["ELKConfiguration:url"];
            var defaultIndex = configuration["ELKConfiguration:index"];

            var settings = new ConnectionSettings(new Uri(url))
                .PrettyJson()
                .DefaultIndex(defaultIndex);

            AddDefaultMappings(settings);

            var client = new ElasticClient(settings);

            services.AddScoped<IElasticClient>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var url = configuration["ELKConfiguration:url"];
                var defaultIndex = configuration["ELKConfiguration:index"];

                var settings = new ConnectionSettings(new Uri(url))
                    .PrettyJson()
                    .DefaultIndex(defaultIndex);

                return new ElasticClient(settings);
            });


            CreateIndex(client, defaultIndex);
        }

        private static void AddDefaultMappings(ConnectionSettings settings)
        {
            settings
                .DefaultMappingFor<News>(m => m
                    .PropertyName(p => p.Title, "title")
                    .PropertyName(p => p.Link, "link")
                    .PropertyName(p => p.ImageUrl, "image_url")
                );
        }



        private static void CreateIndex(IElasticClient client, string indexName)
        {
            var existsResponse = client.Indices.Exists(indexName);

            if (!existsResponse.Exists)
            {
                var createIndexResponse = client.Indices.Create(indexName, index => index
                    .Map<News>(x => x.AutoMap())
                );

                if (!createIndexResponse.IsValid)
                {
                    Console.WriteLine($"Indeks oluşturulamadı: {createIndexResponse.DebugInformation}");
                }
            }
        }

    }
}