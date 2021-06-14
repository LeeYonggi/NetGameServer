using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetProject
{
    public class UserToken
    {
        enum TOKEN_STATE
        {
            IDLE,
            CONNECTED,
            RESERVECLOSING,
            CLOSED
        }

        // Closing require. S -> C
        const short SYS_CLOSE_REQ = 0;
        // Closing response. C -> S
        const short SYS_CLOSE_ACK = -1;
        // Start heartbeat
        public const short SYS_START_HEARTBEAT = -2;
        // Update heartbeat
        public const short SYS_UPDATE_HEARTBEAT = -3;

        int is_closed;

        TOKEN_STATE currentState;

        public Socket Socket { get; set; }

        public SocketAsyncEventArgs receive_event_args { get; private set; }
        public SocketAsyncEventArgs send_event_args { get; private set; }

        
    }
}
