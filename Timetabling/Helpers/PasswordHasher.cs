using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;

namespace Timetabling.Helpers
{
    public static class PasswordHasher
    {
        // Hash a plain text password using SHA-256 + salt
        public static string Hash(string password)
        {
            // Use a fixed salt combined with the password
            // For production use BCrypt — this is suitable for a final year project
            string salted = "TimetableDB_Salt_2026_" + password;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(salted);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash)
                    .Replace("-", "")
                    .ToLower();
            }
        }

        // Compare a plain text password against a stored hash
        public static bool Verify(string password, string storedHash)
        {
            var hash = Hash(password);
            return string.Equals(hash, storedHash,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}