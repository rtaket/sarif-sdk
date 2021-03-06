﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.Writers;

namespace Microsoft.CodeAnalysis.Sarif.Multitool
{
    internal class ConvertCommand : CommandBase
    {
        public int Run(ConvertOptions convertOptions, IFileSystem fileSystem = null)
        {
            if (fileSystem == null) { fileSystem = new FileSystem(); }

            try
            {
                if (string.IsNullOrEmpty(convertOptions.OutputFilePath))
                {
                    convertOptions.OutputFilePath = convertOptions.InputFilePath + ".sarif";
                }

                if (fileSystem.DirectoryExists(convertOptions.OutputFilePath))
                {
                    Console.Error.WriteLine(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "The output path '{0}' is a directory.",
                            convertOptions.OutputFilePath));
                    return FAILURE;
                }

                if (!ValidateOptions(convertOptions, fileSystem)) { return FAILURE; }

                LoggingOptions loggingOptions = LoggingOptions.None;

                OptionallyEmittedData dataToInsert = convertOptions.DataToInsert.ToFlags();

                if (convertOptions.PrettyPrint)
                {
                    loggingOptions |= LoggingOptions.PrettyPrint;
                };

                if (convertOptions.Force)
                {
                    loggingOptions |= LoggingOptions.OverwriteExistingOutputFile;
                };

                new ToolFormatConverter().ConvertToStandardFormat(
                                                convertOptions.ToolFormat,
                                                convertOptions.InputFilePath,
                                                convertOptions.OutputFilePath,
                                                loggingOptions,
                                                dataToInsert,
                                                convertOptions.PluginAssemblyPath);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine(ex);
                return FAILURE;
            }

            return SUCCESS;
        }

        private static bool ValidateOptions(ConvertOptions convertOptions, IFileSystem fileSystem)
        {
            bool valid = true;

            valid &= convertOptions.ValidateOutputOptions();

            valid &= DriverUtilities.ReportWhetherOutputFileCanBeCreated(convertOptions.OutputFilePath, convertOptions.Force, fileSystem);

            return valid;
        }
    }
}
