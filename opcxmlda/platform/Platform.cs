using System;
using System.Diagnostics;
using NLog;

namespace l99.driver.opcxmlda
{
    public partial class Platform
    {
        private ILogger _logger;
        
        private OpcxmldaMachine _machine;
        
        private struct NativeDispatchReturn
        {
            public bool RC;
            public long ElapsedMilliseconds;
        }
        
        private Func<Func<bool>, NativeDispatchReturn> nativeDispatch = (nativeCallWrapper) =>
        {
            bool rc = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            rc = nativeCallWrapper();
            sw.Stop();
            return new NativeDispatchReturn
            {
                RC = rc,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            };
        };
        
        public Platform(OpcxmldaMachine machine)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _machine = machine;
        }
        
        
    }
}