using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    [Serializable]
    class BlackDuckInspectorException : Exception
    {
        public BlackDuckInspectorException() : base()
        {
        }

        public BlackDuckInspectorException(string message) : base(message)
        {
        }

        public BlackDuckInspectorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
