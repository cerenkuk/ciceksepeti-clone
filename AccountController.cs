using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Kullanıcı hesap işlemlerini (Giriş, Kayıt, Profil, Adres, Kart ve Mesajlaşma) yöneten kontrolcü.
    /// [Authorize] niteliği ile bu kontrolcüdeki çoğu işleme sadece giriş yapmış kullanıcılar erişebilir.
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Yardımcı Özellik: Mevcut giriş yapmış kullanıcının e-postasını Claims üzerinden çeker.
        private string MeEmail => User.FindFirstValue(ClaimTypes.Email) ?? "";

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // KİMLİK DOĞRULAMA İŞLEMLERİ (LOGIN & REGISTER)
        // ============================================================

        [AllowAnonymous] // Giriş yapmamış kullanıcılar da erişebilir.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı güvenlik önlemi.
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = true, string? returnUrl = null)
        {
            // Kullanıcıyı e-posta adresine göre veri tabanında sorgula.
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["ok"] = "E-posta veya şifre hatalı.";
                return View();
            }

            // Girilen şifreyi hashleyip veri tabanındaki ile karşılaştır.
            var passwordHash = AuthMemory.Hash(password);

            if (user.PasswordHash != passwordHash)
            {
                TempData["ok"] = "E-posta veya şifre hatalı.";
                return View();
            }

            // Kimlik doğrulama oturumunu (Cookie) başlat.
            await SignInAsync(user.FullName, user.Email, user.Role, rememberMe);

            // Kullanıcı rolüne göre ilgili panele yönlendir.
            if (user.Role == "Admin") return RedirectToAction("Dashboard", "Admin");
            if (user.Role == "Seller") return RedirectToAction("Dashboard", "Seller");

            // Dönüş URL'si varsa oraya, yoksa ana sayfaya yönlendir.
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, bool rememberMe = true)
        {
            // Form verilerinin boş olup olmadığını kontrol et.
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ok"] = "Lütfen tüm alanları doldurun.";
                return View();
            }

            // E-posta adresi sistemde zaten var mı kontrolü.
            if (await _db.AppUsers.AnyAsync(u => u.Email == email))
            {
                TempData["ok"] = "Bu e-posta zaten kayıtlı.";
                return View();
            }

            // Yeni kullanıcıyı oluştur ve veri tabanına kaydet.
            var newUser = new AppUser
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                PasswordHash = AuthMemory.Hash(password),
                Role = "Customer" // Varsayılan rol Müşteri.
            };

            _db.AppUsers.Add(newUser);
            await _db.SaveChangesAsync();

            // Kayıt sonrası otomatik giriş yap.
            await SignInAsync(newUser.FullName, newUser.Email, newUser.Role, rememberMe);

            TempData["ok"] = "Kayıt başarılı. Hoş geldiniz!";
            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // KULLANICI PROFİL VE İÇERİK YÖNETİMİ
        // ============================================================

        [HttpGet]
        public IActionResult Me() => View(); // Kullanıcı ana sayfası.

        [HttpGet]
        public IActionResult Orders() => View(); // Sipariş geçmişi.

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email)?.ToLower() ?? "";
            if (string.IsNullOrEmpty(userEmail)) return View(new List<Review>());

            // Kullanıcının yaptığı yorumları ürün bilgileriyle birlikte getir.
            var userReviews = await _db.Reviews
                .Include(r => r.Product)
                .Where(r => r.UserEmail == userEmail)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return View(userReviews ?? new List<Review>());
        }

        // ============================================================
        // ADRES YÖNETİMİ
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            if (string.IsNullOrEmpty(MeEmail)) return View(new List<SavedAddress>());

            // Kullanıcıya ait tüm kayıtlı adresleri listele.
            var addresses = await _db.SavedAddresses
                                     .Where(a => a.Email == MeEmail)
                                     .ToListAsync();

            return View(addresses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(SavedAddress address, bool isDefault = false)
        {
            address.IsDefault = isDefault;
            address.Email = MeEmail;

            // Eğer yeni eklenen adres varsayılan yapıldıysa, eski varsayılan adresi normale çek.
            if (address.IsDefault)
            {
                var otherAddresses = await _db.SavedAddresses
                    .Where(a => a.Email == MeEmail && a.IsDefault)
                    .ToListAsync();

                foreach (var other in otherAddresses) other.IsDefault = false;
            }

            _db.SavedAddresses.Add(address);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Adres başarıyla eklendi.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            // Silinecek adresin mevcut kullanıcıya ait olduğundan emin ol.
            var addressToDelete = await _db.SavedAddresses
                                           .FirstOrDefaultAsync(a => a.Id == id && a.Email == MeEmail);

            if (addressToDelete != null)
            {
                _db.SavedAddresses.Remove(addressToDelete);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Adres başarıyla silindi.";
            }

            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            // Tüm adreslerin 'Varsayılan' işaretini kaldır, seçileni 'Varsayılan' yap.
            var newDefaultAddress = await _db.SavedAddresses
                                             .FirstOrDefaultAsync(a => a.Id == id && a.Email == MeEmail);

            if (newDefaultAddress != null)
            {
                var oldDefaultAddress = await _db.SavedAddresses
                    .FirstOrDefaultAsync(a => a.Email == MeEmail && a.IsDefault);

                if (oldDefaultAddress != null) oldDefaultAddress.IsDefault = false;

                newDefaultAddress.IsDefault = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Addresses));
        }

        // ============================================================
        // KART YÖNETİMİ (KREDİ KARTI SAKLAMA)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Cards()
        {
            var cards = await _db.SavedCards
                                 .Where(c => c.Email == MeEmail)
                                 .ToListAsync();
            return View(cards ?? new List<SavedCard>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCard(string holderName, string pan, int expMonth, int expYear)
        {
            // Kart numarasının sadece son 4 hanesini maskeli şekilde kaydet (Güvenlik için).
            var newCard = new SavedCard
            {
                Email = MeEmail,
                Brand = "VISA",
                Masked = $"**** {pan.Substring(pan.Length - 4)}",
                HolderName = holderName,
                Exp = $"{expMonth:00}/{expYear}",
            };

            _db.SavedCards.Add(newCard);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Kart başarıyla eklendi.";
            return RedirectToAction(nameof(Cards));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var cardToDelete = await _db.SavedCards
                                         .FirstOrDefaultAsync(c => c.Id == id && c.Email == MeEmail);

            if (cardToDelete != null)
            {
                _db.SavedCards.Remove(cardToDelete);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Kart başarıyla silindi.";
            }

            return RedirectToAction(nameof(Cards));
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == MeEmail);
            var model = new ProfileViewModel
            {
                FullName = user?.FullName ?? "Ad/Soyad Bilinmiyor",
                Email = MeEmail,
            };
            return View(model);
        }

        // ============================================================
        // MESAJLAŞMA SİSTEMİ (MÜŞTERİ DESTEK & SATICI İLETİŞİM)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Messages()
        {
            // Sadece ConversationId == null olanlar değil, kullanıcının dahil olduğu tüm başlıklar
            var conversations = await _db.Message
                .Where(m => m.SenderEmail == MeEmail || m.RecipientEmail == MeEmail)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();

            return View(conversations);
        }

       
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> NewMessage(ciceksepetim.Models.Message message)
            {
                if (ModelState.IsValid)
                {
                    message.SentDate = DateTime.Now;
                    message.SenderEmail = MeEmail;

                    // 1. ÖNCELİK: Sipariş ID varsa, satıcıyı her zaman veritabanından çek
                    if (message.RelatedOrderId.HasValue && message.RelatedOrderId > 0)
                    {
                        var sellerFromDb = await _db.OrderItems
                            .Where(i => i.OrderId == message.RelatedOrderId.Value)
                            .Select(i => i.SellerEmail)
                            .FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(sellerFromDb))
                        {
                            // Formdan ne gelirse gelsin veritabanındaki satıcıyı atıyoruz
                            message.RecipientEmail = sellerFromDb;
                            message.MessageType = "SharedMessage";
                        }
                    }

                    // 2. ÖNCELİK: Hala boşsa (veya sipariş bulunamadıysa) admine yönlendir
                    if (string.IsNullOrWhiteSpace(message.RecipientEmail))
                    {
                        message.RecipientEmail = "admin@site.com";
                        message.MessageType = "CustomerToAdmin";
                    }

                    // Kendi kendine mesaj kontrolü
                    if (message.RecipientEmail == message.SenderEmail)
                    {
                        TempData["error"] = "Kendinize mesaj gönderemezsiniz.";
                        return RedirectToAction("Messages");
                    }

                    message.IsReadBySender = true;
                    message.IsReadByRecipient = false;
                    message.ConversationId = null;

                    _db.Message.Add(message);
                    await _db.SaveChangesAsync();

                    TempData["ok"] = "Mesajınız iletildi!";
                    return RedirectToAction("Messages");
                }
                return View(message);
            }
        
        [Authorize]
        public async Task<IActionResult> MessageDetail(int id)
        {
            // Konuşmaya ait tüm mesajları kronolojik olarak getir.
            var messages = await _db.Message
                .Where(m => (m.Id == id || m.ConversationId == id) && (m.SenderEmail == MeEmail || m.RecipientEmail == MeEmail))
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            if (!messages.Any()) return NotFound();

            // Gelen mesajları "Okundu" olarak işaretle.
            var unreadMessages = messages.Where(m => m.RecipientEmail == MeEmail && !m.IsReadByRecipient).ToList();
            foreach (var msg in unreadMessages) msg.IsReadByRecipient = true;

            await _db.SaveChangesAsync();
            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int conversationId, string content)
        {
            var conversationStarter = await _db.Message.FindAsync(conversationId);
            if (conversationStarter == null) return RedirectToAction(nameof(Messages));

            // Yanıt verilecek kişiyi belirle (Mesajı atan kişiye geri dönülür).
            string recipientEmail = conversationStarter.SenderEmail == MeEmail
                                     ? conversationStarter.RecipientEmail
                                     : conversationStarter.SenderEmail;

            var reply = new Message
            {
                Subject = $"Yanıt: {conversationStarter.Subject}",
                MessageType = conversationStarter.MessageType,
                Content = content,
                SenderEmail = MeEmail,
                RecipientEmail = recipientEmail,
                SentDate = DateTime.Now,
                RelatedOrderId = conversationStarter.RelatedOrderId,
                IsReadBySender = true,
                IsReadByRecipient = false,
                ConversationId = conversationId // Bu bir yanıttır, ana mesaj ID'sine bağlıdır.
            };

            _db.Message.Add(reply);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(MessageDetail), new { id = conversationId });
        }

        [HttpGet]
        public IActionResult NewMessage() => View(new ciceksepetim.Models.Message { SenderEmail = MeEmail });

        // ============================================================
        // OTURUM YÖNETİMİ VE YARDIMCI METOTLAR
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Cookie bazlı oturumu sonlandır.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["ok"] = "Başarıyla çıkış yaptınız.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Kullanıcı bilgilerini içeren Claims oluşturur ve Authentication Cookie'sini sisteme yazar.
        /// </summary>
        private async Task SignInAsync(string fullName, string email, string role, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("N")),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = rememberMe });
        }
    }
}