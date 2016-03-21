using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldSkim.Skimmers
{
    [Export(typeof(ISkimmer<AnalyzeContext>)), Export(typeof(IRuleDescriptor))]
    class FileVersionSkimmer : ISkimmer<AnalyzeContext>, IRuleDescriptor
    {
        Dictionary<string, string> _formatSpecifiers;

        public FileVersionSkimmer()
        {
            // The first replacement string, {0}, is the name of the target file. It is supplied by the driver.
            // All other replacement strings, {1}, {2}, etc, are supplied when logging the result.
            this._formatSpecifiers = new Dictionary<string, string>();
            this._formatSpecifiers.Add("pass", "The file {0} has a valid file version of {1}.");
            this._formatSpecifiers.Add("emptyfileversion", "The file {0} contains an empty file version.");
            this._formatSpecifiers.Add("zerofileversion", "The file {0} contains an invalid file version: {1}");
        }

        public Dictionary<string, string> FormatSpecifiers
        {
            get
            {
                return this._formatSpecifiers;
            }
        }

        public string FullDescription
        {
            get
            {
                return this.ShortDescription;
            }
        }

        public Uri HelpUri
        {
            get
            {
                return new Uri($"http://helloworldskimmer/rules/{this.Id.ToLowerInvariant()}");
            }
        }

        public string Id
        {
            get
            {
                return "EXAMPLE0001";
            }
        }

        public string Name
        {
            get
            {
                return this.GetType().Name;
            }
        }

        public Dictionary<string, string> Options
        {
            get
            {
                return new Dictionary<string, string>();
            }
        }

        public Dictionary<string, string> Properties
        {
            get
            {
                return new Dictionary<string, string>();
            }
        }

        public string ShortDescription
        {
            get
            {
                return "Verifies that executables and libraries have valid file versions.";
            }
        }

        public void Analyze(AnalyzeContext context)
        {
            if (String.IsNullOrWhiteSpace(context.FileVersionInfo.FileVersion))
            {
                // Write failure/error to the SARIF log file.
                context.Logger.Log(this, RuleUtilities.BuildResult(ResultKind.Error, context, null, "emptyfileversion"));
            }
            else if (context.FileVersionInfo.FileMajorPart == 0 && context.FileVersionInfo.FileMinorPart == 0 && context.FileVersionInfo.FileBuildPart == 0 && context.FileVersionInfo.FilePrivatePart == 0)
            {
                // Write failure/error to the SARIF log file.
                context.Logger.Log(this, RuleUtilities.BuildResult(ResultKind.Error, context, null, "zerofileversion", context.FileVersionInfo.FileVersion));
            }
            else
            {
                // Write success/pass to the SARIF log file.
                context.Logger.Log(this, RuleUtilities.BuildResult(ResultKind.Pass, context, null, "pass", context.FileVersionInfo.FileVersion));
            }
        }

        public AnalysisApplicability CanAnalyze(AnalyzeContext context, out string reasonIfNotApplicable)
        {
            string localFile = context.TargetUri.LocalPath;
            string extension = Path.GetExtension(context.TargetUri.LocalPath);
            if (!AnalyzeContext.ValidExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                reasonIfNotApplicable = "Unsupported file extension.";
                return AnalysisApplicability.NotApplicableToSpecifiedTarget;
            }
            else if (!File.Exists(localFile))
            {
                reasonIfNotApplicable = "The target file does not exist on the local machine.";
                return AnalysisApplicability.NotApplicableToSpecifiedTarget;
            }
            else
            {
                reasonIfNotApplicable = String.Empty;
                return AnalysisApplicability.ApplicableToSpecifiedTarget;
            }
        }

        public void Initialize(AnalyzeContext context)
        {
        }
    }
}
