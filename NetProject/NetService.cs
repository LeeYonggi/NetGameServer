using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetProject
{
    public class NetService
    {
        //** Initialize in constructor
        SocketAsyncEventArgsPool mSocketPool;  // Pool for async socket efficiency
        BufferManager mBufferManager;         // represents a large reusable set of buffer for all socket operation
        Socket listenSocket;

        Semaphore mMaxNumberAcceptedClients;

        private int mNumConnections;
        private int mReceiveBufferSize;
        private int mTotalBytesRead;
        private int mNumConnectedSockets;

        const int opsToPreAlloc = 2;

        public NetService(int numConnections, int receiveBufferSize)
        {
            mNumConnections = numConnections;
            mReceiveBufferSize = receiveBufferSize;
            mTotalBytesRead = 0;
            mNumConnectedSockets = 0;

            mBufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc, receiveBufferSize);

            mSocketPool = new SocketAsyncEventArgsPool(numConnections);
            mMaxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        /// <summary>
        /// Initialize pool
        /// </summary>
        public void Init()
        {
            mBufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg = null;

            for(int i = 0; i < mNumConnections; i++) 
            {
                //** Receive pool
                {
                    readWriteEventArg = new SocketAsyncEventArgs();
                    readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IOComplete_Init);
                    readWriteEventArg.UserToken = null;

                    mBufferManager.SetBuffer(readWriteEventArg);

                    mSocketPool.Push(readWriteEventArg);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localEndPoint"></param>
        /// <param name="backLog"></param>
        public void Start(IPEndPoint localEndPoint, int backLog)
        {
            // Create Socket required for receive and async
            // It just need only one
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);

            // Start the server with a listen backlog of 100 connections
            listenSocket.Listen(backLog);

            StartAccept(null);

            Utils.DebugLog("Press any key to terminate the server process....");
        }

        /// <summary>
        /// the function when socket accept client socket
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if(acceptEventArg == null)
            {
                // The single SocketAsyncEventArgs will only be used where accept
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            // Semaphore thread waiting
            mMaxNumberAcceptedClients.WaitOne();

            bool willRaiseEvent = true;
            try
            {
                willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            }
            catch(SocketException e)
            {
                // Will send exception message when socket accepts
            }

            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        /// <summary>
        /// A function to run when the socket's asynchronous accept is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IOComplete_Init(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new AggregateException("The last operation completed on the socket was not a receive or send");
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Interlocked.Increment(ref mNumConnectedSockets);
                Console.WriteLine($"Client connection accepted. There are {mNumConnections} clients connected to the server");

                // Get SocketAsyncEventArgs in socket pool
                SocketAsyncEventArgs readEventArgs = mSocketPool.Pop();
                Socket clientAcceptSocket = e.AcceptSocket;

                // use nagle algorithm
                clientAcceptSocket.NoDelay = true;

                // Get UserToken
                //e.AcceptSocket;

                // Calls a function that waits for a message to be received
                // It will call iocomplete function
                bool willRaiseEvent = clientAcceptSocket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs);
                }
            }
            else
            {
                Console.WriteLine($"Client Failed accepted. Socket Error: {e.SocketError}");
            }

            // Accept the net connection request
            StartAccept(e);
        }

        void ProcessReceive(SocketAsyncEventArgs e)
        {
            // Get user token in SocketAsyncEventArgs param
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // increment the count of the total bytes receive by the server
                Interlocked.Add(ref mTotalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", mTotalBytesRead);

                e.SetBuffer(e.Offset, e.BytesTransferred);
                // Send user token
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // Send user token
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            Interlocked.Decrement(ref mNumConnectedSockets);

            mSocketPool.Push(e);

            mMaxNumberAcceptedClients.Release();
            Console.WriteLine($"A Client has been disconnected from the server. There are {mNumConnectedSockets} clients connected to the server");
        }
    }
}
