using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nest;
using NewsCrawl.Models;
using NewsCrawl.Services;

namespace NewsCrawl.Pages
{
    public class NewsListModel : PageModel
    {
        private readonly NewsService _newsService;
        public List<News> NewsList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Query { get; set; } // Arama sorgusu

        public NewsListModel(NewsService newsService)
        {
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
}

