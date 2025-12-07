using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace MubeaSatinalmaPortal.Filters
{
    /// <summary>
    /// Session kontrolü yapan özel yetkilendirme attribute'u
    /// Kullanım: [SessionAuthorize] veya [SessionAuthorize(MinLevel = 2)]
    /// </summary>
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Minimum kullanıcı seviyesi (opsiyonel)
        /// 0 = Herkes (sadece login kontrolü)
        /// 1 = Departman Yöneticisi ve üstü
        /// 2 = Satın Alma ve üstü
        /// 3 = Sadece Genel Müdür
        /// </summary>
        public int MinLevel { get; set; } = 0;

        /// <summary>
        /// Kullanıcının belirli bir departmana ait olmasını zorunlu kılar
        /// </summary>
        public bool RequireDepartment { get; set; } = false;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // 1️⃣ Kullanıcı login olmuş mu?
            string currentUser = session.GetString("User");

            if (string.IsNullOrEmpty(currentUser))
            {
                // Login olmamış - anasayfaya yönlendir
                context.HttpContext.Response.Redirect("/Home/Index");
                context.Result = new EmptyResult();
                return;
            }

            // 2️⃣ Kullanıcı seviyesi kontrolü (MinLevel belirtilmişse)
            if (MinLevel > 0)
            {
                int? userLevel = session.GetInt32("UserLevel");

                if (!userLevel.HasValue || userLevel.Value < MinLevel)
                {
                    // Yetkisiz - anasayfaya yönlendir ve uyarı göster
                    context.HttpContext.Response.Redirect("/Home/Index?error=unauthorized");
                    context.Result = new EmptyResult();
                    return;
                }
            }

            // 3️⃣ Departman kontrolü (RequireDepartment = true ise)
            if (RequireDepartment)
            {
                string department = session.GetString("UserDepartment");

                if (string.IsNullOrEmpty(department))
                {
                    // Departman bilgisi yok
                    context.HttpContext.Response.Redirect("/Home/Index?error=no_department");
                    context.Result = new EmptyResult();
                    return;
                }
            }

            // ✅ Tüm kontroller başarılı - devam et
            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Sadece Departman Yöneticisi ve üstü için kısayol
    /// Kullanım: [DepartmentManagerOnly]
    /// </summary>
    public class DepartmentManagerOnlyAttribute : SessionAuthorizeAttribute
    {
        public DepartmentManagerOnlyAttribute()
        {
            MinLevel = 1;
        }
    }

    /// <summary>
    /// Sadece Satın Alma ve üstü için kısayol
    /// Kullanım: [PurchasingOnly]
    /// </summary>
    public class PurchasingOnlyAttribute : SessionAuthorizeAttribute
    {
        public PurchasingOnlyAttribute()
        {
            MinLevel = 2;
        }
    }

    /// <summary>
    /// Sadece Genel Müdür için kısayol
    /// Kullanım: [GeneralManagerOnly]
    /// </summary>
    public class GeneralManagerOnlyAttribute : SessionAuthorizeAttribute
    {
        public GeneralManagerOnlyAttribute()
        {
            MinLevel = 3;
        }
    }
}