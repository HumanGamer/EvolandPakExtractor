using ICSharpCode.SharpZipLib.Checksums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolandPakExtractor
{
    public static class Utils
    {
        public static uint Adler32Checksum(this byte[] data, int startIndex, int length)
        {
            Adler32 checksum = new Adler32();

            checksum.Update(data, startIndex, length);

            return (uint)checksum.Value;
        }

        public static uint Adler32Checksum(this byte[] data)
        {
            return data.Adler32Checksum(0, data.Length);
        }

        public static uint Adler32Checksum(string path)
		{
            using (Stream s = File.OpenRead(path))
            using (BinaryReader br = new BinaryReader(s))
            {
                return br.ReadBytes((int)s.Length).Adler32Checksum();
            }
		}
    }
}
