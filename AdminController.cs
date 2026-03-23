using ciceksepetim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Sistem yöneticisi (Admin) yetkisine sahip kullanıcıların erişebildiği yönetim paneli kontrolcüsü.
    /// Sadece "Admin" rolündeki kullanıcıların girişine izin verilir.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Yardımcı Özellik: Aktif adminin e-posta bilgisini Claims üzerinden alır.
        private string AdminEmail => User.FindFirstValue(ClaimTypes.Email) ?? "";

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // DASHBOARD (İSTATİSTİK ÖZETİ)
        // ============================================================
        public async Task<IActionResult> Dashboard()
        {
            // Panel üzerindeki özet kartları için veri tabanından toplam sayıları çekiyoruz.
            ViewBag.ProductCount = await _db.Products.CountAsync();
            ViewBag.CategoryCount = await _db.Categories.CountAsync();
            ViewBag.ReviewCount = await _db.Reviews.CountAsync();
            ViewBag.OrderCount = await _db.Orders.CountAsync();

            // Admin'e gelen okunmamış mesaj sayısı.
            ViewBag.UnreadMessageCount = await _db.Message
                .CountAsync(m => m.RecipientEmail == AdminEmail && !m.IsReadByRecipient);

            // Sistemdeki toplam konuşma, satıcı ve onay bekleyen satıcı sayıları.
            ViewBag.TotalConversationCount = await _db.Message.CountAsync(m => m.ConversationId == null);
            ViewBag.TotalSellerCount = await _db.AppUsers.CountAsync(u => u.Role == "Seller");
            ViewBag.PendingSellersCount = await _db.AppUsers.CountAsync(u => u.Role == "Seller" && u.IsApproved == false);

            return View();
        }

        // ============================================================
        // SİPARİŞ YÖNETİMİ
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _db.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new Order
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    FullName = o.FullName,
                    // MODELDEKİ İSİMLERLE EŞLEŞTİRME:
                    CustomerEmail = o.CustomerEmail, // 'Email' değil 'CustomerEmail'
                    Total = o.Total,                 // 'TotalAmount' değil 'Total'
                    CargoCompany = o.CargoCompany ?? "Belirtilmedi", // NULL hatasını önler
                    TrackingNumber = o.TrackingNumber ?? "-",
                    Items = o.Items
                })
                .ToListAsync();

            return View(orders);
        }
        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            // Belirli bir siparişin detaylarını ve kalemlerini getirir.
            var order = await _db.Orders
                                 .Include(o => o.Items)
                                 .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["hata"] = "Sipariş bulunamadı.";
                return RedirectToAction(nameof(Orders));
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            // Sipariş durumunu (Hazırlanıyor, Kargolandı, Tamamlandı vb.) günceller.
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["hata"] = $"Sipariş #{orderId} bulunamadı.";
                return RedirectToAction(nameof(Orders));
            }

            order.Status = newStatus;
            await _db.SaveChangesAsync();

            TempData["ok"] = $"Sipariş #{orderId} durumu '{newStatus}' olarak güncellendi.";
            return RedirectToAction(nameof(OrderDetail), new { id = orderId });
        }

        // ============================================================
        // KATEGORİ YÖNETİMİ
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _db.Categories.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, string slug)
        {
            name = (name ?? "").Trim();
            slug = (slug ?? "").Trim();

            // Boş veri kontrolü.
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
            {
                TempData["hata"] = "Kategori Adı ve URL alanları boş bırakılamaz.";
                return RedirectToAction(nameof(Categories));
            }

            // Benzersiz URL (Slug) kontrolü.
            if (await _db.Categories.AnyAsync(c => c.Slug == slug))
            {
                TempData["hata"] = "Bu URL (Slug) zaten mevcut.";
                return RedirectToAction(nameof(Categories));
            }

            _db.Categories.Add(new Category { Name = name, Slug = slug });
            await _db.SaveChangesAsync();

            TempData["ok"] = "Kategori başarıyla eklendi.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(string slug)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (category != null)
            {
                _db.Categories.Remove(category);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Kategori silindi.";
            }
            return RedirectToAction(nameof(Categories));
        }

        // ============================================================
        // ÜRÜN VE YORUM YÖNETİMİ
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var products = await _db.Products.ToListAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
                TempData["ok"] = $"Ürün ({product.Name}) silindi.";
            }
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            // Kullanıcı yorumlarını en yeniden en eskiye listeler.
            var reviews = await _db.Reviews
                                   .OrderByDescending(r => r.ReviewDate)
                                   .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review != null)
            {
                _db.Reviews.Remove(review);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Yorum silindi.";
            }
            return RedirectToAction(nameof(Reviews));
        }

        // ============================================================
        // SATICI (SELLER) YÖNETİMİ
        // ============================================================

        [HttpGet]
        public IActionResult CreateSeller() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeller(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["hata"] = "Tüm alanları doldurun.";
                return View();
            }

            if (await _db.AppUsers.AnyAsync(u => u.Email == email))
            {
                TempData["hata"] = "Bu e-posta zaten kullanımda.";
                return View();
            }

            // Yeni bir satıcı hesabı oluşturur ve varsayılan olarak onaylı (IsApproved = true) yapar.
            var newSeller = new AppUser
            {
                FullName = fullName,
                Email = email,
                PasswordHash = AuthMemory.Hash(password),
                Role = "Seller",
                IsApproved = true,
                GiftKnotBalance = 0
            };

            _db.AppUsers.Add(newSeller);
            await _db.SaveChangesAsync();

            TempData["ok"] = $"{fullName} başarıyla eklendi.";
            return RedirectToAction("Dashboard");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSeller(int id)
        {
            // Veritabanından satıcıyı bul
            var seller = await _db.AppUsers.FindAsync(id);
            if (seller == null) return NotFound();

            // Durumu onayla
            seller.IsApproved = true;
            await _db.SaveChangesAsync();

            TempData["ok"] = "Satıcı başarıyla onaylandı.";
            return RedirectToAction(nameof(Sellers));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSeller(int id)
        {
            var seller = await _db.AppUsers.FindAsync(id);
            if (seller == null) return RedirectToAction(nameof(Sellers));

            // DİREKT SİLMEK YERİNE: Onayı kaldır ve rolünü değiştir
            // Bu sayede "REFERENCE constraint" hatası almazsın
            seller.IsApproved = false;
            seller.Role = "Rejected"; // Rolünü değiştirirsen Sellers listesinde gözükmez

            await _db.SaveChangesAsync();

            TempData["ok"] = "Satıcı başarıyla reddedildi ve listeden kaldırıldı.";
            return RedirectToAction(nameof(Sellers));
        }
        public IActionResult Sellers()
        {
            // Sistemdeki tüm satıcı rolüne sahip kullanıcıları listeler.
            var sellers = _db.AppUsers.Where(x => x.Role == "Seller").ToList();
            return View(sellers);
        }

        // ============================================================
        // MESAJ YÖNETİMİ (DESTEK TALEPLERİ)
        // ============================================================

        public async Task<IActionResult> Messages()
        {
            // Başlatılan tüm ana mesajlaşma (konuşma) başlıklarını getirir.
            var conversations = await _db.Message
                .Where(m => m.ConversationId == null)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();
            return View(conversations);
        }

        public async Task<IActionResult> MessageDetail(int id)
        {
            // Bir konuşmaya ait tüm geçmişi kronolojik sırada getirir.
            var messages = await _db.Message
                .Where(m => m.Id == id || m.ConversationId == id)
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            if (!messages.Any()) return NotFound();

            // Karşı tarafın rolünü tespit eder (Görünümde Müşteri/Satıcı etiketi basmak için).
            var firstMsg = messages.First();
            var otherEmail = firstMsg.SenderEmail != AdminEmail ? firstMsg.SenderEmail : firstMsg.RecipientEmail;
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == otherEmail);

            ViewBag.ParticipantRole = user?.Role switch
            {
                "Customer" => "Müşteri",
                "Seller" => "Satıcı",
                "Admin" => "Yönetici",
                _ => "Misafir"
            };

            // Admin tarafından görüntülenen mesajları 'Okundu' olarak işaretler.
            var unread = messages.Where(m => m.RecipientEmail == AdminEmail && !m.IsReadByRecipient).ToList();
            foreach (var msg in unread) msg.IsReadByRecipient = true;
            await _db.SaveChangesAsync();

            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCargo(int orderId, string cargoCompany, string trackingNumber)
        {
            // Kargo bilgilerini günceller ve sipariş durumunu otomatik olarak "Kargolandı" çeker.
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.CargoCompany = cargoCompany;
            order.TrackingNumber = trackingNumber;
            order.Status = OrderStatus.Kargolandı;

            await _db.SaveChangesAsync();
            return RedirectToAction("OrderDetail", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int conversationId, string content)
        {
            // Admin'in bir destek talebine veya satıcı mesajına yanıt vermesini sağlar.
            var starter = await _db.Message.FindAsync(conversationId);
            if (starter == null || string.IsNullOrWhiteSpace(content)) return RedirectToAction(nameof(Messages));

            // Alıcıyı konuşma geçmişinden tespit et.
            string recipientEmail = starter.SenderEmail == AdminEmail ? starter.RecipientEmail : starter.SenderEmail;

            var reply = new Message
            {
                Subject = $"Yanıt: {starter.Subject}",
                MessageType = starter.MessageType,
                Content = content,
                SenderEmail = AdminEmail,
                RecipientEmail = recipientEmail,
                SentDate = DateTime.Now,
                IsReadBySender = true,
                IsReadByRecipient = false,
                ConversationId = conversationId // Yanıtın hangi ana başlığa ait olduğunu belirtir.
            };

            _db.Message.Add(reply);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(MessageDetail), new { id = conversationId });
        }
    }
}