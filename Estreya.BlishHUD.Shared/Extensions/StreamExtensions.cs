namespace Estreya.BlishHUD.Shared.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class StreamExtensions
    {
        public static byte[] ToByteArray(this Stream input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input is MemoryStream memStream) return memStream.ToArray();

            byte[] buffer = new byte[16 * 1024];
            using MemoryStream ms = new MemoryStream();
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }
}
