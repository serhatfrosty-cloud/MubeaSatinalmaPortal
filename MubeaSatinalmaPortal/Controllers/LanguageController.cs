using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace MubeaSatinalmaPortal.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            if (string.IsNullOrEmpty(culture))
            {
                return RedirectToAction("Index", "Home");
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"  // ← BU SATIRI EKLEYİN
                }
            );

            return LocalRedirect(returnUrl);
        }
    }
}