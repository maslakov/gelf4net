using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Esilog.Gelf4net.Transport;
using log4net.Appender;

namespace Esilog.Gelf4net.Appender
{
    public class Gelf4NetAppender : AppenderSkeleton
    {
        public static String UNKNOWN_HOST = "unknown_host";
        private GelfTransport _transport;
        private String _additionalFields;
        private Int32 _maxChunkSize;

        private Dictionary<String, String> innerAdditionalFields;

        public String AdditionalFields
        {
            get
            {
                return _additionalFields;
            }
            set
            {
                _additionalFields = value;

                if( _additionalFields != null )
                {
                    innerAdditionalFields = new Dictionary<String, String>();
                }
                else
                {
                    innerAdditionalFields.Clear();
                }
                innerAdditionalFields = _additionalFields.Split( ',' ).ToDictionary(
                    it => it.Split( ':' )[0], it => it.Split( ':' )[1] );
            }
        }

        public String Facility
        {
            get;
            set;
        }

        public String GrayLogServerHost
        {
            get;
            set;
        }

        public String GrayLogServerHostIpAddress
        {
            get;
            set;
        }

        public Int32 GrayLogServerPort
        {
            get;
            set;
        }

        public String Host
        {
            get;
            set;
        }

        public Boolean IncludeLocationInformation
        {
            get;
            set;
        }

        public Boolean UseUdpTransport
        {
            get;
            set;
        }

        public Int32 MaxChunkSize
        {
            get
            {
                return _maxChunkSize;
            }
            set
            {
                _maxChunkSize = value;
                if( UseUdpTransport )
                {
                    ((UdpTransport)_transport).MaxChunkSize = value;
                }
            }
        }

        public Int32 GrayLogServerAmqpPort
        {
            get;
            set;
        }

        public String GrayLogServerAmqpUser
        {
            get;
            set;
        }

        public String GrayLogServerAmqpPassword
        {
            get;
            set;
        }

        public String GrayLogServerAmqpVirtualHost
        {
            get;
            set;
        }

        public String GrayLogServerAmqpQueue
        {
            get;
            set;
        }

        public Gelf4NetAppender()
        {
            Facility = null;
            GrayLogServerHost = "";
            GrayLogServerHostIpAddress = "";
            GrayLogServerPort = 12201;
            Host = null;
            IncludeLocationInformation = false;
            MaxChunkSize = 1024;
            UseUdpTransport = true;
            GrayLogServerAmqpPort = 5672;
            GrayLogServerAmqpUser = "guest";
            GrayLogServerAmqpPassword = "guest";
            GrayLogServerAmqpVirtualHost = "/";
            GrayLogServerAmqpQueue = "queue1";
        }

        public override void ActivateOptions()
        {
            _transport = (UseUdpTransport)
                             ? new UdpTransport { MaxChunkSize = MaxChunkSize }
                             : (GelfTransport)new AmqpTransport
                             {
                                 VirtualHost = GrayLogServerAmqpVirtualHost,
                                 User = GrayLogServerAmqpUser,
                                 Password = GrayLogServerAmqpPassword,
                                 Queue = GrayLogServerAmqpQueue
                             };
        }

        protected override void Append( log4net.Core.LoggingEvent loggingEvent )
        {
            String message = (Layout != null)
                                 ? base.RenderLoggingEvent( loggingEvent )
                                 : loggingEvent.MessageObject!=null ? loggingEvent.MessageObject.ToString() : loggingEvent.RenderedMessage;

            String gelfJsonString = new GelfJsonBuilder().BuildFromLoggingEvent(
                message,
                loggingEvent, GetLoggingHostName(), Facility, IncludeLocationInformation, innerAdditionalFields );

            if( UseUdpTransport )
            {
                SendGelfMessageToGrayLog( gelfJsonString );
            }
            else
            {
                SendAmqpMessageToGrayLog( gelfJsonString );
            }
        }

        private void SendGelfMessageToGrayLog( String message )
        {
            if( GrayLogServerHostIpAddress == String.Empty )
            {
                GrayLogServerHostIpAddress = GetIpAddressFromHostName();
            }
            _transport.Send( GetLoggingHostName(), GrayLogServerHostIpAddress, GrayLogServerPort, message );
        }

        private void SendAmqpMessageToGrayLog( String message )
        {
            if( GrayLogServerHostIpAddress == String.Empty )
            {
                GrayLogServerHostIpAddress = GetIpAddressFromHostName();
            }
            _transport.Send( GetLoggingHostName(), GrayLogServerHostIpAddress, GrayLogServerAmqpPort, message );
        }

        private String GetLoggingHostName()
        {
            String ret = Host;
            if( ret == null )
            {
                try
                {
                    ret = System.Net.Dns.GetHostName();
                }
                catch( SocketException )
                {
                    ret = UNKNOWN_HOST;
                }
            }
            return ret;
        }

        private String GetIpAddressFromHostName()
        {
            IPAddress[] addresslist = Dns.GetHostAddresses( GrayLogServerHost );
            return addresslist[0].ToString();
        }
    }
}