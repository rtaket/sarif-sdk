using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;
using System.IO;
using System.Diagnostics;

namespace HelloWorldSkim
{
    class AnalyzeContext : IAnalysisContext
    {
        Uri _targetUri;
        internal static string[] ValidExtensions = new string[] { ".dll", ".exe" };

        public bool IsValidAnalysisTarget
        {
            get;
            set;
        }

        public IAnalysisLogger Logger
        {
            get;
            set;
        }

        public string MimeType
        {
            get
            {
                return "application/octet-stream";
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public PropertyBag Policy
        {
            get;
            set;
        }

        public IRuleDescriptor Rule
        {
            get;
            set;
        }

        public RuntimeConditions RuntimeErrors
        {
            get;
            set;
        }

        public Exception TargetLoadException
        {
            get;
            set;
        }

        public FileVersionInfo FileVersionInfo
        {
            get;
            set;
        }

        public Uri TargetUri
        {
            get
            {
                return this._targetUri;
            }
            set
            {
                // When the TargetUri is set, the setter is responsible for assigning the appropriate
                // values for TargetLoadException and IsValidAnalysisTarget.
                try
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("TargetUri");
                    }

                    // Set the backing field. 
                    this._targetUri = value;

                    string localPath = value.LocalPath;
                    string fileExtension = Path.GetExtension(localPath);

                    // Verify that the file is one of the expected file types and that it is a local file.
                    if (!ValidExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase) || !File.Exists(localPath))
                    {
                        this.IsValidAnalysisTarget = false;
                    }
                    else
                    {
                        this.IsValidAnalysisTarget = true;

                        // Set the file info for the target. This will be used by the skimmers to do their evaluation.
                        this.FileVersionInfo = FileVersionInfo.GetVersionInfo(this._targetUri.LocalPath);
                    }
                }
                catch (Exception e)
                {
                    this.TargetLoadException = e;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
