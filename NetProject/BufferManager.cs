using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetProject
{
    public class BufferManager 
    {
        //** Initialize in constructor
        int mNumBytes;
        byte[] mBuffer;
        Stack<int> mFreeIndexPool; 
        int mCurrentIndex;
        int mBufferSize;
        
        /// <summary>
        /// BufferManager constructor
        /// </summary>
        /// <param name="totalBytes">Total size of bytes</param>
        /// <param name="bufferSize">Current buffer size to specify</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            mNumBytes = totalBytes;
            mCurrentIndex = 0;
            mBufferSize = bufferSize;
            mFreeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// Initialize Buffer
        /// </summary>
        public void InitBuffer()
        {
            mBuffer = new byte[mNumBytes];
        }

        /// <summary>
        /// After checking if there is a current buffer, put it args
        /// </summary>
        /// <param name="args">Socketasync to put the buffer on</param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (mFreeIndexPool.Count > 0)
            {
                args.SetBuffer(mBuffer, mFreeIndexPool.Pop(), mBufferSize);
            }
            else
            {
                if ((mNumBytes - mBufferSize) < mCurrentIndex)
                {
                    return false;
                }
                args.SetBuffer(mBuffer, mCurrentIndex, mBufferSize);
                mCurrentIndex += mBufferSize;
            }

            return true;
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs object that buffer is to be removed</param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            mFreeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
