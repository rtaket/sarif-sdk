# HelloWorldSkim Sample
The HelloWorldSkim sample verifies if text file contain the word 'helloworld'. It demonstrates how to consume the SARIF driver SDK to
create an analysis tool which outputs results in the SARIF file format.

## Files
1. AnalyzeOptions.cs

    Defines any additional command line options.  
2. AnalyzeCommand.cs

    AnalyzeComamnd drives the analysis. Most of the work is done by its base class, `AnalyzeCommandBase`. The only customization in AnalyzeCommand is to define which assemblies contain the skimmers (rules).  
3. AnalyzeContext.cs

    AnalyzeContext provides information about the target file and any configuration to be used during the analysis.  
    Note: The driver SDK sets the TargetUri property to the file under analysis. It is the TargetUri's setter's responsiblity to set
    the TargetLoadException and IsValidAnalysisTarget properties appropriately after loading the target file.
4. Skimmers\HelloWorldSkimmer.cs

    A skimmer (rule) which verifies if the word 'helloworld' exists in the target file. The skimmer makes itself available for analysis
by exporting the ISkimmer interface.
`[Export(typeof(ISkimmer<AnalyzeContext>))]`   
    The Analyze() command is called for each target being analyzed. It is responsible for performing the verification and writing results to the loggers.  
    If no output file is specified on the command line, the log is only written to the console. If an output file is specified on the command line, the log is also written to the output file in the SARIF format.  

## How to Run
````
HelloWorldSkim.exe analyze -output C:\Temp\helloworldskim.sarif.json C:\Input 
```

### Example Console Output
````
Analyzing...
C:\Input\bad.txt(1,1,1,11): error HW0001: The word 'helloworld' was found in the text file bad.txt on line C:\Input\bad.txt, column 0

Analysis completed successfully.
```

### Example SARIF File
````
{
  "version": "1.0.0-beta.1",
  "runLogs": [
    {
      "toolInfo": {
        "name": "HelloWorldSkim",
        "fullName": "HelloWorldSkim 1.0.0",
        "version": "1.0.0"
      },
      "runInfo": {
        "invocationInfo": "HelloWorldSkim.exe  -o C:\\Temp\\helloworldskim.sarif.json C:\\Input",
        "analysisTargets": [
          {
            "uri": "file:///C:/Input/bad.txt"
          },
          {
            "uri": "file:///C:/Input/good.txt"
          }
        ]
      },
      "results": [
        {
          "ruleId": "HW0001",
          "kind": "error",
          "formattedMessage": {
            "specifierId": "fail",
            "arguments": [
              "bad.txt",
              "C:\\Input\\bad.txt",
              "0",
              "0"
            ]
          },
          "locations": [
            {
              "analysisTarget": [
                {
                  "uri": "file:///C:/Input/bad.txt",
                  "mimeType": "text/plain",
                  "region": {
                    "startLine": 1,
                    "startColumn": 1,
                    "endLine": 1,
                    "endColumn": 11
                  }
                }
              ]
            }
          ]
        }
      ],
      "ruleInfo": [
        {
          "id": "HW0001",
          "name": "HelloWorldSkimmer",
          "shortDescription": "Verifies a file does not contain the word 'helloworld'.",
          "fullDescription": "Verifies a file does not contain the word 'helloworld'.",
          "options": {},
          "formatSpecifiers": {
            "pass": "The word 'helloworld' was not found in the text file {0}",
            "fail": "The word 'helloworld' was found in the text file {0} on line {1}, column {2}"
          },
          "properties": {}
        }
      ]
    }
  ]
}
```
