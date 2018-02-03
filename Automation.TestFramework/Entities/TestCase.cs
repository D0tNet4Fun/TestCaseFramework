using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Automation.TestFramework.Execution;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Automation.TestFramework.Entities
{
    internal class TestCase : XunitTestCase
    {
        private Dictionary<Type, object> _classFixtureMappings;
        private Type _testNotificationType;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes", error: true)]
        public TestCase()
        {
        }

        public TestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            if (string.Equals(displayName, BaseDisplayName, StringComparison.InvariantCultureIgnoreCase))
                return TestMethod.Method.GetDisplayNameFromName();
            return base.GetDisplayName(factAttribute, displayName);
        }

        //public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        //    => new TestCaseRunner(this, DisplayName, SkipReason, constructorArguments, messageBus, aggregator, cancellationTokenSource, _classFixtureMappings, _testNotificationType).RunAsync();

        internal void SetClassFixtureMappings(Dictionary<Type, object> classFixtureMappings)
        {
            _classFixtureMappings = classFixtureMappings;
        }

        public void SetAssemblyTestNotificationType(Type testNotificationType)
        {
            _testNotificationType = testNotificationType;
        }
    }
}