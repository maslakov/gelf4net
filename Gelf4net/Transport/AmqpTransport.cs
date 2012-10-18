using System;
using RabbitMQ.Client;

namespace Esilog.Gelf4net.Transport
{
    internal class AmqpTransport : GelfTransport
    {
        public String VirtualHost
        {
            get;
            set;
        }

        public String User
        {
            get;
            set;
        }

        public String Password
        {
            get;
            set;
        }

        public String Queue
        {
            get;
            set;
        }

        public override void Send( String serverHostName, String serverIpAddress, Int32 serverPort, String message )
        {
            //Create the Connection 
            var factory = new ConnectionFactory
            {
                Protocol = Protocols.FromEnvironment(),
                HostName = serverIpAddress,
                Port = serverPort,
                VirtualHost = VirtualHost,
                UserName = User,
                Password = Password
            };

            using( IConnection conn = factory.CreateConnection() )
            {
                var model = conn.CreateModel();
                model.ExchangeDeclare( "sendExchange", ExchangeType.Direct );
                model.QueueDeclare( Queue, true, true, true, null );
                model.QueueBind( Queue, "sendExchange", "key" );
                Byte[] messageBodyBytes = GzipMessage( message );
                model.BasicPublish( Queue, "key", null, messageBodyBytes );
            }
        }
    }
}