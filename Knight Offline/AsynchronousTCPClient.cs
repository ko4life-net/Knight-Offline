using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Knight_Offline
{
    public class StateObject
    {
        public Socket Socket;
        public const int BufferSize = 8192; // CUser::Parsing -> buff[2048] vs ByteBuffer(4096) vs #define MAX_PACKET_SIZE (1024*8)
        public byte[] Buffer = new byte[BufferSize];
        public byte[] PacketBuffer;
    }

    // Still buggy
    class AsynchronousTCPClient
    {
        private Socket Socket;
        private int SocketID; // SocketID aka InstanceID
        private ConcurrentQueue<byte[]> ReceivedPacketsQueue;
        // Delegates not implemented yet
        public delegate void ConnectionCompleteEventDelegate(AsynchronousTCPClient Sender, int SocketID);
        public delegate void DisconnectionCompleteEventDelegate(AsynchronousTCPClient Sender, int SocketID);
        public delegate void SendCompleteEventDelegate(AsynchronousTCPClient Sender, int SocketID, byte[] Packet);
        public delegate void DataReceivedEventDelegate(AsynchronousTCPClient Sender, int SocketID, byte[] Packet);
        public event ConnectionCompleteEventDelegate ConnectionComplete;
        public event DisconnectionCompleteEventDelegate DisconnectionComplete;
        public event SendCompleteEventDelegate SendComplete;
        public event DataReceivedEventDelegate DataReceived;

        public AsynchronousTCPClient(ConcurrentQueue<byte[]> ReceivedPacketsQueue/*, int ID*/)
        {
            this.ReceivedPacketsQueue = ReceivedPacketsQueue;
            // TODO: Implement saving network traffic to a file 
            // SocketID = ID;
        }

        public void Connect(string ServerIP, int Port)
        {
            try
            {
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ServerIP), Port), new AsyncCallback(ConnectCallback), Socket);
                Receive(Socket);
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString());
                throw e;
            }
        }

        public void Disconnect()
        {
            if (Socket.Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();

                // TODO: Add Socket.BeginDisconnect()

                if (DisconnectionComplete != null)
                {
                    DisconnectionComplete.Invoke(this, SocketID);
                }
            }
        }

        public void Send(byte[] Packet)
        {
            if (Socket.Connected)
            {
                StateObject State = new StateObject
                {
                    Socket = Socket,
                    PacketBuffer = Packet.Take(Packet.Length).ToArray()
                };

                Socket.BeginSend(Packet, 0, Packet.Length, 0, new AsyncCallback(SendCallback), State);
            }
        }

        public void Receive(Socket Socket)
        {
            try
            {
                StateObject State = new StateObject
                {
                    Socket = Socket
                };

                Socket.BeginReceive(State.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), State);
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString());
                throw e;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket Socket = (Socket)ar.AsyncState;
                Socket.EndConnect(ar);

                if (Socket.Connected)
                {
                    if (ConnectionComplete != null)
                    {
                        ConnectionComplete.Invoke(this, SocketID);
                    }
                }
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString());
                throw e;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                StateObject State = (StateObject)ar.AsyncState;
                Socket Socket = State.Socket;                

                if (Socket.Connected)
                {
                    int BytesSent = Socket.EndSend(ar);
                    SendComplete?.Invoke(this, SocketID, State.PacketBuffer);
                }
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString());
                throw e;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject State = (StateObject)ar.AsyncState;
                Socket Socket = State.Socket;

                // Remove it and try to close connections ;)
                if (Socket.Connected)
                {
                    int BytesRead = Socket.EndReceive(ar);

                    if (BytesRead > 0)
                    {
                        PacketParser.DefragmentPackets(State.Buffer.Take(BytesRead).ToArray()).ForEach(x => ReceivedPacketsQueue.Enqueue(x)); // Magic here
                        Socket.BeginReceive(State.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), State);
                        DataReceived?.Invoke(this, SocketID, State.Buffer.Take(BytesRead).ToArray());
                    }
                    else
                    {
                        // ?
                    }
                }
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString());
                throw e;
            }
        }
    }
}