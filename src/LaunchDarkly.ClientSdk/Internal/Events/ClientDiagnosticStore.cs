﻿using System;
using System.Collections.Generic;
using LaunchDarkly.Sdk.Internal.Events;
using LaunchDarkly.Sdk.Internal.Http;
using LaunchDarkly.Sdk.Client.Interfaces;

using static LaunchDarkly.Sdk.Internal.Events.DiagnosticConfigProperties;

namespace LaunchDarkly.Sdk.Client.Internal.Events
{
    internal class ClientDiagnosticStore : DiagnosticStoreBase
    {
        private readonly LdClientContext _context;
        private readonly Configuration _config;
        private readonly TimeSpan _startWaitTime;

        protected override string SdkKeyOrMobileKey => _context.MobileKey;
        protected override string SdkName => "dotnet-client-sdk";
        protected override IEnumerable<LdValue> ConfigProperties => GetConfigProperties();
        protected override string DotNetTargetFramework => GetDotNetTargetFramework();
        protected override HttpProperties HttpProperties => _context.Http.HttpProperties;
        protected override Type TypeOfLdClient => typeof(LdClient);

        internal ClientDiagnosticStore(LdClientContext context, Configuration config, TimeSpan startWaitTime)
        {
            _context = context;
            _config = config;
            _startWaitTime = startWaitTime;
            // We pass in startWaitTime separately because in the client-side SDK, it is not
            // part of the configuration - it is a separate parameter to the LdClient
            // constructor. That's because this parameter only matters if they use the
            // synchronous method LdClient.Init(); if they use LdClient.InitAsync() instead,
            // there's no such thing as a startup timeout within the SDK (in which case this
            // parameter will be zero and the corresponding property in the diagnostic event
            // data will be zero, since there is no meaningful value for it).
        }

        private IEnumerable<LdValue> GetConfigProperties()
        {
            yield return LdValue.BuildObject()
                .WithStartWaitTime(_startWaitTime)
                .Add("backgroundPollingDisabled", !_config.EnableBackgroundUpdating)
                .Add("evaluationReasonsRequested", _config.EvaluationReasons)
                .Build();

            // Allow each pluggable component to describe its own relevant properties.
            yield return GetComponentDescription(_config.DataSourceFactory ?? Components.StreamingDataSource());
            yield return GetComponentDescription(_config.EventProcessorFactory ?? Components.SendEvents());
            yield return GetComponentDescription(_config.HttpConfigurationBuilder ?? Components.HttpConfiguration());
        }

        private LdValue GetComponentDescription(object component) =>
            component is IDiagnosticDescription dd ?
                dd.DescribeConfiguration(_context) : LdValue.Null;

        internal static string GetDotNetTargetFramework()
        {
            // Note that this is the _target framework_ that was selected at build time based on the application's
            // compatibility requirements; it doesn't tell us anything about the actual OS version. We'll need to
            // update this whenever we add or remove supported target frameworks in the .csproj file.
#if NETSTANDARD2_0
            return "netstandard2.0";
#elif MONOANDROID71
            return "monoandroid7.1";
#elif MONOANDROID80
            return "monoandroid8.0";
#elif MONOANDROID81
            return "monoandroid8.1";
#elif XAMARIN_IOS10
            return "xamarinios1.0";
#else
            return "unknown";
#endif
        }
    }
}
