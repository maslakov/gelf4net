using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Esilog.Gelf4net.Transport
{
    public abstract class GelfTransport
    {
        public abstract void Send( String serverHostName, String serverIpAddress, Int32 serverPort, String message );

        protected Byte[] GzipMessage( String message )
        {
            // TODO: fix here is possible memory leak via LOH if messages are lagre. possible to use BufferPool
            Byte[] buffer = Encoding.UTF8.GetBytes( message );
            var ms = new MemoryStream();
            using( var zip = new System.IO.Compression.GZipStream( ms, CompressionMode.Compress, true ) )
            {
                zip.Write( buffer, 0, buffer.Length );
            }
            ms.Position = 0;
            Byte[] compressed = new Byte[ms.Length];
            ms.Read( compressed, 0, compressed.Length );
            return compressed;
        }
    }
}