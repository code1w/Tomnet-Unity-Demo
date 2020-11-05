using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomNet.Exceptions
{
    public class TomNetError : Exception
    {
        public TomNetError(string message):base(message)
        {
        }
    }
}
