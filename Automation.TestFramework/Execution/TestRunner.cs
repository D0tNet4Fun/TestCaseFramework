﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
using ITest = Automation.TestFramework.Entities.ITest;

namespace Automation.TestFramework.Execution
{
    internal class TestRunner : TestRunner<ITestCase>, ITestRunner
    {
        private readonly Type _testNotificationType;
        private TestInvoker _testInvoker;

        public TestRunner(ITest test, IMessageBus messageBus, object[] constructorArguments, MethodInfo testMethod, string skipReason, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, Type testNotificationType)
            : base(test, messageBus, test.Instance.GetType(), constructorArguments, testMethod, new object[0], skipReason, aggregator, cancellationTokenSource)
        {
            _testNotificationType = testNotificationType;
        }

        public object TestMethodResult => _testInvoker.TestMethodResult;

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

        protected virtual Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            _testInvoker = new TestInvoker((ITest)Test, MessageBus, TestClass, TestMethod, aggregator, CancellationTokenSource, _testNotificationType);
            return _testInvoker.RunAsync();
        }
    }
}