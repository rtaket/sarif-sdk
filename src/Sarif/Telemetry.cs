using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif
{
    class Telemetry : IDisposable
    {
        private static volatile Telemetry s_instance;
        private static object s_syncRoot = new Object();
        private TelemetryClient _telemetryClient;
        private bool _isDisposed = false;
        private HashSet<string> _rules;
        private int _resultCount;

        private Telemetry() : this(Environment.UserName, Guid.NewGuid().ToString())
        {
        }

        private Telemetry(string userId, string sessionId)
        {
            this._rules = new HashSet<string>();

            this._telemetryClient = new TelemetryClient();

            this.Initialize(userId, sessionId);
            this.BeginTelemetry();
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

        private void Initialize(string userId, string sessionId)
        {
            // TODO: Add instrumentation key.
            this._telemetryClient.InstrumentationKey = "e61f617c-a65d-464b-b6a9-b8a7a17ec205";
            this._telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            this._telemetryClient.Context.User.Id = userId;
            this._telemetryClient.Context.Session.Id = sessionId;
        }

        public void LoadedLogger()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackPageView("SarifLogger");
            }
        }

        public void AnalysisStarted()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackEvent("AnalysisStarted");
            }
        }

        public void AnalysisStopped()
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackEvent("AnalysisStopped");
            }
        }

        public void AnalysisDurationMetric(double duration)
        {
            if (this.IsEnabled)
            {
                this._telemetryClient.TrackMetric("AnalysisDuration", duration);
            }
        }

        public void LogTool(Tool tool)
        {
            if (this.IsEnabled)
            {
                Dictionary<string, string> eventProperties = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(tool.FileVersion))
                {
                    eventProperties.Add("FileVersion", tool.FileVersion);
                }

                if (!String.IsNullOrEmpty(tool.FullName))
                {
                    eventProperties.Add("FullName", tool.FullName);
                }

                if (!String.IsNullOrEmpty(tool.Language))
                {
                    eventProperties.Add("Language", tool.Language);
                }

                if (!String.IsNullOrEmpty(tool.Name))
                {
                    eventProperties.Add("Name", tool.Name);
                }

                bool hasProperties = tool.Properties != null && tool.Properties.Count > 0;
                eventProperties.Add("HasProperties", hasProperties.ToString());

                if (!String.IsNullOrEmpty(tool.SarifLoggerVersion))
                {
                    eventProperties.Add("SarifLoggerVersion", tool.SarifLoggerVersion);
                }

                if (!String.IsNullOrEmpty(tool.SemanticVersion))
                {
                    eventProperties.Add("SemanticVersion", tool.SemanticVersion);
                }

                if (!String.IsNullOrEmpty(tool.Version))
                {
                    eventProperties.Add("Version", tool.Version);
                }

                this._telemetryClient.TrackEvent("LogTool", eventProperties);
            }
        }

        public void LogResult(IRule rule, Result result)
        {
            if (this.IsEnabled)
            {
                Dictionary<string, string> eventProperties = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(rule.Id))
                {
                    eventProperties.Add("RuleId", rule.Id);
                }

                if (!String.IsNullOrEmpty(rule.Name))
                {
                    eventProperties.Add("RuleName", rule.Name);
                    eventProperties.Add("Name", rule.Name);
                }

                int codeFlowCount = result.CodeFlows == null ? 0 :result.CodeFlows.Count;
                eventProperties.Add("CodeFlowCount", codeFlowCount.ToString());

                int locationCount = result.Locations == null ? 0 : result.Locations.Count;
                eventProperties.Add("LocationCount", locationCount.ToString());

                int relatedLocationCount = result.RelatedLocations == null ? 0 : result.RelatedLocations.Count;
                eventProperties.Add("RelatedLocationCount", relatedLocationCount.ToString());

                int fixCount = result.Fixes == null ? 0 : result.Fixes.Count;
                eventProperties.Add("FixCount", fixCount.ToString());

                int propertyCount = result.Properties == null ? 0 : result.Properties.Count;
                eventProperties.Add("PropertyCount", propertyCount.ToString());

                int tagCount = result.Tags == null ? 0 : result.Tags.Count;
                eventProperties.Add("TagCount", tagCount.ToString());

                int stackCount = result.Stacks == null ? 0 : result.Stacks.Count;
                eventProperties.Add("StackCount", stackCount.ToString());

               eventProperties.Add("BaselineState", result.BaselineState.ToString());

               eventProperties.Add("HasFormattedRuleMessage", (result.FormattedRuleMessage != null).ToString());

               eventProperties.Add("ResultLevel", result.Level.ToString());

                if (!String.IsNullOrEmpty(result.RuleId))
                {
                    eventProperties.Add("ResultRuleId", result.RuleId);
                }

                eventProperties.Add("HasRuleKey", (!String.IsNullOrEmpty(result.RuleKey)).ToString());
                eventProperties.Add("HasSnippet", (!String.IsNullOrEmpty(result.Snippet)).ToString());
                eventProperties.Add("HasToolFingerprintContribution", (!String.IsNullOrEmpty(result.ToolFingerprintContribution)).ToString());

                eventProperties.Add("SuppressionStates", result.SuppressionStates.ToString());

                this._telemetryClient.TrackEvent("LogResult", eventProperties);

                this._resultCount++;
                this._rules.Add(rule.Id);
            }
        }

        private Dictionary<string, string> GetNotificationProperties(Notification notification)
        {
            Dictionary<string, string> eventProperties = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(notification.Id))
            {
                eventProperties.Add("NotificationId", notification.Id);
            }

            eventProperties.Add("ExceptionCount", notification.Exception == null ? "0" : "1");

            int propertyCount = notification.Properties == null ? 0 : notification.Properties.Count;
            eventProperties.Add("PropertyCount", propertyCount.ToString());

            int tagCount = notification.Tags == null ? 0 : notification.Tags.Count;
            eventProperties.Add("TagCount", tagCount.ToString());

            eventProperties.Add("Level", notification.Level.ToString());

            eventProperties.Add("HasPhysicalLocation", (notification.PhysicalLocation != null).ToString());

            if (!String.IsNullOrEmpty(notification.RuleId))
            {
                eventProperties.Add("RuleId", notification.RuleId);
            }

            eventProperties.Add("HasRuleKey", (!String.IsNullOrWhiteSpace(notification.RuleKey)).ToString());

            eventProperties.Add("HasThreadId", (notification.ThreadId != 0).ToString());

            return eventProperties;
        }

        public void LogRunMetrics(Run run)
        {
            if (this.IsEnabled)
            {
                Dictionary<string, string> eventProperties = new Dictionary<string, string>();

                this._telemetryClient.TrackMetric("FileCount", run.Files == null ? 0 : run.Files.Count);

                this._telemetryClient.TrackMetric("ResultCount", this._resultCount);
                this._telemetryClient.TrackMetric("RuleCount", this._rules.Count);
            }
        }

        private void BeginTelemetry()
        {
            this._telemetryClient.TrackEvent("BeginTelemetry");
        }

        private void EndTelemetry()
        {
            this._telemetryClient.TrackEvent("EndTelemetry");
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

                        this.EndTelemetry();

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
