using CommandLine;
using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldSkim
{
    [Verb("analyze", HelpText = "Analyze binary files for defects.")]
    internal class AnalyzeOptions : AnalyzeOptionsBase
    {
    }
}
