using Microsoft.ApplicationInsights;
using Microsoft.CodeAnalysis.Sarif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer
{
    class Telemetry : IDisposable
    {
        private static volatile Telemetry s_instance;
        private static object s_syncRoot = new Object();
        private TelemetryClient _telemetryClient;
        private bool _isDisposed = false;

        private Telemetry() : this(Environment.UserName, Guid.NewGuid().ToString())
        {
        }

        private Telemetry(string userId, string sessionId)
        {
            this._telemetryClient = new TelemetryClient();

            // TODO: Add instrumentation key.
            this._telemetryClient.InstrumentationKey = "";
            this._telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            this._telemetryClient.Context.User.Id = userId;
            this._telemetryClient.Context.Session.Id = sessionId;
        }

        public static Telemetry Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_syncRoot)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new Telemetry();
                        }
                    }
                }

                return s_instance;
            }
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public void LoadedPackage()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackPageView("Package");
            }
        }

        public void OpenedDialog()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackPageView("ToolWindow");
            }
        }

        public void SelectedTab(string tabName)
        {
            if (this.IsEnabled)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties.Add("Name", tabName);
                this._telemetryClient.TrackEvent("SelectedTab", properties);
            }
        }

        public void OpenResultFile(Run run)
        {
            if (this.IsEnabled)
            {
                Dictionary<string, string> eventProperties = new Dictionary<string, string>();
                eventProperties.Add("ToolName", run.Tool.Name);
                eventProperties.Add("ToolFileVersion", run.Tool.FileVersion);
                eventProperties.Add("ToolFullName", run.Tool.FullName);
                eventProperties.Add("ToolLanguage", run.Tool.Language);
                eventProperties.Add("ToolSarifLoggerVersion", run.Tool.SarifLoggerVersion);
                eventProperties.Add("ToolSemanticVersion", run.Tool.SemanticVersion);
                eventProperties.Add("ToolVersion", run.Tool.Version);
                eventProperties.Add("ResultsCount", run.Results.Count.ToString());
                this._telemetryClient.TrackEvent("OpenResultFile", eventProperties);

                this._telemetryClient.TrackMetric("ResultCount", run.Results.Count);
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (this.IsEnabled && this._telemetryClient != null)
                    {
                        this._telemetryClient.Flush(); 

                        // Allow time for flushing:
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
