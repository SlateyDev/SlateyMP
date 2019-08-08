using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace SlateyMP.Server.Login {
    public static class ManagementFunctions {
        private static void CreateUser(string username, string password) {
            SHA256Managed sha = new SHA256Managed();
            byte[] passwordHash = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", username, password.ToUpper())));

            var random = new RNGCryptoServiceProvider();
            var random_salt = new byte[20];
            random.GetBytes(random_salt);
            random_salt[random_salt.Length - 1] &= 0x7F;
            var passwordSalt = new BigInteger(random_salt);
            var x = new BigInteger(sha.ComputeHash(passwordHash.Concat(random_salt).ToArray()).Concat(new byte[] { 0 }).ToArray());
            var g = new BigInteger(new byte[] { 7 });
            var N = new BigInteger(new byte[] { 137, 75, 100, 94, 137, 225, 83, 91, 189, 173, 91, 139, 41, 6, 80, 83, 8, 1, 177, 142, 191, 191, 94, 143, 171, 60, 130, 135, 42, 62, 155, 183, 0 });
            BigInteger passwordVerifier = BigInteger.ModPow(g, x, N);
            MySqlCommand sqlcmd = new MySqlCommand(string.Format("INSERT INTO meteordb.account (username, passwordHash, salt, verifier, accesslevel, banned) VALUES ('{0}','{1}','{2}','{3}',0,0)", username, Convert.ToBase64String(passwordHash), passwordSalt, passwordVerifier), Program.db);
            sqlcmd.ExecuteScalar();
        }
    }
}