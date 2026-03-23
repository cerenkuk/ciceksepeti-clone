using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

/// <summary>
/// Kullanıcıların ürünlere yaptığı yorumları ve değerlendirmeleri yöneten kontrolcü.
/// [Authorize] ile korunmaktadır; kullanıcı sadece kendi yorumlarını görebilir.
/// </summary>
[Authorize]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _db;

    // Aktif kullanıcının kimliğini (Email) Claim üzerinden çeken yardımcı özellik.
    private string MeEmail => User.FindFirstValue(ClaimTypes.Email) ?? "";

    public ReviewsController(ApplicationDbContext db) => _db = db;

    // ============================================================
    // KULLANICI YORUMLARI LİSTESİ
    // ============================================================

    /// <summary>
    /// Giriş yapmış olan kullanıcının geçmişte yaptığı tüm yorumları listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Reviews()
    {
        // Güvenlik Kontrolü: Email bilgisi alınamazsa boş liste dön.
        if (string.IsNullOrEmpty(MeEmail))
            return View(new List<Review>());

        // VERİ İZOLASYONU: 
        // Veri tabanındaki tüm yorumlar içinden sadece 'UserEmail' alanı 
        // aktif kullanıcıya eşit olanlar filtrelelenir.
        var userReviews = await _db.Reviews
                .Where(r => r.UserEmail == MeEmail)
                .OrderByDescending(r => r.ReviewDate) // En güncel yorumu en üstte gösterir.
                .ToListAsync();

        // Veriler Views/Reviews/Reviews.cshtml sayfasına gönderilir.
        return View(userReviews);
    }
}