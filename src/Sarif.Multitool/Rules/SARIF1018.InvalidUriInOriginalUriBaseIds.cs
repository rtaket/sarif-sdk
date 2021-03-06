﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Json.Pointer;

namespace Microsoft.CodeAnalysis.Sarif.Multitool.Rules
{
    public class InvalidUriInOriginalUriBaseIds : SarifValidationSkimmerBase
    {
        private readonly MultiformatMessageString _fullDescription = new MultiformatMessageString
        {
            Text = RuleResources.SARIF1018_InvalidUriInOriginalUriBaseIds
        };

        public override MultiformatMessageString FullDescription => _fullDescription;

        public override FailureLevel DefaultLevel => FailureLevel.Error;

        /// <summary>
        /// SARIF1018
        /// </summary>
        public override string Id => RuleId.InvalidUriInOriginalUriBaseIds;

        protected override IEnumerable<string> MessageResourceNames => new string[]
        {
            nameof(RuleResources.SARIF1018_Default)
        };

        protected override void Analyze(Run run, string runPointer)
        {
            if (run.OriginalUriBaseIds != null)
            {
                string originalUriBaseIdsPointer = runPointer.AtProperty(SarifPropertyName.OriginalUriBaseIds);

                foreach (string key in run.OriginalUriBaseIds.Keys)
                {
                    AnalyzeOriginalUriBaseIdsEntry(run.OriginalUriBaseIds[key], originalUriBaseIdsPointer.AtProperty(key));
                }
            }
        }

        private void AnalyzeOriginalUriBaseIdsEntry(ArtifactLocation artifactLocation, string pointer)
        {
            // If uriBaseId is present, the uri must be relative. But this is true for _all_
            // artifactLocation objects, not just the ones in run.originalUriBaseIds, so we
            // will not verify it here. There will be a separate validation rule to enforce
            // this condition. See https://github.com/microsoft/sarif-sdk/issues/1643.
            if (artifactLocation.UriBaseId != null) { return; }

            // We know that uriBaseId is absent. In this case, uri must _either_ be an absolute
            // URI, or it must be absent.
            if (artifactLocation.Uri == null) { return; }

            // We know that uri is present, so now we can verify that it's an absolute URI.

            // If it's not a well-formed URI of _any_ kind, then don't bother triggering this rule.
            // Rule SARIF1003, UrisMustBeValid, will catch it.
            // Check for well-formedness first, before attempting to create a Uri object, to
            // avoid having to do a try/catch. Unfortunately Uri.TryCreate will return true
            // even for a malformed URI string.
            string uriString = artifactLocation.Uri.OriginalString;
            if (uriString != null && Uri.IsWellFormedUriString(uriString, UriKind.RelativeOrAbsolute))
            {
                // Ok, it's a well-formed URI of some kind. If it's not absolute, _now_ we
                // can report it.
                Uri uri = new Uri(uriString, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                {
                    LogResult(pointer, nameof(RuleResources.SARIF1018_Default), uriString);
                }
            }
        }
    }
}
