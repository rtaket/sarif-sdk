using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;
using System.IO;

namespace HelloWorldSkim
{
    class AnalyzeContext : IAnalysisContext
    {
        Uri _targetUri;
        internal static string[] ValidExtensions = new string[] { ".txt", ".log" };

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
                return "text/plain";
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

                    string fileExtension = Path.GetExtension(value.LocalPath);

                    this.IsValidAnalysisTarget = ValidExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception e)
                {
                    this.TargetLoadException = e;
                }

                this._targetUri = value;
            }
        }

        public void Dispose()
        {
        }
    }
}
