using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IConnection
    {
        void SendMessage(byte[] msg);
        void Close();

        NewMessageDelegate OnNewMessage { get; set; }
    }
}
