using System;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Accessh.Daemon
{
    /// <summary>
    /// Worker service
    /// </summary>
    public class HostService : IHostedService, IDisposable
    {
        private static readonly AutoResetEvent CloseRequested = new(false);
        private Task _work;
        private readonly IDaemonService _daemonService;
        
        public HostService(IDaemonService daemonService)
        {
            _daemonService = daemonService;
        }
        
        /// <summary>
        /// Run worker
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _work = Task.Run(_daemonService.Worker, cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop worker
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            CloseRequested.Set();

            if (_work == null) return Task.CompletedTask;
            
            _work = null;

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Dispose daemon
        /// </summary>
        /// Automatically executed after <see cref="StopAsync"/>
        public void Dispose()
        {
            Log.Information("Host: The daemon will close now");
            _daemonService.Dispose();
        }
    }
}
