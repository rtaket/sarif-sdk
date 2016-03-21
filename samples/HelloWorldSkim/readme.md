# HelloWorldSkim Sample
The HelloWorldSkim sample verifies if exe/dll files define a valid file version. It demonstrates how to consume the SARIF driver SDK to
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
4. Skimmers\FileVersionSkimmer.cs

    A skimmer (rule) which verifies that exe/dlls have valid file versions. In the example skimmer, a valid file version is defined as a non-empty and non-zero file version. The skimmer makes itself available for analysis
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
C:\Input\HelloWorldSkim.exe: error EXAMPLE0001: The file HelloWorldSkim.exe contains an invalid file version: 0.0.0.0

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
        "invocationInfo": "HelloWorldSkim.exe  -o C:\\Temp\\helloworldskim.sarif.json C:\Input",
        "analysisTargets": [
          {
            "uri": "file:///C:/Input/HelloWorldSkim.exe"
          },
          {
            "uri": "file:///C:/Input/Newtonsoft.Json.dll"
          }
        ]
      },
      "results": [
        {
          "ruleId": "EXAMPLE0001",
          "kind": "error",
          "formattedMessage": {
            "specifierId": "zerofileversion",
            "arguments": [
              "HelloWorldSkim.exe",
              "0.0.0.0"
            ]
          },
          "locations": [
            {
              "analysisTarget": [
                {
                  "uri": "file:///C:/Input/HelloWorldSkim.exe",
                  "mimeType": "application/octet-stream"
                }
              ]
            }
          ]
        }
      ],
      "ruleInfo": [
        {
          "id": "EXAMPLE0001",
          "name": "FileVersionSkimmer",
          "shortDescription": "Verifies that executables and libraries have valid file versions.",
          "fullDescription": "Verifies that executables and libraries have valid file versions.",
          "options": {},
          "formatSpecifiers": {
            "pass": "The file {0} has a valid file version of {1}.",
            "emptyfileversion": "The file {0} contains an empty file version.",
            "zerofileversion": "The file {0} contains an invalid file version: {1}"
          },
          "properties": {}
        }
      ]
    }
  ]
}
```
