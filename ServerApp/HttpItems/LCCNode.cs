using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    internal class LCCNode
    {
        private HttpListenerContext _context;


        public HttpListenerContext Context
        {
            get => _context;
            set => _context = value;
        }

        internal LCCNode()
        {
        }
    }
}
