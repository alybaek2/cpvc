﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IConnection
    {
        bool SendMessage(byte[] msg);
        void Close();

        event NewMessageDelegate OnNewMessage;
        event CloseConnectionDelegate OnCloseConnection;

        bool IsConnected { get; }
    }
}
