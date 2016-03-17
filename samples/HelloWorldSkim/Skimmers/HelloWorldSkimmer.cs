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
    class HelloWorldSkimmer : ISkimmer<AnalyzeContext>, IRuleDescriptor
    {
        Dictionary<string, string> _formatSpecifiers;

        public HelloWorldSkimmer()
        {
            this._formatSpecifiers = new Dictionary<string, string>();
            this._formatSpecifiers.Add("pass", "The word 'helloworld' was not found in the text file {0}");
            this._formatSpecifiers.Add("fail", "The word 'helloworld' was found in the text file {0} on line {1}, column {2}");
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
                return "HW0001";
            }
        }

        public string Name
        {
            get
            {
                return "HelloWorldSkimmer";
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
                return "Verifies a file does not contain the word 'helloworld'.";
            }
        }

        public void Analyze(AnalyzeContext context)
        {
            string localFile = context.TargetUri.LocalPath;
            string[] lines = File.ReadAllLines(localFile);
            bool foundResult = false;

            for (int i = 0; i < lines.Length; i++)
            {
                int index = lines[i].IndexOf("helloworld", StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    // Write failure/error to the SARIF log file.
                    context.Logger.Log(this, RuleUtilities.BuildResult(ResultKind.Error, context, new Region(i + 1, index + 1, i + 1, index + 11, 0, 0, 0), "fail", localFile, i.ToString(), index.ToString()));
                    foundResult = true;
                }
            }

            if (!foundResult)
            {
                // Write success/pass to the SARIF log file.
                context.Logger.Log(this, RuleUtilities.BuildResult(ResultKind.Pass, context, null, "pass", localFile));
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
