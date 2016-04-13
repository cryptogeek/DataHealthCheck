using System;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Security.Cryptography;

namespace DataHealthCheck
{
    public class md5Class
    {
        public static String md5String;
        static byte[] buffer = new byte[1024 * 80]; //got best performance with this buffer size
        static int bytesRead;
        static long size;
        static long totalBytesRead;
        public static void md5Method(String fileString)
        {
            using (FileStream file = new FileStream(fileString, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) //no need to close filestream with using
            {
                totalBytesRead = 0;
                size = file.Length;
                if (size > 0)
                {
                    using (HashAlgorithm hasher = MD5.Create()) 
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
                        md5String = ByteArrayToHexViaLookup32Class.ByteArrayToHexViaLookup32(hasher.Hash);
                    }
                }
                else
                {
                    md5String = "d41d8cd98f00b204e9800998ecf8427e";
                }
            }           
        }
    }
}
