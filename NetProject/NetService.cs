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
        /// 
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IOComplete_Init(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.None:
                    break;
                case SocketAsyncOperation.Accept:
                    break;
                case SocketAsyncOperation.Connect:
                    break;
                case SocketAsyncOperation.Disconnect:
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.ReceiveFrom:
                    break;
                case SocketAsyncOperation.ReceiveMessageFrom:
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.SendPackets:
                    break;
                case SocketAsyncOperation.SendTo:
                    break;
            }
        }

        void ProcessReceive(SocketAsyncEventArgs e)
        {

        }

        void ProcessSend(SocketAsyncEventArgs e)
        {

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
