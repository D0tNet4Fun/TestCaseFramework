using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Automation.TestFramework.Execution
{
    internal class TestAssemblyRunner : XunitTestAssemblyRunner
    {
        public ISourceInformationProvider SourceInformationProvider { get; }
        private Type _testNotificationType;

        public TestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, ISourceInformationProvider sourceInformationProvider)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
            SourceInformationProvider = sourceInformationProvider;
        }

        #region AssemblyFixture - copied from Xunit Samples

        private readonly Dictionary<Type, object> _assemblyFixtureMappings = new Dictionary<Type, object>();

        protected override async Task AfterTestAssemblyStartingAsync()
        {
            // Let everything initialize
            await base.AfterTestAssemblyStartingAsync();

            // Go find all the AssemblyFixtureAttributes adorned on the test assembly
            Aggregator.Run(() =>
            {
                var fixturesAttrs = ((IReflectionAssemblyInfo)TestAssembly.Assembly).Assembly
                    .GetCustomAttributes(typeof(AssemblyFixtureAttribute), false)
                    .Cast<AssemblyFixtureAttribute>()
                    .ToList();

                // Instantiate all the fixtures
                foreach (var fixtureAttr in fixturesAttrs)
                    _assemblyFixtureMappings[fixtureAttr.FixtureType] = Activator.CreateInstance(fixtureAttr.FixtureType);
            });

            Aggregator.Run(GetTestNotification);
        }

        protected override Task BeforeTestAssemblyFinishedAsync()
        {
            // Make sure we clean up everybody who is disposable, and use Aggregator.Run to isolate Dispose failures
            foreach (var disposable in _assemblyFixtureMappings.Values.OfType<IDisposable>())
                Aggregator.Run(disposable.Dispose);

            return base.BeforeTestAssemblyFinishedAsync();
        }

        #endregion

        private void GetTestNotification()
        {
            var testNotificationAttribute = ((IReflectionAssemblyInfo)TestAssembly.Assembly).Assembly
                .GetCustomAttributes(typeof(TestNotificationAttribute), false)
                .Cast<TestNotificationAttribute>()
                .SingleOrDefault();

            _testNotificationType = testNotificationAttribute?.Type;
        }

        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
            => new TestCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource, _assemblyFixtureMappings, _testNotificationType, ExecutionOptions.MaxParallelThreadsOrDefault(), SourceInformationProvider).RunAsync();
    }
}