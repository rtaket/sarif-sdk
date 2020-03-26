using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Logging;

namespace Microsoft.CodeAnalysis.WorkItems.Logging
{
    public class TimingLog : IDisposable
    {
        private bool disposedValue = false;
        private readonly Stopwatch stopwatch;

        public TimingLog(EventId eventId)
        {
            this.EventId = eventId;
            this.Logger = ServiceProviderFactory.ServiceProvider.GetService<ILogger<ILogger>>();
            this.CustomDimensions = new Dictionary<string, object>();

            this.Logger.LogDebug(this.EventId, $"Begin: Event {this.EventId.Id} ({this.EventId.Name})");
            stopwatch = Stopwatch.StartNew();
        }

        public ILogger Logger { get; }
        public EventId EventId { get; }
        public Dictionary<string, object> CustomDimensions { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.CustomDimensions.Add("ElapsedTime", stopwatch.ElapsedMilliseconds);
                    this.Logger.LogMetrics(this.EventId, this.CustomDimensions);
                    this.Logger.LogDebug(this.EventId, $"End: Event {this.EventId.Id} ({this.EventId.Name})");
                    stopwatch?.Stop();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
