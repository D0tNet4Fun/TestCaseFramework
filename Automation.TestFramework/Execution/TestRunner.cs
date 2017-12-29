﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
using ITest = Automation.TestFramework.Entities.ITest;

namespace Automation.TestFramework.Execution
{
    internal class TestRunner : TestRunner<ITestCase>
    {
        private readonly Type _testNotificationType;

        public TestRunner(ITest test, IMessageBus messageBus, Type testClass, MethodInfo testMethod, string skipReason, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, Type testNotificationType)
            : base(test, messageBus, testClass, new object[0], testMethod, new object[0], skipReason, aggregator, cancellationTokenSource)
        {
            _testNotificationType = testNotificationType;
        }

        public new ITest Test => (ITest)base.Test;

        protected override async Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
        {
            // copy from XunitTestRunner

            var output = string.Empty;

            TestOutputHelper testOutputHelper = null;
            foreach (object obj in ConstructorArguments)
            {
                testOutputHelper = obj as TestOutputHelper;
                if (testOutputHelper != null)
                    break;
            }

            if (testOutputHelper != null)
                testOutputHelper.Initialize(MessageBus, Test);

            var executionTime = await InvokeTestMethodAsync(aggregator);

            if (testOutputHelper != null)
            {
                output = testOutputHelper.Output;
                testOutputHelper.Uninitialize();
            }

            return Tuple.Create(executionTime, output);
        }

        private Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
            => new TestInvoker(Test, MessageBus, TestClass, TestMethod, aggregator, CancellationTokenSource, _testNotificationType).RunAsync();
    }
}