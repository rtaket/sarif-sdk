using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldSkim
{
    internal class AnalyzeCommand : AnalyzeCommandBase<AnalyzeContext, AnalyzeOptions>
    {
        public override string Prerelease
        {
            get
            {
                // The prerelease string is used to generate the tool version number in the SARIF log file.
                // Since this is not a prerelease version of the tool, return an empty string.
                return String.Empty;
            }
        }

        public override IEnumerable<Assembly> DefaultPlugInAssemblies
        {
            get
            {
                return new Assembly[] { this.GetType().Assembly };
            }
            set { throw new InvalidOperationException(); }
        }
    }
}
