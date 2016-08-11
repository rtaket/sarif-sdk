// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *                                                       *
// ********************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Microsoft.CodeAnalysis.Sarif.Converters
{
    internal class FindBugsConverter : ToolFileConverterBase
    {
        /// <summary> Convert FindBugs generated reports into static analysis results interchange format </summary>
        /// <param name="input"> input stream containing the FindBugs generated report </param>
        /// <param name="output"> where to write out converted issues </param>
        public override void Convert(Stream input, IResultLogWriter output)
        {
            ConvertReport(input, output);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
            Justification = "They are element or attribute names defined by the schema")]
        private static void ConvertReport(Stream input, IResultLogWriter output)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.DtdProcessing = DtdProcessing.Ignore;
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreProcessingInstructions = true;
            readerSettings.IgnoreWhitespace = true;
            readerSettings.NameTable = FindBugsReportXsdConstants.NameSet;
            readerSettings.XmlResolver = null;

            using (var xmlReader = XmlReader.Create(input, readerSettings))
            {
                try
                {
                    xmlReader.MoveToContent();

                    // The root element of a FindBugs report should be a "BugCollection"
                    if ((xmlReader.NodeType != XmlNodeType.Element) ||
                        !object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.BugCollection))
                    {
                        throw xmlReader.CreateException("expecting a 'BugCollection' root element");
                    }

                    if (xmlReader.MoveToFirstAttribute())
                    {
                        Tool toolInfo = new Tool();
                        toolInfo.Name = "FindBugs";
                        ConvertBugCollectionAttributes(xmlReader, toolInfo);
                        xmlReader.MoveToElement();
                        output.WriteTool(toolInfo);
                        toolInfo = null;    // we're done with it
                    }
                    else
                    {
                        throw xmlReader.CreateException("missing some required 'BugCollection' attributes");
                    }

                    bool readyToExamine = false;

                    // A "BugCollection" composes of a "Project" element, followed by {"BugInstance"}*, followed
                    // {"BugCategory"}*, followed by {"BugPattern"}*, followed by {"BugCode"}*, followed by an
                    // "Errors" element which lists all missing classes (possibly 0), followed by a
                    // "FindBugsSummary" element, followed by an optional "SummaryHTML" element, followed by
                    // a "ClassFeatures" element and ended with a "History" element.
                    if (xmlReader.Read() &&
                        object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Project))
                    {
                        Debug.Assert(xmlReader.Depth == 1);
                        Debug.Assert(xmlReader.NodeType == XmlNodeType.Element);

                        ConvertProjectElements(xmlReader);

                        readyToExamine = ((xmlReader.NodeType != XmlNodeType.EndElement) ||
                            !object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Project));
                    }
                    else
                    {
                        throw xmlReader.CreateException("something unexpected while processing the required 'Project' element");
                    }

                    bool warningGiven = false;  // whether a warning message has been displayed
                    var buffer = new StringBuilder();
                    var sourceLocations = new List<Location>();
                    NameGenerator locRoleNames = new NameGenerator("LOC_{0}_role");

                    // The report contains a BugCollection composing of BugInstance and other elements.
                    // This loop dispatches handling of the top two-level (at depth 0 and 1) elements, and
                    // all nested elements / attributes are handled by respective level-1 elements.
                    while ((readyToExamine || xmlReader.Read()) &&
                        object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.BugInstance))
                    {
                        Debug.Assert(xmlReader.Depth == 1);
                        Debug.Assert(xmlReader.NodeType == XmlNodeType.Element);

                        Result issue = new Result();
                        issue.Locations = sourceLocations;
                        sourceLocations.Clear();
                        ConvertBugInstanceElement(xmlReader, issue, sourceLocations, buffer, locRoleNames);
                        readyToExamine = false;

                        if (issue.Message == null || issue.ToolFingerprintContribution == null)
                        {
                            // Print out a warning only once.
                            // Q: should we stop converting?
                            if (!warningGiven)
                            {
                                warningGiven = true;
                                System.Console.WriteLine("Issue 'instanceHash'es and/or messages missing in reports.");
                                System.Console.WriteLine("Please use the command line with the '-xml:withMessages' to regenerate reports");
                            }

                            if (issue.Message == null)
                            {
                                Debug.Assert(!String.IsNullOrWhiteSpace(issue.RuleId));
                                issue.Message = DefaultFullMessage;
                            }
                        }

                        output.WriteResult(issue);
                        issue = null;
                    }

                    Debug.Assert(xmlReader.IsStartElement() && (xmlReader.Depth == 1));

                    // TODO: handle optional "BugCategory" elements; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.BugCategory, IgnoreOptions.Optional | IgnoreOptions.Multiple);

                    // TODO: handle optional "BugPattern" elements; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.BugPattern, IgnoreOptions.Optional | IgnoreOptions.Multiple);

                    // TODO: handle optional "BugCode" elements; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.BugCode, IgnoreOptions.Optional | IgnoreOptions.Multiple);

                    // TODO: handle required "Errors" element; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.Errors, IgnoreOptions.Required);

                    // TODO: handle required "FindBugsSummary" element; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.Summary, IgnoreOptions.Required);

                    // TODO: handle optional "SummaryHTML" element; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.SummaryHtml, IgnoreOptions.Optional);

                    // TODO: handle required "ClassFeatures" element; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.ClassFeatures, IgnoreOptions.Required);

                    // TODO: handle required "History" element; currently we have no way to save the info in UIS
                    xmlReader.IgnoreElement(FindBugsReportXsdConstants.History, IgnoreOptions.Required);

                    if ((xmlReader.NodeType == XmlNodeType.EndElement) &&
                        object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.BugCollection))
                    {
                        // We could also verify nothing left in the report, but we've gotten all we want.
                        return;
                    }

                    throw xmlReader.CreateException("expecting BugCollection end element");
                }
                finally
                {
                    pathMap.Clear();
                }
            }
        }

        // NOTE: the following code does not use some data structures in a thread-safe way

        // Source paths FindBugs used to describe an issue are always relative ones, although the project
        // info may contain a set of source directories (SrcDir), Java classpaths or build directories (Jar).
        private const string LocationRootIdPrefix = "$(PROJ_ROOT)";

        // FindBugs does have an instance hash associated with each issue but instance hashes may not be
        // included in reports (The AndroidStudio plug-in appears to include this but reports produced by
        // FindBugs may not -- you have to use the command line with the "-xml:withMessages" option to
        // have instance hashes and issues messages in generated reports, but there appears to be no known
        // GUI configuration option for the same purpose).  In general, we should persistent info provided
        // by a tool if we determine that they are valuable, but if we cannot get the info directly from a
        // tool, each format converter should not try to derive it from other info provided by the tool.
        // Derived info should be computed in a consistent way that can be evolved together with other info
        // (and idealy by the universal issue store management tool when importing issues).

        // FindBugs generated reports may not contain short or long messages unless you use the command
        // line with the "-xml:withMessages" option to do the analysis and produce reports.
        private const string DefaultFullMessage = "Please use the RuleId string as the key searching on "
            + " http://findbugs.sourceforge.net/bugDescriptions.html for more info about this issue, and"
            + " check properties and keywords attached to instances for instance-specific data.";

        // a map from paths read from reports to what we want to save in UIS.  A tradeoff of computing for
        // string allocations, given that it is likely the same source file may contain multiple issues.
        private static readonly Dictionary<string, string> pathMap = new Dictionary<string, string>();

        // Source paths in FindBugs reports uses "/" as the path separator (like "com/yammer/api/utils/someFile.java")
        private static string GetSarifPathFromFindBugsSourcePath(string reportPath)
        {
            Debug.Assert(reportPath != null);

            var pathMap = FindBugsConverter.pathMap;
            string uisPath = null;
            if (!pathMap.TryGetValue(reportPath, out uisPath))
            {
                uisPath = (reportPath.IndexOf('/') < 0) ? reportPath : reportPath.Replace('/', '\\');
                pathMap.Add(reportPath, uisPath);
            }
            return uisPath;
        }

        private static readonly string[] FindBugsPriorities = new string[] {
            "High",     // FindBugs' HIGH_PRIORITY
            "Normal",   // FindBugs' NORMAL_PRIORITY
            "Low",      // FindBugs' LOW_PRIORITY
            "Experimental", // FindBugs' EXP_PRIORITY
            "Ignore"    // FindBugs' IGNORE_PRIORITY
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
            Justification = "They are element or attribute names defined by the schema")]
        private static void ConvertBugInstanceAttributes(XmlReader xmlReader, Result issue)
        {
            Debug.Assert((xmlReader != null) && (issue != null));
            Debug.Assert(xmlReader.IsStartElement());

            // We only check whether we get all required attributes or not; duplicate and unwanted attributes are discarded
            bool seenType = false;
            bool seenPriority = false;
            bool seenAbbrev = false;
            bool seenCategory = false;
            while (xmlReader.MoveToNextAttribute())
            {
                string attrName = xmlReader.LocalName;
                Debug.Assert(!string.IsNullOrEmpty(attrName));
                switch (attrName[0])
                {
                    case 't':
                        if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.TypeAttr))
                        {
                            // Bug Type (e.g. EI_EXPOSE_REP, IS2_INCONSISTENT_SYNC, RCN_REDUNDANT_NULLCHECK_OF_NONNULL_VALUE)
                            if (!seenType)
                            {
                                issue.RuleId = xmlReader.Value;
                                seenType = true;
                            }
                            break;
                        }
                        goto default;

                    case 'p':
                        if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.PriorityAttr))
                        {
                            /* Bug Priority; it's an unsigned byte
                                1 = HIGH_PRIORITY
                                2 = NORMAL_PRIORITY
                                3 = LOW_PRIORITY
                                4 = EXP_PRIORITY    // experimental
                                5 = IGNORE_PRIORITY
                            */
                            if (!seenPriority)
                            {
                                int priority = xmlReader.ReadContentAsInt();
                                if ((priority <= 0) || (priority >= FindBugsPriorities.Length))
                                {
                                    priority = FindBugsPriorities.Length;
                                }
                                issue.SetProperty(attrName, FindBugsPriorities[priority - 1]);
                                seenPriority = true;
                            }
                            break;
                        }
                        goto default;

                    case 'a':
                        if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.AbbrevAttr))
                        {
                            // ignore
                            if (!seenAbbrev)
                            {
                                seenAbbrev = true;
                            }
                            break;
                        }
                        goto default;

                    case 'c':
                        if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.CategoryAttr))
                        {
                            // Bug Category (e.g. MALICIOUS_CODE, MT_CORRECTNESS, STYLE)
                            if (!seenCategory)
                            {
                                issue.SetProperty(attrName, xmlReader.Value);
                                seenCategory = true;
                            }
                            break;
                        }
                        else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.CweidAttr))
                        {
                            // Bug CWE#
                            issue.SetProperty(attrName, xmlReader.Value);
                            break;
                        }
                        goto default;

                    case 'r':
                        if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.RankAttr))
                        {
                            // Bug Rank
                            issue.SetProperty(attrName, xmlReader.Value);
                            break;
                        }
                        goto default;

                    case 'i':
                        if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.InstanceHashAttr))
                        {
                            // Bug Instance Hash
                            issue.ToolFingerprintContribution = xmlReader.Value;
                            break;
                        }
                        goto default;

                    default:
                        // ignore other attributes (instanceOccurenceNum, instanceOccurrenceMax)
                        //throw xmlReader.CreateException("Unrecognized BugInstance attributes");
                        break;
                }
            }

            // BugInstance has 4 required attributes
            if (!(seenCategory & seenAbbrev & seenPriority & seenType))
            {
                throw xmlReader.CreateException("missing some required BugInstance attributes");
            }
        }

        private struct BugConversionContext
        {
            internal FindBugsSourceLineInfo SourceLineInfo;
            internal FindBugsMethodInfo MethodInfo;
            internal FindBugsFieldInfo FieldInfo;
            internal Dictionary<string, string> Properties { get { return properties; } }
            internal IList<Location> Locations { get { return locations; } }
            internal StringBuilder Buffer { get { return buffer; } }
            internal NameGenerator NameCache { get { return nameCache; } }

            private Dictionary<string, string> properties;
            private IList<Location> locations;
            private StringBuilder buffer;
            private NameGenerator nameCache;

            internal BugConversionContext(Dictionary<string, string> properties, IList<Location> locations, StringBuilder buffer, NameGenerator nameCache)
            {
                SourceLineInfo = new FindBugsSourceLineInfo();
                MethodInfo = new FindBugsMethodInfo();
                FieldInfo = new FindBugsFieldInfo();
                this.properties = properties;
                this.locations = locations;
                this.buffer = buffer;
                this.nameCache = nameCache;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "Straightline code matches the schema definition")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
            Justification = "They are element or attribute names defined by the schema")]
        private static void ConvertBugInstanceElement(
            XmlReader xmlReader,
            Result issue,
            IList<Location> locations,
            StringBuilder buffer,
            NameGenerator locRoleNames)
        {
            Debug.Assert((xmlReader != null) && (issue != null) && (locations != null) && (buffer != null));
            Debug.Assert(xmlReader.Depth == 1);

            // Check and convert attributes on a bug instance
            Debug.Assert(xmlReader.HasAttributes);
            ConvertBugInstanceAttributes(xmlReader, issue);
            xmlReader.MoveToElement();

            uint countGoodSourceLines = 0;
            uint countClasses = 0;
            uint countMethods = 0;
            uint countFields = 0;
            uint countTypes = 0;
            uint countVars = 0;
            uint countInts = 0;
            uint countStrings = 0;

            var context = new BugConversionContext(/* BUGBUG */ new Dictionary<string, string>(), locations, buffer, locRoleNames);

            bool readyToExamine = xmlReader.Read();
            int bugInstanceChildElementDepth = xmlReader.Depth;
            if (!readyToExamine || (bugInstanceChildElementDepth < 2) || (xmlReader.NodeType != XmlNodeType.Element))
            {
                string info = "expecting some required sub-elements for '" + FindBugsReportXsdConstants.BugInstance + '\'';
                throw xmlReader.CreateException(info);
            }

            do
            {
                Debug.Assert(xmlReader.IsStartElement());
                string elementName = xmlReader.LocalName;
                Debug.Assert(!string.IsNullOrWhiteSpace(elementName));
                switch (elementName[0])
                {
                    case 'S':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.SourceLine))
                        {
                            countGoodSourceLines = ConvertFindBugsSourceLineElement(xmlReader, ref context, countGoodSourceLines);
                            break;
                        }
                        else if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.String))
                        {
                            countStrings = ConvertFindBugsIntOrStringElement(xmlReader, context.Properties, countStrings, "STRING");
                            break;
                        }
                        else if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.ShortMessage))
                        {
                            issue.Message = xmlReader.ReadElementContentAsString();
                            readyToExamine = true;
                            continue;
                        }
                        goto default;

                    case 'L':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.LocalVariable))
                        {
                            countVars = ConvertFindBugsLocalVariableElement(xmlReader, context.Properties, countVars);
                            break;
                        }
                        else if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.LongMessage))
                        {
                            issue.Message = xmlReader.ReadElementContentAsString();
                            readyToExamine = true;
                            continue;
                        }
                        goto default;

                    case 'C':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Class))
                        {
                            countClasses = ConvertFindBugsClassElement(xmlReader, ref context, countClasses);
                            break;
                        }
                        goto default;

                    case 'M':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Method))
                        {
                            countMethods = ConvertFindBugsMethodElement(xmlReader, ref context, countMethods);
                            break;
                        }
                        goto default;

                    case 'T':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Type))
                        {
                            countTypes = ConvertFindBugsTypeElement(xmlReader, context.Properties, buffer, countTypes);
                            break;
                        }
                        goto default;

                    case 'F':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Field))
                        {
                            countFields = ConvertFindBugsFieldElement(xmlReader, ref context, countFields);
                            break;
                        }
                        goto default;

                    case 'I':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Int))
                        {
                            countInts = ConvertFindBugsIntOrStringElement(xmlReader, context.Properties, countInts, "INT");
                            break;
                        }
                        goto default;

                    case 'P':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Property))
                        {
                            string value;
                            string name = ReadFindBugsPropertyInfo(xmlReader, out value);
                            if (!TryAdd(context.Properties, name, value))
                            {
                                // ignore duplicate properties; we could deem the report invalid if we want
                            }
                            break;
                        }
                        goto default;

                    case 'U':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.UserAnnotation))
                        {
                            // ignore for now
                            break;
                        }
                        goto default;

                    default:
                        string info = "unrecognized element '" + elementName + '\'';
                        throw xmlReader.CreateException(info);
                }

                readyToExamine = SkipRestElementChildren(xmlReader, elementName, bugInstanceChildElementDepth);
            }
            while ((readyToExamine || xmlReader.Read()) && (xmlReader.NodeType == XmlNodeType.Element));

            Debug.Assert((xmlReader.NodeType == XmlNodeType.EndElement) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.BugInstance));

            if (countGoodSourceLines == 0)
            {
                Location loc = null;

                // The instance does not contain a nested "SourceLine" element.  Use one embedded in
                // either the "Field" element or the "Method" element.  sourceInfo is filled when we
                // read a "Class" element.  We choose the most nested scope first as it would provide
                // more refined scope info.
                if (context.FieldInfo.IsAvailable() && context.FieldInfo.SourceLineInfo.IsInteresting())
                {
                    loc = ConvertFindBugsSourceInfo(ref context.FieldInfo.SourceLineInfo);
                }
                else if (context.MethodInfo.IsAvailable() && context.MethodInfo.SourceLineInfo.IsInteresting())
                {
                    loc = ConvertFindBugsSourceInfo(ref context.MethodInfo.SourceLineInfo);
                }
                else if (context.SourceLineInfo.IsAvailable())
                {
                    loc = ConvertFindBugsSourceInfo(ref context.SourceLineInfo);
                }

                Debug.Assert(loc != null);
                locations.Add(loc);
            }
        }

        private static uint ConvertFindBugsSourceLineElement(
            XmlReader xmlReader,
            ref BugConversionContext context,
            uint countGoodSourceLines)
        {
            if (countGoodSourceLines == 0)
            {
                ReadFindBugsSourceLineInfo(xmlReader, ref context.SourceLineInfo);
                context.Locations.Add(ConvertFindBugsSourceInfo(ref context.SourceLineInfo));
                return ++countGoodSourceLines;
            }

            var tmpSourceInfo = new FindBugsSourceLineInfo();
            ReadFindBugsSourceLineInfo(xmlReader, ref tmpSourceInfo);

            if (countGoodSourceLines == 1)
            {
                // reports exported by FindBugs' plug-in for AndroidStudio may contain duplicate source lines
                if (context.SourceLineInfo.Equals(ref tmpSourceInfo))
                {
                    return countGoodSourceLines;
                }

                context.Locations.Add(ConvertFindBugsSourceInfo(ref context.SourceLineInfo));
            }

            context.Locations.Add(ConvertFindBugsSourceInfo(ref tmpSourceInfo));

            if (!string.IsNullOrWhiteSpace(tmpSourceInfo.Role))
            {
                context.Properties.Add(context.NameCache.GetName(countGoodSourceLines), tmpSourceInfo.Role);
            }
            return ++countGoodSourceLines;
        }

        private static uint ConvertFindBugsClassElement(XmlReader xmlReader, ref BugConversionContext context, uint count)
        {
            string classRole;
            string className;
            if (count > 0)
            {
                FindBugsSourceLineInfo tmpSourceInfo = new FindBugsSourceLineInfo();
                classRole = ReadFindBugsClassInfo(xmlReader, ref tmpSourceInfo);
                className = tmpSourceInfo.ClassName;
            }
            else
            {
                classRole = ReadFindBugsClassInfo(xmlReader, ref context.SourceLineInfo);
                className = context.SourceLineInfo.ClassName;
            }
            if (classRole != null)
            {
                AddWithCount(context.Properties, classRole, className, count);
            }
            return ++count;
        }

        private static uint ConvertFindBugsMethodElement(XmlReader xmlReader, ref BugConversionContext context, uint count)
        {
            string methodRole;
            string methodName;
            if (count > 0)
            {
                FindBugsMethodInfo tmpMethodInfo = new FindBugsMethodInfo();
                methodRole = ReadFindBugsMethodInfo(xmlReader, ref tmpMethodInfo);
                methodName = tmpMethodInfo.GetFullName(context.Buffer);
            }
            else
            {
                methodRole = ReadFindBugsMethodInfo(xmlReader, ref context.MethodInfo);
                methodName = context.MethodInfo.GetFullName(context.Buffer);
            }
            if (methodRole == null)
            {
                methodRole = "METHOD";
            }
            AddWithCount(context.Properties, methodRole, methodName, count);
            return ++count;
        }

        private static uint ConvertFindBugsFieldElement(XmlReader xmlReader, ref BugConversionContext context, uint count)
        {
            string fieldRole;
            string fieldName;
            if (count > 0)
            {
                FindBugsFieldInfo tmpFieldInfo = new FindBugsFieldInfo();
                fieldRole = ReadFindBugsFieldInfo(xmlReader, ref tmpFieldInfo);
                fieldName = tmpFieldInfo.GetFullName(context.Buffer);
            }
            else
            {
                fieldRole = ReadFindBugsFieldInfo(xmlReader, ref context.FieldInfo);
                fieldName = context.FieldInfo.GetFullName(context.Buffer);
            }
            if (fieldRole == null)
            {
                fieldRole = "FIELD";
            }
            AddWithCount(context.Properties, fieldRole, fieldName, count);
            return ++count;
        }

        private static uint ConvertFindBugsTypeElement(XmlReader xmlReader, Dictionary<string, string> properties, StringBuilder buffer, uint count)
        {
            FindBugsTypeInfo typeInfo = new FindBugsTypeInfo();
            string typeRole = ReadFindBugsTypeInfo(xmlReader, ref typeInfo) ?? "TYPE";
            string typeName = typeInfo.GetFullTypeName(buffer);
            AddWithCount(properties, typeRole, typeName, count);
            return ++count;
        }

        private static uint ConvertFindBugsLocalVariableElement(XmlReader xmlReader, Dictionary<string, string> properties, uint count)
        {
            FindBugsLocalVariableInfo varInfo = new FindBugsLocalVariableInfo();
            ReadFindBugsLocalVariableInfo(xmlReader, ref varInfo);
            Debug.Assert(!string.IsNullOrWhiteSpace(varInfo.Role));
            AddWithCount(properties, varInfo.Role, varInfo.Name, count);
            return ++count;
        }

        private static uint ConvertFindBugsIntOrStringElement(
            XmlReader xmlReader,
            Dictionary<string, string> properties,
            uint count,
            string defaultRole)
        {
            string value;
            string role = ReadFindBugsIntOrStringInfo(xmlReader, out value);
            if (role == null)
            {
                role = defaultRole;
            }
            AddWithCount(properties, role, value, count);
            return ++count;
        }

        private static void AddWithCount(Dictionary<string, string> properties, string key, string value, uint count)
        {
            if (count > 0)
            {
                if (!TryAdd(properties, key, value))
                {
                    key = key + '_' + count;
                    properties.Add(key, value);
                }
            }
            else
            {
                properties.Add(key, value);
            }
        }

        // It's a shame that Dictionary<K,V> doesn't have a TryAdd().  Create a less-than-ideal one here.
        private static bool TryAdd(Dictionary<string, string> dict, string key, string value)
        {
            try
            {
                dict.Add(key, value);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        private static Location ConvertFindBugsSourceInfo(ref FindBugsSourceLineInfo sourceInfo)
        {
            Debug.Assert(sourceInfo.IsAvailable());

            Location loc = new Location();
            string path = "UNKNOWN";
            if (sourceInfo.SourcePath != null)
            {
                path = GetSarifPathFromFindBugsSourcePath(sourceInfo.SourcePath);
                string srcPath = @".\src\" + path;
                srcPath = Path.GetFullPath(srcPath);
                if (File.Exists(srcPath))
                {
                    path = srcPath;
                }
            }
            loc.ResultFile = new PhysicalLocation();
            loc.ResultFile.Uri = new Uri(path, UriKind.RelativeOrAbsolute);

            if (sourceInfo.RelSourcePath != null)
            {

            }

            if (sourceInfo.UsingByteCodePositionInfo)
            {
                Debug.Assert(sourceInfo.EndByteCode >= sourceInfo.StartByteCode);
                Debug.Assert(!string.IsNullOrWhiteSpace(sourceInfo.ClassName));

                Region region = new Region();
                region.StartLine = sourceInfo.StartByteCode;
                region.EndLine = sourceInfo.EndByteCode;

                loc.ResultFile.Region = region;
            }
            else
            {
                // Some reports may get the startLine and endLine info messed up
                int startLine = sourceInfo.StartLine;
                int endLine = sourceInfo.EndLine;
                if (endLine == 0)
                {
                    endLine = startLine;    // still makes a single-line but reduces further checks below
                }
                if (startLine > endLine)
                {
                    startLine = endLine;
                    endLine = sourceInfo.StartLine;
                }

                if (startLine != 0)
                {
                    // Don't set the "EndLine" if it's same as "StartLine"
                    if (endLine != startLine)
                    {
                        Region region = new Region();
                        loc.ResultFile.Region = region;
                        region.StartLine = startLine;
                        region.EndLine = endLine;
                    }
                }
            }

            return loc;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static void ReadFindBugsSourceLineInfo(XmlReader xmlReader, ref FindBugsSourceLineInfo sourceInfo)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.SourceLine));

            sourceInfo.Reset();

            int startLine = 0;
            int endLine = 0;
            int startByteCode = 0;
            int endByteCode = 0;

            // Most FindBugs SourceLine info is available via attributes, except the nested optional "Message" element
            // which can be derived from other info and we don't need to save in UIS
            if (!xmlReader.MoveToFirstAttribute())
            {
                // "SourceLine" has a required "classname" attribute
                string info = "missing some required attributes for '" + FindBugsReportXsdConstants.SourceLine + '\'';
                throw xmlReader.CreateException(info);
            }

            // All other attributes are optional
            do
            {
                string attrName = xmlReader.LocalName;
                if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Classname))
                {
                    sourceInfo.ClassName = xmlReader.Value;
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Start))
                {
                    startLine = xmlReader.ReadContentAsInt();
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.End))
                {
                    endLine = xmlReader.ReadContentAsInt();
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Sourcefile))
                {
                    sourceInfo.FileName = xmlReader.Value;
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Sourcepath))
                {
                    sourceInfo.SourcePath = xmlReader.Value;
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.RelSourcepath))
                {
                    sourceInfo.RelSourcePath = xmlReader.Value;
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                {
                    sourceInfo.Role = xmlReader.Value;
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.StartByteCode))
                {
                    startByteCode = xmlReader.ReadContentAsInt();
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.EndByteCode))
                {
                    endByteCode = xmlReader.ReadContentAsInt();
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Synthetic))
                {
                    sourceInfo.IsSynthetic = xmlReader.ReadContentAsBoolean();
                }
                // ignore the "primary" attribute
            }
            while (xmlReader.MoveToNextAttribute());

            // ignore the optional "Message" element

            // Check whether we need to use the byte code position info
            if (((startLine | endLine) == 0) && ((startByteCode | endByteCode) != 0))
            {
                sourceInfo.SetByteCodeInfo(startByteCode, endByteCode);
            }
            else
            {
                sourceInfo.SetSourceCodeInfo(startLine, endLine);
            }
        }

        // A "Class" element in FindBugs reports always has a required "classname" attribute and other optional
        // attributes (e.g. "role", "primary"), and has a required nested "SourceLine" element and an optional
        // nested "Message"element.  We'll ignore the "Message" element since its info can be derived.  Also
        // since the "classname" attribute here is always the same as what from the nested "SourceLine" element,
        // we can safely ignore it.  The only thing that's essential is the optional "Role" attribute, which
        // will be returned if available (otherwise null is returned)
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static string ReadFindBugsClassInfo(XmlReader xmlReader, ref FindBugsSourceLineInfo sourceInfo)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Class));

            string classRole = null;
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                    {
                        classRole = xmlReader.Value;
                        break;
                    }
                    // ignore other attribues ("classname" and "primary")
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "Class" has a required "classname" attribute
                string info = "missing some required attributes for '" + FindBugsReportXsdConstants.Class + '\'';
                throw xmlReader.CreateException(info);
            }

            if (xmlReader.Read() &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.SourceLine))
            {
                ReadFindBugsSourceLineInfo(xmlReader, ref sourceInfo);
            }
            else
            {
                string info = "missing required '" + FindBugsReportXsdConstants.SourceLine + '\'';
                throw xmlReader.CreateException(info);
            }

            return classRole;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static string ReadFindBugsMethodInfo(XmlReader xmlReader, ref FindBugsMethodInfo methodInfo)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Method));

            string methodRole = null;
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Classname))
                    {
                        methodInfo.SourceLineInfo.ClassName = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Name))
                    {
                        methodInfo.Name = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Signature))
                    {
                        methodInfo.Signature = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                    {
                        methodRole = xmlReader.Value;
                        break;
                    }
                    // ignore other attribues ("isStatic" and "primary")
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "Method" has a required "classname" attribute
                string info = "missing some required attributes for '" + FindBugsReportXsdConstants.Method + '\'';
                throw xmlReader.CreateException(info);
            }

            // optional "SourceLine" element
            if (xmlReader.Read() &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.SourceLine))
            {
                ReadFindBugsSourceLineInfo(xmlReader, ref methodInfo.SourceLineInfo);
            }

            // ignore the optional "Message" element

            return methodRole;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static string ReadFindBugsTypeInfo(XmlReader xmlReader, ref FindBugsTypeInfo typeInfo)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Type));

            string typeRole = null;
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Descriptor))
                    {
                        typeInfo.Descriptor = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.TypeParameters))
                    {
                        typeInfo.TypeParameters = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                    {
                        typeRole = xmlReader.Value;
                    }
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "Type" has a required "descriptor" attribute
                throw xmlReader.CreateException(FindBugsReportXsdConstants.Descriptor);
            }

            if (xmlReader.Read() &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.SourceLine))
            {
                ReadFindBugsSourceLineInfo(xmlReader, ref typeInfo.SourceLineInfo);
            }

            return typeRole;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static string ReadFindBugsFieldInfo(XmlReader xmlReader, ref FindBugsFieldInfo fieldInfo)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Field));

            string fieldRole = null;
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Classname))
                    {
                        // Do nothing. It's same as what available in the required "SourceLine" element
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Name))
                    {
                        fieldInfo.Name = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Signature))
                    {
                        fieldInfo.Signature = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.SourceSignature))
                    {
                        fieldInfo.SourceSignature = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                    {
                        fieldRole = xmlReader.Value;
                    }
                    // ignore other attributes ("isStatic" and "primary")
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "Field" has a required "classname" attribute
                string info = "missing some required attributes for '" + FindBugsReportXsdConstants.Field + '\'';
                throw xmlReader.CreateException(info);
            }

            // required "SourceLine" element
            if (xmlReader.Read() &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.SourceLine))
            {
                ReadFindBugsSourceLineInfo(xmlReader, ref fieldInfo.SourceLineInfo);
            }
            else
            {
                string info = "missing required '" + FindBugsReportXsdConstants.SourceLine + '\'';
                throw xmlReader.CreateException(info);
            }

            // ignore the optional "Message" element

            return fieldRole;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
            Justification = "They are element or attribute names defined by the schema")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static void ReadFindBugsLocalVariableInfo(XmlReader xmlReader, ref FindBugsLocalVariableInfo varInfo)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.LocalVariable));

            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Name))
                    {
                        varInfo.Name = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                    {
                        varInfo.Role = xmlReader.Value;
                    }
                    // ignore "register" and "pc" attributes
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "LocalVariable" has a required "register" attribute
                string info = "missing some required attributes for 'LocalVariable'";
                throw xmlReader.CreateException(info);
            }

            // ignore the optional "Message" element
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static string ReadFindBugsIntOrStringInfo(XmlReader xmlReader, out string value)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                (object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.String) ||
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Int)));

            string v = null;
            string role = null;
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Value))
                    {
                        v = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Role))
                    {
                        role = xmlReader.Value;
                    }
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "Int" / "String" has a required "value" attribute
                string info = "missing some required attributes ('value' or 'role')";
                throw xmlReader.CreateException(info);
            }

            // ignore the optional "Message" element

            value = v;
            return role;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static string ReadFindBugsPropertyInfo(XmlReader xmlReader, out string value)
        {
            Debug.Assert(xmlReader != null);
            Debug.Assert(xmlReader.IsStartElement());
            Debug.Assert((xmlReader.NodeType == XmlNodeType.Element) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Property));

            string v = null;
            string name = null;
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    string attrName = xmlReader.LocalName;
                    if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Value))
                    {
                        v = xmlReader.Value;
                    }
                    else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Name))
                    {
                        name = xmlReader.Value;
                    }
                }
                while (xmlReader.MoveToNextAttribute());
                xmlReader.MoveToElement();
            }
            else
            {
                // "Property" has a required "value" attribute
                string info = "missing required 'Property' attributes";
                throw xmlReader.CreateException(info);
            }

            value = v;
            return name;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            MessageId = "toolInfo", Justification = "May be needed if we want to save more info about the tool in UIS")]
        private static void ConvertBugCollectionAttributes(XmlReader xmlReader, Tool toolInfo)
        {
            Debug.Assert((xmlReader != null) && (toolInfo != null));

            do
            {
                string attrName = xmlReader.LocalName;
                if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Version))
                {
                    // This is the FindBugs tool version
                    // toolInfo.Version = xmlReader.Value;
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Sequence))
                {
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.Timestamp))
                {
                }
                else if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.AnalysisTimestamp))
                {
                }
                // ignore the "release" attribute.
            } while (xmlReader.MoveToNextAttribute());
        }

        private static void ConvertProjectAttributes(XmlReader xmlReader)
        {
            Debug.Assert(xmlReader != null);

            do
            {
                string attrName = xmlReader.LocalName;
                if (object.ReferenceEquals(attrName, FindBugsReportXsdConstants.ProjectName))
                {
                    // Do we care about the project name?
                }
                // ignore the "filename" attribute
            } while (xmlReader.MoveToNextAttribute());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "exception message")]
        private static void ConvertProjectElements(XmlReader xmlReader)
        {
            Debug.Assert((xmlReader != null) && (xmlReader.Depth == 1));

            if (xmlReader.MoveToFirstAttribute())
            {
                ConvertProjectAttributes(xmlReader);
                xmlReader.MoveToElement();
            }

            int projectLevel = xmlReader.Depth;
            bool readyToExamine = xmlReader.Read();
            if (!readyToExamine || (xmlReader.Depth <= projectLevel))
            {
                // no more project info left
                return;
            }

            while ((readyToExamine || xmlReader.Read()) && (xmlReader.NodeType == XmlNodeType.Element))
            {
                string elementName = xmlReader.LocalName;

                Debug.Assert(xmlReader.IsStartElement());
                Debug.Assert(!string.IsNullOrWhiteSpace(elementName));
                switch (elementName[0])
                {
                    case 'J':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Jar))
                        {
                            // where to find the target code to analyze (with matching source code specified via SrcDir)
                            //string classPath = xmlReader.ReadContentAsString();
                            break;
                        }
                        goto default;

                    case 'A':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.AuxClasspath))
                        {
                            // where to find "third-party" code (without source code) to help understand the code in question
                            //string auxClassPath = xmlReader.ReadContentAsString();
                            break;
                        }
                        goto default;

                    case 'S':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.SrcDir))
                        {
                            // where to find the matching source
                            //string sourcePath = xmlReader.ReadContentAsString();
                            break;
                        }
                        else if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.SuppressionFilter))
                        {
                            // Filters could be simple or complex expressions.  Haven't seen one example yet though.
                            // could be part of the tool info
                            break;
                        }
                        goto default;

                    case 'P':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Plugin))
                        {
                            // could be part of the tool info
                            //string plugin = xmlReader.ReadContentAsString();
                            break;
                        }
                        goto default;

                    case 'W':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.WrkDir))
                        {
                            // ignore
                            break;
                        }
                        goto default;

                    case 'C':
                        if (object.ReferenceEquals(elementName, FindBugsReportXsdConstants.Cloud))
                        {
                            // ignore
                            break;
                        }
                        goto default;

                    default:
                        throw xmlReader.CreateException("unrecognized element '" + elementName + '\'');
                }

                readyToExamine = SkipRestElementChildren(xmlReader, elementName, projectLevel + 1);
            }

            Debug.Assert((xmlReader.NodeType == XmlNodeType.EndElement) &&
                object.ReferenceEquals(xmlReader.LocalName, FindBugsReportXsdConstants.Project));
        }

        /// <summary> Skip the rest children of the current element node </summary>
        /// <param name="xmlReader"></param>
        /// <param name="elementName"></param>
        /// <param name="elementDepth"> the depth of the current element node </param>
        /// <returns> true if the xmlReader's position is beyond the current element node,
        /// false if it is at the end element position </returns>
        private static bool SkipRestElementChildren(XmlReader xmlReader, string elementName, int elementDepth)
        {
            Debug.Assert((xmlReader != null) && (elementName != null));

            if ((xmlReader.Depth == elementDepth)
                && (xmlReader.NodeType == XmlNodeType.Element)
                && object.ReferenceEquals(xmlReader.LocalName, elementName))
            {
                xmlReader.Read();
            }

            while (xmlReader.Depth > elementDepth)
            {
                xmlReader.Skip();
            }

            return (xmlReader.Depth < elementDepth) || (xmlReader.NodeType != XmlNodeType.EndElement);
        }

        private struct FindBugsReportXsdConstants
        {
            internal static readonly NameTable NameSet = new NameTable();

            internal static readonly string BugCollection = NameSet.Add("BugCollection");

            internal static readonly string Project = NameSet.Add("Project");
            internal static readonly string ProjectName = NameSet.Add("projectName");
            internal static readonly string Jar = NameSet.Add("Jar");
            internal static readonly string AuxClasspath = NameSet.Add("AuxClasspathEntry");
            internal static readonly string SrcDir = NameSet.Add("SrcDir");
            internal static readonly string Plugin = NameSet.Add("Plugin");
            internal static readonly string WrkDir = NameSet.Add("WrkDir");
            internal static readonly string Cloud = NameSet.Add("Cloud");
            internal static readonly string Id = NameSet.Add("id");
            internal static readonly string Enabled = NameSet.Add("enabled");
            internal static readonly string SuppressionFilter = NameSet.Add("SuppressionFilter");
            internal static readonly string Version = NameSet.Add("version");
            internal static readonly string Sequence = NameSet.Add("sequence");
            internal static readonly string Timestamp = NameSet.Add("timestamp");
            internal static readonly string AnalysisTimestamp = NameSet.Add("analysisTimestamp");
            internal static readonly string Release = NameSet.Add("release");

            internal static readonly string Errors = NameSet.Add("Errors");
            internal static readonly string MissingClass = NameSet.Add("MissingClass");
            internal static readonly string ErrorsAttr = NameSet.Add("errors");
            internal static readonly string MissingClassesAttr = NameSet.Add("missingClasses");

            internal static readonly string BugInstance = NameSet.Add("BugInstance");
            internal static readonly string ShortMessage = NameSet.Add("ShortMessage");
            internal static readonly string LongMessage = NameSet.Add("LongMessage");
            internal static readonly string Class = NameSet.Add("Class");
            internal static readonly string Type = NameSet.Add("Type");
            internal static readonly string Method = NameSet.Add("Method");
            internal static readonly string LocalVariable = NameSet.Add("LocalVariable");
            internal static readonly string Field = NameSet.Add("Field");
            internal static readonly string Int = NameSet.Add("Int");
            internal static readonly string String = NameSet.Add("String");
            internal static readonly string Property = NameSet.Add("Property");
            internal static readonly string UserAnnotation = NameSet.Add("UserAnnotation");
            internal static readonly string TypeAttr = NameSet.Add("type");
            internal static readonly string PriorityAttr = NameSet.Add("priority");
            internal static readonly string AbbrevAttr = NameSet.Add("abbrev");
            internal static readonly string CategoryAttr = NameSet.Add("category");
            internal static readonly string RankAttr = NameSet.Add("rank");
            internal static readonly string CweidAttr = NameSet.Add("cweid");
            internal static readonly string InstanceHashAttr = NameSet.Add("instanceHash");
            /* Not used for now
            internal static readonly string UidAttr = NameSet.Add("uid");
            internal static readonly string ReviewsAttr = NameSet.Add("reviews");
            internal static readonly string FirstSeenAttr = NameSet.Add("firstSeen");
            internal static readonly string ConsensusAttr = NameSet.Add("consensus");
            internal static readonly string IsInCloudAttr = NameSet.Add("isInCloud");
            internal static readonly string LastAttr = NameSet.Add("last");
            internal static readonly string RemovedByChangeAttr = NameSet.Add("removedByChange");
            internal static readonly string ShouldFixAttr = NameSet.Add("shouldFix");
            internal static readonly string AgeInDaysAttr = NameSet.Add("ageInDays");
            internal static readonly string NotAProblemAttr = NameSet.Add("notAProblem");
            internal static readonly string InstanceOccurrenceNumAttr = NameSet.Add("instanceOccurrenceNum");
            internal static readonly string InstanceOccurrenceMaxAttr = NameSet.Add("instanceOccurrenceMax");
            */

            internal static readonly string BugCategory = NameSet.Add("BugCategory");
            internal static readonly string BugPattern = NameSet.Add("BugPattern");
            internal static readonly string BugCode = NameSet.Add("BugCode");
            internal static readonly string Summary = NameSet.Add("FindBugsSummary");
            internal static readonly string SummaryHtml = NameSet.Add("SummaryHTML");
            internal static readonly string ClassFeatures = NameSet.Add("ClassFeatures");
            internal static readonly string History = NameSet.Add("History");

            internal static readonly string Message = NameSet.Add("Message");

            internal static readonly string SourceLine = NameSet.Add("SourceLine");
            internal static readonly string Classname = NameSet.Add("classname");
            internal static readonly string Start = NameSet.Add("start");
            internal static readonly string End = NameSet.Add("end");
            internal static readonly string StartByteCode = NameSet.Add("startBytecode");
            internal static readonly string EndByteCode = NameSet.Add("endBytecode");
            internal static readonly string Sourcefile = NameSet.Add("sourcefile");
            internal static readonly string Sourcepath = NameSet.Add("sourcepath");
            internal static readonly string RelSourcepath = NameSet.Add("relSourcepath");
            internal static readonly string Synthetic = NameSet.Add("synthetic");
            internal static readonly string Role = NameSet.Add("role");
            internal static readonly string Primary = NameSet.Add("primary");
            internal static readonly string Descriptor = NameSet.Add("descriptor");
            internal static readonly string TypeParameters = NameSet.Add("typeParameters");
            internal static readonly string Name = NameSet.Add("name");
            internal static readonly string Signature = NameSet.Add("signature");
            internal static readonly string IsStatic = NameSet.Add("isStatic");
            internal static readonly string Register = NameSet.Add("register");
            internal static readonly string Pc = NameSet.Add("pc");
            internal static readonly string SourceSignature = NameSet.Add("sourceSignature");
            internal static readonly string Value = NameSet.Add("value");
        }

        // Represents a SourceLine element in FindBugs reports
        // Although a SourceLine element may contain both source code and byte code position info,
        // we may only need one of the two (most of time source code position info is preferred,
        // unless it's about compiler-generated code that has no source code position info.
        private struct FindBugsSourceLineInfo
        {
            // Ignore the "Message" element

            internal string ClassName { get; set; }
            internal string FileName { get; set; }
            internal string SourcePath { get; set; }
            internal string RelSourcePath { get; set; }
            internal string Role { get; set; }
            internal int StartLine { get; private set; }
            internal int EndLine { get; private set; }

            internal bool IsSynthetic { get; set; }
            internal bool UsingByteCodePositionInfo { get; private set; }

            // Reuse "StartLine" and "EndLine" for "StartByteCode" and "EndByteCode"
            // when "UsingByteCodePositionInfo" is true
            internal int StartByteCode
            {
                get { return StartLine; }
            }

            internal int EndByteCode
            {
                get { return EndLine; }
            }

            internal void SetByteCodeInfo(int startByteCode, int endByteCode)
            {
                StartLine = startByteCode;
                EndLine = endByteCode;
                UsingByteCodePositionInfo = true;
            }

            internal void Reset()
            {
                ClassName = null;
                FileName = null;
                SourcePath = null;
                RelSourcePath = null;
                Role = null;
                StartLine = 0;
                EndLine = 0;
                IsSynthetic = false;
                UsingByteCodePositionInfo = false;
            }

            internal void SetSourceCodeInfo(int startLine, int endLine)
            {
                StartLine = startLine;
                EndLine = endLine;
            }

            internal bool IsAvailable()
            {
                return ClassName != null;
            }

            // Some reports contain "SourceLine" elements with only "classname" attributes
            internal bool IsInteresting()
            {
                return StartLine != 0;
            }

            internal bool Equals(ref FindBugsSourceLineInfo that)
            {
                return (StartLine == that.StartLine)
                    && (EndLine == that.EndLine)
                    && (IsSynthetic == that.IsSynthetic)
                    && (UsingByteCodePositionInfo == that.UsingByteCodePositionInfo)
                    && (ClassName == that.ClassName)
                    && (FileName == that.FileName)
                    && (SourcePath == that.SourcePath)
                    && (RelSourcePath == that.RelSourcePath)
                    && (Role == that.Role);
            }
        }

        private struct FindBugsMethodInfo
        {
            internal string Name { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
                Justification = "May be needed if we want to save the relevant info in UIS")]
            internal string Signature { get; set; }

            internal FindBugsSourceLineInfo SourceLineInfo;

            internal string ClassName
            {
                get { return SourceLineInfo.ClassName; }
            }

            internal bool IsAvailable()
            {
                return Name != null;
            }

            internal string GetFullName(StringBuilder buffer)
            {
                buffer.Clear();
                buffer.Append(ClassName);
                buffer.Append('.');
                buffer.Append(Name);
                return buffer.ToString();
            }
        }

        private struct FindBugsTypeInfo
        {
            internal string Descriptor { get; set; }
            internal string TypeParameters { get; set; }

            internal FindBugsSourceLineInfo SourceLineInfo;

            internal string ClassName
            {
                get { return SourceLineInfo.ClassName; }
            }

            internal string GetFullTypeName(StringBuilder buffer)
            {
                Debug.Assert(Descriptor != null);

                string name = ClassName ?? Descriptor;
                if (TypeParameters == null)
                {
                    return name;
                }

                buffer.Clear();
                buffer.Append(name);
                Debug.Assert(!(TypeParameters.StartsWith("&lt;") && TypeParameters.EndsWith("&gt;")));
                buffer.Append(TypeParameters);
                return buffer.ToString();
            }
        }

        private struct FindBugsLocalVariableInfo
        {
            internal string Name { get; set; }
            internal string Role { get; set; }
        }

        private struct FindBugsFieldInfo
        {
            internal string Name { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
                Justification = "May be needed if we want to save the relevant info in UIS")]
            internal string Signature { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
                Justification = "May be needed if we want to save the relevant info in UIS")]
            internal string SourceSignature { get; set; }

            internal FindBugsSourceLineInfo SourceLineInfo;

            internal string ClassName
            {
                get { return SourceLineInfo.ClassName; }
            }

            internal bool IsAvailable()
            {
                return Name != null;
            }

            internal string GetFullName(StringBuilder buffer)
            {
                Debug.Assert(IsAvailable());
                buffer.Clear();
                buffer.Append(ClassName);
                buffer.Append('.');
                buffer.Append(Name);
                return buffer.ToString();
            }
        }

        private sealed class NameGenerator
        {
            private readonly string pattern;
            private readonly List<string> names;

            internal NameGenerator(string namePattern)
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(namePattern));

                pattern = namePattern;
                names = new List<string>();
            }

            internal string GetName(uint suffix)
            {
                Debug.Assert(suffix < Int32.MaxValue);

                int index = checked((int)suffix);
                List<string> nameList = names;

                int delta = nameList.Count - index;
                while (delta <= 0)
                {
                    nameList.Add(String.Empty);
                    ++delta;
                }

                string name = nameList[index];
                if (object.ReferenceEquals(name, String.Empty))
                {
                    name = string.Format(CultureInfo.InvariantCulture, pattern, suffix);
                    nameList[index] = name;
                }

                return name;
            }
        }
    }
}
