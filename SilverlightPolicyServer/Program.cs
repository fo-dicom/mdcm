// Implementation provided by Stack Overflow user 'luke'
// Detailed description of the implementation can be found here:
// http://stackoverflow.com/questions/2993735/silverlight-socket-unhandled-error-in-silverlight-application-an-attempt-was-mad

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SilverlightPolicyServer
{
    public abstract class Server
    {
        protected Socket Listener { get; set; }
        protected int Port { get; private set; }
        protected int Backlog { get; private set; }
        protected bool isStopped { get; set; }
        protected SocketAsyncEventArgs AcceptArgs { get; set; }

        public Server(int port)
        {
            AcceptArgs = new SocketAsyncEventArgs();
            AcceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
            isStopped = true;
            Port = port;
            Backlog = 100;
        }


        public Server(int port, int backlog)
        {
            isStopped = true;
            Port = port;
            Backlog = backlog;
        }

        public void Start()
        {
            isStopped = false;

            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, Port);
            Listener.ExclusiveAddressUse = true;
            Listener.Bind(ep);
            Console.WriteLine("Listening on " + Port);
            Listener.Listen(Backlog);

            Listener.AcceptAsync(AcceptArgs);
        }

        void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (isStopped) return;
            Socket client = e.AcceptSocket;
            Console.WriteLine("Accepted Connection From: " + client.RemoteEndPoint.ToString());
            e.AcceptSocket = null;
            Listener.AcceptAsync(AcceptArgs);
            HandleClient(client);
        }

        public virtual void Stop()
        {
            if (isStopped) throw new InvalidOperationException("Server already Stopped!");
            isStopped = true;
            try
            {
                Listener.Shutdown(SocketShutdown.Both);
                Listener.Close();
            }
            catch (Exception)
            {
            }
        }

        protected abstract void HandleClient(Socket Client);
    }
    public class PolicyServer : Server
    {
        public const String policyStr = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                                        <access-policy>
                                            <cross-domain-access>
                                                <policy>
                                                    <allow-from>
                                                        <domain uri=""*"" />
                                                    </allow-from>
                                                    <grant-to>
                                                        <socket-resource port=""4502-4534"" protocol=""tcp"" />
                                                    </grant-to>
                                                </policy>
                                            </cross-domain-access>
                                        </access-policy>";
        private byte[] policy = Encoding.ASCII.GetBytes(policyStr);
        private static string policyRequestString = "<policy-file-request/>";

        public PolicyServer()
            : base(943)
        {
        }

        protected override void HandleClient(Socket socket)
        {
            TcpClient client = new TcpClient { Client = socket };
            Stream s = client.GetStream();
            byte[] buffer = new byte[policyRequestString.Length];
            client.ReceiveTimeout = 5000;
            s.Read(buffer, 0, buffer.Length);//read in the request string, but don't do anything with it
            //you could confirm that it is equal to the policyRequestString
            s.Write(policy, 0, policy.Length);
            s.Flush();

            socket.Shutdown(SocketShutdown.Both);
            socket.Close(1);
            client.Close();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            PolicyServer ps = new PolicyServer();
            ps.Start();
            while (true) { }
        }
    }
}
