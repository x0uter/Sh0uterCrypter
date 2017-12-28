using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Decrypter
{
    public class Sh0uterCrypterException : ApplicationException
    {
        public Sh0uterCrypterException(string ErrorMsg) : base(ErrorMsg) { }
    }

    class Decrypt
    {
        private string password;
        private const int BUFFER_SIZE = 128 * 1024;
        private const ulong FC_TAG = 0x0000000000000001;
        public Decrypt(string password)
        {
            this.password = password;
        }

        public void allFiles(List<FileInfo> allFiles, CancellationToken ct)
        {
            try
            {
                Parallel.ForEach(allFiles, new ParallelOptions() { CancellationToken = ct },
                    (file) => {
                        letsDecrypt(file);
                    });
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }


        public void letsDecrypt(FileInfo file)
        {
            using (FileStream fin = File.OpenRead(file.Directory + @"\" + file.Name),
                      fout = File.OpenWrite(file.Directory + @"\" + file.Name.Substring(0, file.Name.Length - 4)))
            {
                int size = (int)fin.Length;
                byte[] bytes = new byte[BUFFER_SIZE];
                int read = -1;
                int value = 0;
                int outValue = 0;

                byte[] IV = new byte[16];
                fin.Read(IV, 0, 16);
                byte[] salt = new byte[16];
                fin.Read(salt, 0, 16);

                SymmetricAlgorithm sma = CreateRijndael(password, salt);
                sma.IV = IV;

                value = 32;
                long lSize = -1;

                HashAlgorithm hasher = SHA256.Create();

                using (CryptoStream cin = new CryptoStream(fin, sma.CreateDecryptor(), CryptoStreamMode.Read),
                          chash = new CryptoStream(Stream.Null, hasher, CryptoStreamMode.Write))
                {
                    BinaryReader br = new BinaryReader(cin);
                    lSize = br.ReadInt64();
                    ulong tag = br.ReadUInt64();

                    if (!FC_TAG.Equals(tag))
                        throw new Sh0uterCrypterException("File Corrupted!");

                    long numReads = lSize / BUFFER_SIZE;

                    long slack = (long)lSize % BUFFER_SIZE;

                    for (int i = 0; i < numReads; ++i)
                    {
                        read = cin.Read(bytes, 0, bytes.Length);
                        fout.Write(bytes, 0, read);
                        chash.Write(bytes, 0, read);
                        value += read;
                        outValue += read;
                    }

                    if (slack > 0)
                    {
                        read = cin.Read(bytes, 0, (int)slack);
                        fout.Write(bytes, 0, read);
                        chash.Write(bytes, 0, read);
                        value += read;
                        outValue += read;
                    }
                  
                    chash.Flush();
                    chash.Close();

                    fout.Flush();
                    fout.Close();

                    byte[] curHash = hasher.Hash;

                    byte[] oldHash = new byte[hasher.HashSize / 8];
                    read = cin.Read(oldHash, 0, oldHash.Length);
                    if ((oldHash.Length != read) || (!CheckByteArrays(oldHash, curHash)))
                        throw new Sh0uterCrypterException("File Corrupted!");
                }
            }
            file.Delete();
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

        private static bool CheckByteArrays(byte[] b1, byte[] b2)
        {
            if (b1.Length == b2.Length)
            {
                for (int i = 0; i < b1.Length; ++i)
                {
                    if (b1[i] != b2[i])
                        return false;
                }
                return true;
            }
            return false;
        }

    }
}