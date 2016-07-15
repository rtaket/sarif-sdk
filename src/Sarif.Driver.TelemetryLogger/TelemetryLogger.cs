using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif.Driver
{
    public class TelemetryLogger : IAnalysisLogger, IDisposable
    {
        private bool _isDisposed = false;
        private TelemetryClient _telemetryClient;
        private HashSet<string> _analysisTargets;
        private HashSet<string> _rules;
        private int _resultCount;
        private Stopwatch _runtimeWatch;

        public TelemetryLogger()
        {
            this._analysisTargets = new HashSet<string>();
            this._rules = new HashSet<string>();
            this._runtimeWatch = new Stopwatch();

            this._telemetryClient = new TelemetryClient();
            this._telemetryClient.InstrumentationKey = "e61f617c-a65d-464b-b6a9-b8a7a17ec205";
            this._telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            this._telemetryClient.Context.User.Id = Environment.UserName;
            this._telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();

            this._telemetryClient.TrackEvent("BeginTelemetry");
        }

        public void AnalysisStarted()
        {
            this._runtimeWatch.Restart();

            this._telemetryClient.TrackEvent("AnalysisStarted");
        }

        public void AnalysisStopped(RuntimeConditions runtimeConditions)
        {
            this._runtimeWatch.Stop();

            this._telemetryClient.TrackEvent("AnalysisStopped");
            this._telemetryClient.TrackMetric("AnalysisRuntime", this._runtimeWatch.Elapsed.TotalSeconds);
        }

        public void AnalyzingTarget(IAnalysisContext context)
        {
            this._analysisTargets.Add(context.TargetUri.ToString());
        }

        public void Log(IRule rule, Result result)
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

            int codeFlowCount = result.CodeFlows == null ? 0 : result.CodeFlows.Count;
            eventProperties.Add("CodeFlowCount", codeFlowCount.ToString());

            int locationCount = result.Locations == null ? 0 : result.Locations.Count;
            eventProperties.Add("LocationCount", locationCount.ToString());

            int relatedLocationCount = result.RelatedLocations == null ? 0 : result.RelatedLocations.Count;
            eventProperties.Add("RelatedLocationCount", relatedLocationCount.ToString());

            int fixCount = result.Fixes == null ? 0 : result.Fixes.Count;
            eventProperties.Add("FixCount", fixCount.ToString());

            int propertyCount = result.PropertyNames == null ? 0 : result.PropertyNames.Count;
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

        public void LogConfigurationNotification(Notification notification)
        {
            // Do not report configuration notifications
        }

        public void LogMessage(bool verbose, string message)
        {
            // Do not report messages
        }

        public void LogToolNotification(Notification notification)
        {            
            // Do not report tool notifications
        }

        //private Dictionary<string, string> GetNotificationProperties(Notification notification)
        //{
        //    Dictionary<string, string> eventProperties = new Dictionary<string, string>();

        //    if (!String.IsNullOrEmpty(notification.Id))
        //    {
        //        eventProperties.Add("NotificationId", notification.Id);
        //    }

        //    eventProperties.Add("ExceptionCount", notification.Exception == null ? "0" : "1");

        //    int propertyCount = notification.PropertyNames == null ? 0 : notification.PropertyNames.Count;
        //    eventProperties.Add("PropertyCount", propertyCount.ToString());

        //    int tagCount = notification.Tags == null ? 0 : notification.Tags.Count;
        //    eventProperties.Add("TagCount", tagCount.ToString());

        //    eventProperties.Add("Level", notification.Level.ToString());

        //    eventProperties.Add("HasPhysicalLocation", (notification.PhysicalLocation != null).ToString());

        //    if (!String.IsNullOrEmpty(notification.RuleId))
        //    {
        //        eventProperties.Add("RuleId", notification.RuleId);
        //    }

        //    eventProperties.Add("HasRuleKey", (!String.IsNullOrWhiteSpace(notification.RuleKey)).ToString());

        //    eventProperties.Add("HasThreadId", (notification.ThreadId != 0).ToString());

        //    return eventProperties;
        //}

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (this._telemetryClient != null)
                    {
                        this._telemetryClient.TrackMetric("AnalysisTargetsCount", this._analysisTargets.Count);
                        this._telemetryClient.TrackMetric("ResultCount", this._resultCount);
                        this._telemetryClient.TrackMetric("RuleCount", this._rules.Count);
                        //this._telemetryClient.TrackMetric("FileCount", run.Files == null ? 0 : run.Files.Count);
                        this._telemetryClient.TrackEvent("EndTelemetry");

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
