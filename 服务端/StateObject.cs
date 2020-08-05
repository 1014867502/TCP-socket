using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace _11111
{
    public class StateObject
    {
        public Socket socket = null;
        public const int bufferSizes = 1024 * 2;
        public byte[] buffer = new byte[bufferSizes];
        public StringBuilder sb = new StringBuilder();
    }
}
