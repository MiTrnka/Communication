using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;


namespace WebApplicationRedis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache; // Redis cache

        public string SessionValue { get; set; }
        public string CookieValue { get; set; }
        public string VlastniHodnotaVRedis { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IHttpContextAccessor httpContextAccessor, IDistributedCache cache)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public void OnGet()
        {
            SessionValue = _httpContextAccessor?.HttpContext?.Session.GetString("klic1") ?? "nic";
            string? hodnota = this.HttpContext.Request.Cookies["cookieklic"];
            if (String.IsNullOrEmpty(hodnota))
            {
                this.HttpContext.Response.Cookies.Append("cookieklic", DateTime.Now.ToString());
            }
            else
            {
                CookieValue = hodnota;
            }
            _cache.SetString("klic40", "Kaptah"); // v redis bude uložena hodnota "Kaptah" pod klíèem "PrefixProNazevKlicekvuliKoliziMeziAPlikacemaklic40" a to jako typ hash
            VlastniHodnotaVRedis = _cache.GetString("klic40") ?? "Klíè v Redis neexistuje"; // Naètení hodnoty z Redisu (je i async verze), zde prefix pro klic neuvadime, protoze se automaticky doplni
        }
    }
}
