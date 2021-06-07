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
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            // Semaphore thread waiting
            mMaxNumberAcceptedClients.WaitOne();

            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
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
            Interlocked.Increment(ref mNumConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server", mNumConnections);

            SocketAsyncEventArgs readEventArgs = mSocketPool.Pop();
            // Get UserToken
            //e.AcceptSocket;

            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
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

        #region SocketAsyncEventArgsPool
        /// <summary>
        /// Pool Manage SocketAsyncEventArgs object
        /// </summary>
        private class SocketAsyncEventArgsPool
        {
            //** Initialize in constructor
            Stack<SocketAsyncEventArgs> mPool;

            // Initialize SocketAsyncEventArgs pool to the specified size
            public SocketAsyncEventArgsPool(int capacity)
            {
                mPool = new Stack<SocketAsyncEventArgs>(capacity);
            }

            /// <summary>
            /// Push item to SocketAsyncEventArgs object pool
            /// </summary>
            /// <param name="item">SocektAsyncEventArgs instance</param>
            public void Push(SocketAsyncEventArgs item)
            {
                if(item == null) { throw new ArgumentNullException("Item added a SocketAsyncEventArgsPool cannot be found"); }
                lock (mPool)
                {
                    mPool.Push(item);
                }
            }

            /// <summary>
            /// Return item that taked out in SocketAsyncEventArgs stack
            /// </summary>
            /// <returns>SocketArgs instance</returns>
            public SocketAsyncEventArgs Pop()
            {
                lock (mPool)
                {
                    return mPool.Pop();
                }
            }

            public int Count
            {
                get { return mPool.Count; }
            }
        }
        #endregion
    }
}
