// Models/AuthMemory.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ciceksepetim.Models
{
    /// <summary>
    /// In-memory kullanıcı deposu (sadece demo/test için).
    /// NOT: AppUser POCO'su ayrı dosyada (Models/AppUser.cs) tanımlı olmalı.
    /// </summary>
    public static class AuthMemory
    {
        // Thread-safe kullanım için lock
        private static readonly object _lockObj = new();

        // Basit in-memory liste + id sekansı
        private static readonly List<AppUser> _users = new();
        private static int _seq = 1;

        // Demo seed (uygulama açılışında)
        static AuthMemory()
        {
            // Hata atarsa önemsemiyoruz (zaten varsa)
            TrySeed("Yönetici", "admin@test.com", "123456", "Admin");
            TrySeed("Satıcı", "satici@test.com", "123456", "Seller");
            TrySeed("Müşteri", "user@test.com", "123456", "Customer");
        }

        /// <summary>Seed kullanıcı ekler; varsa sessizce geçer.</summary>
        private static void TrySeed(string name, string email, string pass, string role)
        {
            try { Register(name, email, pass, role); } catch { /* already exists */ }
        }

        /// <summary>E-posta + şifre ile doğrulama.</summary>
        public static AppUser? Validate(string email, string password)
        {
            lock (_lockObj)
            {
                var hash = Hash(password);
                return _users.FirstOrDefault(u =>
                    u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                    u.PasswordHash == hash);
            }
        }

        /// <summary>Yeni kullanıcı kaydı. Aynı e-posta varsa hata atar.</summary>
        public static AppUser Register(string fullName, string email, string password, string role = "Customer")
        {
            lock (_lockObj)
            {
                if (_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Bu e-posta zaten kayıtlı.");

                var user = new AppUser
                {
                    Id = _seq++,
                    FullName = fullName.Trim(),
                    Email = email.Trim(),
                    PasswordHash = Hash(password),
                    Role = string.IsNullOrWhiteSpace(role) ? "Customer" : role.Trim()
                };
                _users.Add(user);
                return user;
            }
        }

        /// <summary>Var olan kullanıcının rolünü günceller (test için pratik).</summary>
        public static bool UpdateRole(string email, string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole)) return false;
            lock (_lockObj)
            {
                var u = _users.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (u == null) return false;
                u.Role = newRole.Trim();
                return true;
            }
        }

        /// <summary>E-postaya göre kullanıcıyı getir.</summary>
        public static AppUser? FindByEmail(string email)
        {
            lock (_lockObj)
                return _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Listeyi güvenli şekilde döndürür (kopya).</summary>
        public static IReadOnlyList<AppUser> AllUsers()
        {
            lock (_lockObj) return _users.ToList();
        }

        /// <summary>Şifreyi SHA256 ile hashler (demo içindir).</summary>
        public static string Hash(string s)
        {
            // EĞER SIFRE BOS GELIRSE HATA VERME, BOS STRING OLARAK KABUL ET
            if (string.IsNullOrEmpty(s)) return "";

            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }
    }
}
