using System;

namespace RtuBroker.ZeroMq.Transport
{
    class LambdaDisposable : IDisposable
    {
        private readonly Action _dispose;

        public LambdaDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose();
        }
    }
}