using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldSkim
{
    class Program
    {
        static int Main(string[] args)
        {
            ParserResult<AnalyzeOptions> result = Parser.Default.ParseArguments<AnalyzeOptions>(args);

            return result.MapResult((AnalyzeOptions analyzeOptions) => new AnalyzeCommand().Run(analyzeOptions), errs => 1);
        }
    }
}
