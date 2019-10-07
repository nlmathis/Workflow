using GraphWorkflow.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Tests.Net
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string logMessage)
        {
            Console.WriteLine(logMessage);
        }
    }
}
