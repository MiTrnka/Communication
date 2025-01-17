using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace WebApplicationRedis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public string SessionValue { get; set; }
        public string CookieValue { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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
        }
    }
}
