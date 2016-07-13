using Microsoft.ApplicationInsights;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;
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
            this._telemetryClient.InstrumentationKey = "fc71152a-ec96-400a-bd98-1d5ef2a21bcf";
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

                if (!String.IsNullOrEmpty(run.Tool.Name))
                {
                    eventProperties.Add("ToolName", run.Tool.Name);
                }

                if (!String.IsNullOrEmpty(run.Tool.FileVersion))
                {
                    eventProperties.Add("ToolFileVersion", run.Tool.FileVersion);
                }

                if (!String.IsNullOrEmpty(run.Tool.FullName))
                {
                    eventProperties.Add("ToolFullName", run.Tool.FullName);
                }

                if (!String.IsNullOrEmpty(run.Tool.Language))
                {
                    eventProperties.Add("ToolLanguage", run.Tool.Language);
                }

                if (!String.IsNullOrEmpty(run.Tool.SarifLoggerVersion))
                {
                    eventProperties.Add("ToolSarifLoggerVersion", run.Tool.SarifLoggerVersion);
                }

                if (!String.IsNullOrEmpty(run.Tool.SemanticVersion))
                {
                    eventProperties.Add("ToolSemanticVersion", run.Tool.SemanticVersion);
                }

                if (!String.IsNullOrEmpty(run.Tool.Version))
                {
                    eventProperties.Add("ToolVersion", run.Tool.Version);
                }

                if (run.Results != null)
                {
                    eventProperties.Add("ResultsCount", run.Results.Count.ToString());

                    Dictionary<string, int> ruleCount = new Dictionary<string, int>();
                    foreach (Result result in run.Results)
                    {
                        IRule rule;
                        if (run.TryGetRule(result.RuleId, result.RuleKey, out rule))
                        {
                            if (!ruleCount.ContainsKey(rule.Id))
                            {
                                ruleCount[rule.Id] = 0;
                            }

                            ruleCount[rule.Id]++;
                        }
                    }

                    foreach (string ruleId in ruleCount.Keys)
                    {
                        eventProperties.Add(ruleId, ruleCount[ruleId].ToString());
                    }
                }

                this._telemetryClient.TrackEvent("OpenResultFile", eventProperties);

                this._telemetryClient.TrackMetric("ResultCount", run.Results.Count);
            }
        }

        public void OpenErrorListResult(SarifErrorListItem result)
        {
            if (this.IsEnabled)
            {
                Dictionary<string, string> eventProperties = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(result.Message))
                {
                    eventProperties.Add("Message", result.Message);
                }

                if (!String.IsNullOrEmpty(result.Rule.Id))
                {
                    eventProperties.Add("RuleId", result.Rule.Id);
                }

                if (!String.IsNullOrEmpty(result.Rule.Name))
                {
                    eventProperties.Add("RuleName", result.Rule.Name);
                }

                if (!String.IsNullOrEmpty(result.Rule.Severity))
                {
                    eventProperties.Add("Severity", result.Rule.Severity);
                }

                this._telemetryClient.TrackEvent("OpenErrorListResult", eventProperties);
            }
        }

        public void ViewLogFile()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackEvent("ViewLogFile");
            }
        }

        public void PreviewFix()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackEvent("PreviewFix");
            }
        }

        public void ApplyFix()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackEvent("ApplyFix");
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
