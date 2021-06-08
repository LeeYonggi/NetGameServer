using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetProject
{
    /// <summary>
    /// Pool Manage SocketAsyncEventArgs object
    /// </summary>
    public class SocketAsyncEventArgsPool
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
            if (item == null) { throw new ArgumentNullException("Item added a SocketAsyncEventArgsPool cannot be found"); }
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
}
