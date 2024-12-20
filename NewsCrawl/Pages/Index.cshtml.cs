using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewsCrawl.Models;
using NewsCrawl.Services;

namespace NewsCrawl.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly NewsService _newsService;
    public List<News> NewsList { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Query { get; set; } // Arama sorgusu
    public IndexModel(ILogger<IndexModel> logger, NewsService newsService) 
    {
        _logger = logger;
        _newsService = newsService;

    }

    public async Task OnGetAsync()
    {

        //haberleri dbye aktaracak bir method yaz 
        await _newsService.AddNewsToElasticsearch();

        // Arama sorgusu boşsa tüm haberleri getir
        if (string.IsNullOrEmpty(Query))
        {
            NewsList = await _newsService.GetNewsFromElasticSearch();
        }
        else
        {
            // Arama sorgusuna göre filtrelenmiş haberleri getir
            NewsList = await _newsService.SearchNewsFromElasticSearch(Query);
        }


    }
}

