using GraphWorkflow.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Tests.Core
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string logMessage)
        {
            Console.WriteLine(logMessage);
        }
    }
}
