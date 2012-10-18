using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Esilog.Gelf4net.Transport
{
    public class UdpTransport : GelfTransport
    {
        public Int32 MaxChunkSize
        {
            get;
            set;
        }

        private Int32 _maxHeaderSize = 8;

        public override void Send( String serverHostName, String serverIpAddress, Int32 serverPort, String message )
        {
            var ipAddress = IPAddress.Parse( serverIpAddress );
            IPEndPoint ipEndPoint = new IPEndPoint( ipAddress, serverPort );

            using( UdpClient udpClient = new UdpClient() )
            {
                var gzipMessage = GzipMessage( message );

                if( MaxChunkSize < gzipMessage.Length )
                {
                    var chunkCount = (gzipMessage.Length / MaxChunkSize) + 1;
                    var messageId = GenerateMessageId( serverHostName );
                    for( Int32 i = 0; i < chunkCount; i++ )
                    {
                        var messageChunkPrefix = CreateChunkedMessagePart( messageId, i, chunkCount );
                        var skip = i * MaxChunkSize;
                        var messageChunkSuffix = gzipMessage.Skip( skip ).Take( MaxChunkSize ).ToArray();

                        var messageChunkFull = new Byte[messageChunkPrefix.Length + messageChunkSuffix.Length];
                        messageChunkPrefix.CopyTo( messageChunkFull, 0 );
                        messageChunkSuffix.CopyTo( messageChunkFull, messageChunkPrefix.Length );

                        udpClient.Send( messageChunkFull, messageChunkFull.Length, ipEndPoint );
                    }
                }
                else
                {
                    udpClient.Send( gzipMessage, gzipMessage.Length, ipEndPoint );
                }
            }
        }

        public Byte[] CreateChunkedMessagePart( String messageId, Int32 index, Int32 chunkCount )
        {
            var result = new List<Byte>();
            result.Add( Convert.ToByte( 30 ) );
            result.Add( Convert.ToByte( 15 ) );
            result.AddRange( Encoding.Default.GetBytes( messageId ).ToArray() );
            result.Add( Convert.ToByte( index ) );
            result.Add( Convert.ToByte( chunkCount ) );

            return result.ToArray<Byte>();
        }

        public String GenerateMessageId( String serverHostName )
        {
            var md5String = String.Join(
                "",
                MD5.Create().ComputeHash( Encoding.Default.GetBytes( serverHostName ) ).Select(
                    it => it.ToString( "x2" ) ).ToArray() );
            var random = new Random( (Int32)DateTime.Now.Ticks );
            var sb = new StringBuilder();
            var t = DateTime.Now.Ticks % 1000000000;
            var s = String.Format( "{0}{1}", md5String.Substring( 0, 10 ), md5String.Substring( 20, 10 ) );
            var r = random.Next( 10000000 ).ToString( "00000000" );

            sb.Append( t );
            sb.Append( s );
            sb.Append( r );

            //Message ID: 8 bytes 
            return sb.ToString().Substring( 0, _maxHeaderSize );
        }
    }
}