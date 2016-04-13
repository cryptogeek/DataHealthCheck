using System;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Security.Cryptography;

namespace DataHealthCheck
{
    public class sha1Class
    {
        public static String sha1String;
        static byte[] buffer = new byte[1024 * 80]; //got best performance with this buffer size
        static int bytesRead;
        static long size;
        static long totalBytesRead;
        public static void sha1Method(String fileString)
        {
            using (FileStream file = new FileStream(fileString, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) //no need to close filestream with using
            {
                totalBytesRead = 0;
                size = file.Length;
                if (size > 0)
                {
                    using (HashAlgorithm hasher = SHA1.Create())
                    {
                        do
                        {
                            bytesRead = file.Read(buffer, 0, buffer.Length);
                            totalBytesRead += bytesRead;
                            hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                            Form1.progressBar1.Invoke(new MethodInvoker(delegate
                            {
                                Form1.progressBar1.Value = (int)((double)totalBytesRead / (size) * 100);
                            }));
                        }
                        while (bytesRead != 0);

                        hasher.TransformFinalBlock(buffer, 0, 0);

                        //comparison of bytes to string methods
                        //http://stackoverflow.com/a/624379

                        //md5StringBuilder = new StringBuilder(32);
                        //foreach (byte b in hasher.Hash)
                        //    md5StringBuilder.Append(b.ToString("x2"));
                        //md5String = md5StringBuilder.ToString();

                        //md5String = BitConverter.ToString(hasher.Hash);
                        //md5String.Replace("-", "");

                        //fastest bytes to string method as of 2014/10/13
                        sha1String = ByteArrayToHexViaLookup32Class.ByteArrayToHexViaLookup32(hasher.Hash);
                    }
                }
                else
                {
                    sha1String = "da39a3ee5e6b4b0d3255bfef95601890afd80709";
                }
            }
        }
    }
}
