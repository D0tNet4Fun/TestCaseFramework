using System;
using System.Threading;
using System.Threading.Tasks;
using Automation.TestFramework.Entities;
using Xunit.Sdk;

namespace Automation.TestFramework.Execution
{
    internal class ExpectedResultTestRunner : TestRunner
    {
        public ExpectedResult ExpectedResult { get; }

        public ExpectedResultTestRunner(ITest test, Func<ITest, TestRunner> testRunnerFactory,
            TestCounter testCounter, IMessageBus messageBus, object[] constructorArguments, string skipReason, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, Type testNotificationType)
            : base(test, testCounter, messageBus, constructorArguments, test.MethodInfo.ToRuntimeMethod(), skipReason, aggregator, cancellationTokenSource, testNotificationType)
        {
            ExpectedResult = new ExpectedResult(test, testRunnerFactory);
        }

        protected override void InitializeTestStep()
        {
            base.InitializeTestStep();
            TestStep.Current.ExpectedResult = ExpectedResult;
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            // run the test first to collect all the expected result's assertions; if it fails then exit.
            var result = await base.InvokeTestMethodAsync(aggregator);
            if (aggregator.HasExceptions) return result;

            // check if the expected result has errors; if so then throw a generic exception in the end
            if (ExpectedResult.RunSummary.Failed > 0)
            {
                var errorMessage = "One or more of the assertions failed.";
                if (ExpectedResult.RunSummary.Skipped > 0) errorMessage += $" {ExpectedResult.RunSummary.Skipped} assertion(s) were skipped.";
                throw new ExpectedResultFailedException(errorMessage);
            }

            return result;
        }
    }
}