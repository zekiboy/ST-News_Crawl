using System;
using System.Net;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Nest;
using NewsCrawl.Models;

namespace NewsCrawl.Services
{

    public class NewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<NewsService> _logger;

        public NewsService(HttpClient httpClient, IElasticClient elasticClient, ILogger<NewsService> logger)
        {
            _httpClient = httpClient;
            _elasticClient = elasticClient;
            _logger = logger;
        }


        // HTML dokümanını asenkron şekilde çeker
        private async Task<HtmlDocument> GetHtmlDocumentAsync(string url)
        {
            try
            {
                _logger.LogInformation("HTML dokümanı çekiliyor: {Url}", url);
                var response = await _httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(response);
                _logger.LogInformation("HTML dokümanı başarıyla yüklendi.");
                return htmlDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTML dokümanı alınırken bir hata oluştu: {Url}", url);
                throw;
            }
        }


        // Web sitesinden haberleri çeken metot
        public async Task<List<News>> GetNewsFromWebsiteAsync()
        {
            _logger.LogInformation("Web sitesinden haberler çekiliyor...");
            var newsList = new List<News>();

            try
            {
                var url = "https://www.sozcu.com.tr";
                var htmlDocument = await GetHtmlDocumentAsync(url);

                var newsNodes = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'news-card')]");
                if (newsNodes == null)
                {
                    _logger.LogWarning("Haber kartları bulunamadı.");
                    return newsList;
                }

                foreach (var node in newsNodes)
                {
                    //ilgili siteden kullanmak üzere ilgili alanları çekiyoruz
                    var titleNode = node.SelectSingleNode(".//a[contains(@class, 'news-card-footer')]");
                    var linkNode = node.SelectSingleNode(".//a[contains(@class, 'news-card-footer')]/@href");
                    var imageNode = node.SelectSingleNode(".//img");

                    //Karakter sıkıntısı vs yaşamamak için boşlukları temizliyoruz
                    var title = titleNode?.InnerText.Trim();
                    var link = linkNode?.GetAttributeValue("href", "").Trim();
                    var imageUrl = imageNode?.GetAttributeValue("src", "").Trim();

                    if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                        link = "https://www.sozcu.com.tr" + link;

                    if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
                        imageUrl = "https://www.sozcu.com.tr" + imageUrl;

                    if (!string.IsNullOrEmpty(title))
                        title = WebUtility.HtmlDecode(title);

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(link))
                    {
                        newsList.Add(new News
                        {
                            Title = title,
                            Link = link,
                            ImageUrl = imageUrl
                        });
                        _logger.LogInformation("Haber eklendi: {Title}", title);
                    }
                }

                _logger.LogInformation("Web sitesinden {Count} haber başarıyla çekildi.", newsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Web sitesinden haberler çekilirken bir hata oluştu.");
            }

            return newsList;
        }



        [HttpPost(Name = "AddNewsToElasticsearch")]
        public async Task<List<News>> AddNewsToElasticsearch()
        {
            _logger.LogInformation("Haberler Elasticsearch'e ekleniyor...");
            var newsList = await GetNewsFromWebsiteAsync();

            foreach (var news in newsList)
            {
                try
                {
                    // Elasticsearch'te aynı başlığa sahip bir haber var mı kontrol ediyoruz
                    var searchResponse = await _elasticClient.SearchAsync<News>(s => s
                        .Query(q => q
                            .Match(m => m
                                .Field(f => f.Title)
                                .Query(news.Title)
                            )
                        )
                    );

                    if (searchResponse.Hits.Count == 0)
                    {
                        await _elasticClient.IndexDocumentAsync(news);
                        _logger.LogInformation("Haber Elasticsearch'e eklendi: {Title}", news.Title);
                    }
                    else
                    {
                        _logger.LogInformation("Haber zaten Elasticsearch'te mevcut: {Title}", news.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Haber Elasticsearch'e eklenirken bir hata oluştu: {Title}", news.Title);
                }
            }

            _logger.LogInformation("Tüm haberler işleme alındı.");
            return newsList;
        }



        [HttpGet(Name = "GetAllProducts")]
        public async Task<List<News>> GetNewsFromElasticSearch()
        {
            _logger.LogInformation("ElasticSearch'ten haberler çekiliyor...");

            try
            {
                var result = await _elasticClient.SearchAsync<News>(s => s
                    .Index("news-index")
                    .Query(q => q.MatchAll())
                    .Size(500));

                _logger.LogInformation("{Count} haber ElasticSearch'ten başarıyla çekildi.", result.Documents.Count);
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ElasticSearch'ten haberler çekilirken bir hata oluştu.");
                throw;
            }
        }


        //anahtar kelime aramamızı yapacak olan metod
        public async Task<List<News>> SearchNewsFromElasticSearch(string query)
        {
            _logger.LogInformation("ElasticSearch'te arama yapılıyor: {Query}", query);

            try
            {
                var result = await _elasticClient.SearchAsync<News>(s => s
                    .Index("news-index") // İndeks adı
                    .Query(q => q
                        .MultiMatch(m => m
                            .Fields(f => f
                                .Field(n => n.Title) // Başlıkta ara
                                .Field(n => n.Link) // Linkte ara
                            )
                            .Query(query) // Kullanıcı sorgusu
                            .Fuzziness(Fuzziness.Auto)// Esnek arama
                        )
                    )
                    .Size(50) // Sonuç sayısı
                );

                _logger.LogInformation("{Count} sonuç bulundu.", result.Documents.Count);
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ElasticSearch'te arama yapılırken bir hata oluştu.");
                throw;
            }
        }




    }

}

