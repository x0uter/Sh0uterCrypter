using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Encrypter
{
    class Encrypt
    {
        private const int BUFFER_SIZE = 128 * 1024;
        private const ulong FC_TAG = 0x0000000000000001;
        private static RandomNumberGenerator rand = new RNGCryptoServiceProvider();
        private string password;
        
        public Encrypt(string password)
        {
            this.password = password;
        }

        public void allFiles(List<FileInfo> allFiles, CancellationToken ct)
        {
            try
            {
                Parallel.ForEach(allFiles, new ParallelOptions() { CancellationToken = ct },
                    (file) => {
                        letsEncrypt(file);
                    });
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void letsEncrypt(FileInfo file)
        {
            using (FileStream fin = File.OpenRead(file.Directory +@"\"+ file.Name),
            fout = File.OpenWrite(file.Directory + @"\" + file.Name + ".enc"))
            {
                    long lSize = fin.Length;
                    int size = (int)lSize;
                    byte[] bytes = new byte[BUFFER_SIZE];
                    int read = -1;
                    int value = 0;

                    byte[] IV = GenerateRandomBytes(16);
                    byte[] salt = GenerateRandomBytes(16);

                    SymmetricAlgorithm sma = CreateRijndael(password, salt);
                    sma.IV = IV;

                    fout.Write(IV, 0, IV.Length);
                    fout.Write(salt, 0, salt.Length);

                    HashAlgorithm hasher = SHA256.Create();
                    using (CryptoStream cout = new CryptoStream(fout, sma.CreateEncryptor(),
                        CryptoStreamMode.Write),
                          chash = new CryptoStream(Stream.Null, hasher,
                            CryptoStreamMode.Write))
                    {
                        BinaryWriter bw = new BinaryWriter(cout);
                        bw.Write(lSize);

                        bw.Write(FC_TAG);

                        while ((read = fin.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            cout.Write(bytes, 0, read);
                            chash.Write(bytes, 0, read);
                            value += read;
                        }

                        chash.Flush();
                        chash.Close();

                        byte[] hash = hasher.Hash;

                        cout.Write(hash, 0, hash.Length);

                        cout.Flush();
                        cout.Close();
                    }
            }
            file.Delete();
        }

        private static byte[] GenerateRandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            rand.GetBytes(bytes);
            return bytes;
        }

        private static SymmetricAlgorithm CreateRijndael(string password, byte[] salt)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, salt, "SHA256", 1000);

            SymmetricAlgorithm sma = Rijndael.Create();
            sma.KeySize = 256;
            sma.Key = pdb.GetBytes(32);
            sma.Padding = PaddingMode.PKCS7;
            return sma;
        }

    }
}