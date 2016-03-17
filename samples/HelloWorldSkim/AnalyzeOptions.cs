using CommandLine;
using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldSkim
{
    [Verb("analyze", HelpText = "Analyze one or more text files for keywords.")]
    internal class AnalyzeOptions : AnalyzeOptionsBase
    {
    }
}
