using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ZedisServer.Services
{
    public interface IHashingService
    {
        string PasswordHashing(string password);
        bool VerifyPassword(string input, string storedHash); 
    }

    public class HashingService : IHashingService 
    {

        public string PasswordHashing(string password) 
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);


            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(salt.Concat(hash).ToArray());
        }

        public bool VerifyPassword(string input, string storedHash) 
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = hashBytes[..16];
            byte[] storedSubHash = hashBytes[16..];

            var pbkdf2 = new Rfc2898DeriveBytes(input, salt, 100000, HashAlgorithmName.SHA256);
            byte[] inputHash = pbkdf2.GetBytes(32);

            return storedSubHash.SequenceEqual(inputHash);
        }

    }
}