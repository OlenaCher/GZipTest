using System.IO;

namespace GZipTest
{
    public static class Extensions
    {
        //reads the bytes from the current stream to another stream - will be realized in .NET Framework from version 4.0
        public static void CopyTo(this Stream input, Stream output)
        {
            var buffer = new byte[64 * 1024];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, bytesRead);
        }
    }
}
