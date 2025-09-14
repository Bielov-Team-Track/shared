using System.Security.Cryptography;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Services
{
    public class HashingService : IHashingService
    {
        public string Hash(string value)
        {
            //TODO: remove obsolete code
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            // STEP 2: Create the Rfc2898DeriveBytes and get the hash value
            var pbkdf2 = new Rfc2898DeriveBytes(value, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            // STEP 3: Combine the salt and password bytes for later use
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            // STEP 4: Turn the combined salt+hash into a string for storage
            return Convert.ToBase64String(hashBytes);
        }

    }
}
