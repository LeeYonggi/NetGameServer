using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetProject
{
    class Defines
    {
        public static readonly short HEADERSIZE = 4;
    }

    public delegate void CompletedMessageCallback(ArraySegment<byte> buffer);

    /// <summary>
    /// [head][body] A class that parses structured data
    /// - header : Data size. It has the same size as the type defined Defines.HEADERSIZE
    ///            In the case of 2byte, Int16, and 4byte as Int32
    ///            If main text not over Int16.Max value, you can to use 2byte.
    /// - body   : Message main text.
    /// </summary>
    class MessageResolver
    {
        // Message size
        int messageSize;

        // In progress buffer
        byte[] messageBuffer = new byte[1024];

        // A variable That indicate index of buffer in progress
        // If you complete one packet, initialize it 0 after
        int currentPosition;

        // Target position to read
        int positionToRead;

        // Remaining size
        int remainBytes;

        public MessageResolver()
        {
            messageSize = 0;
            currentPosition = 0;
            positionToRead = 0;
            remainBytes = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="srcPosition"></param>
        /// <returns></returns>
        bool ReadUntil(byte[] buffer, ref int srcPosition)
        {
            int copySize = positionToRead - currentPosition;

            // Copy only as much as possible if remain data less copysize
            if (remainBytes < copySize)
            {
                copySize = remainBytes;
            }

            // Copy buffer
            Array.Copy(buffer, srcPosition, messageBuffer, currentPosition, copySize);

            // Move position main text buffer
            srcPosition += copySize;

            currentPosition += copySize;

            remainBytes -= copySize;

            if (currentPosition < positionToRead)
                return false;

            return true;
        }

        public void OnReceive(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
        {
            remainBytes = transffered;

            int srcPosition = offset;

            while(remainBytes > 0)
            {
                bool completed = false;

                if (currentPosition < Defines.HEADERSIZE)
                {
                    positionToRead = Defines.HEADERSIZE;

                    completed = ReadUntil(buffer, ref srcPosition);

                    if (completed)
                    {
                        // Not yet to read so wait next receive
                        return;
                    }

                    messageSize = GetTotalMessageSize();

                    // It was wrong message if size less than zero.
                    if(messageSize <= 0)
                    {
                        ClearBuffer();
                        return;
                    }

                    // Next target position
                    positionToRead = messageSize;

                    if(remainBytes <= 0)
                    {
                        return;
                    }
                }

                // Read message
                completed = ReadUntil(buffer, ref srcPosition);

                if(completed)
                {
                    byte[] clone = new byte[positionToRead];
                    Array.Copy(messageBuffer, clone, positionToRead);
                    ClearBuffer();
                    callback(new ArraySegment<byte>(clone, 0, positionToRead));
                }
            }
        }

        int GetTotalMessageSize()
        {
            if(Defines.HEADERSIZE == 2)
            {
                return BitConverter.ToInt16(messageBuffer, 0);
            }
            else if(Defines.HEADERSIZE == 4)
            {
                return BitConverter.ToInt32(messageBuffer, 0);
            }

            return 0;
        }

        public void ClearBuffer()
        {
            Array.Clear(messageBuffer, 0, messageBuffer.Length);

            currentPosition = 0;
            messageSize = 0;
        }
    }
}
