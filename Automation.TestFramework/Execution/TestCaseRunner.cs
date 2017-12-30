﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automation.TestFramework.Entities;
using Xunit.Abstractions;
using Xunit.Sdk;
using ITest = Automation.TestFramework.Entities.ITest;

namespace Automation.TestFramework.Execution
{
    internal class TestCaseRunner : TestCaseRunner<IXunitTestCase>
    {
        private object _testClassInstance;
        private ITest _test; // the test bound to the test case
        private TestCaseDefinition _testCaseDefinition;

        public TestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(testCase, messageBus, aggregator, cancellationTokenSource)
        {
            DisplayName = displayName;
            SkipReason = skipReason;
            ConstructorArguments = constructorArguments;
            TestClass = TestCase.TestMethod.TestClass.Class.ToRuntimeType();
            TestMethod = TestCase.Method.ToRuntimeMethod();
        }

        public string DisplayName { get; }
        public string SkipReason { get; }
        public object[] ConstructorArguments { get; }

        public Type TestClass { get; }
        public MethodInfo TestMethod { get; }

        protected override async Task AfterTestCaseStartingAsync()
        {
            await base.AfterTestCaseStartingAsync();

            // create the test class instance
            _test = new Test(TestCase, TestCase.Method, DisplayName);
            var timer = new ExecutionTimer();
            Aggregator.Run(() => _testClassInstance = _test.CreateTestClass(TestClass, ConstructorArguments, MessageBus, timer, CancellationTokenSource));

            // discover the other tests
            Aggregator.Run(() =>
            {
                _testCaseDefinition = new TestCaseDefinition(TestCase);
                _testCaseDefinition.DiscoverTestCaseComponents();
            });
        }

        protected override Task BeforeTestCaseFinishedAsync()
        {
            var test = new XunitTest(TestCase, DisplayName);
            var timer = new ExecutionTimer();
            Aggregator.Run(() => test.DisposeTestClass(_testClassInstance, MessageBus, timer, CancellationTokenSource));

            return base.BeforeTestCaseFinishedAsync();
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            var runSummary = new RunSummary();

            runSummary.Aggregate(await RunTestCaseComponents());

            // run the summary last
            var runner = CreateTestRunner(_test, TestCase.Method);
            runSummary.Aggregate(await runner.RunAsync());

            return runSummary;
        }

        private TestRunner CreateTestRunner(ITest test, IMethodInfo testMethod)
        {
            var method = (testMethod as IReflectionMethodInfo).MethodInfo;
            return new TestRunner(_testClassInstance, test, MessageBus, TestClass, method, new ExceptionAggregator(Aggregator), CancellationTokenSource);
        }

        private async Task<RunSummary> RunTestCaseComponents()
        {
            var runSummary = new RunSummary();
            foreach (var precondition in _testCaseDefinition.Preconditions)
            {
                var runner = CreateTestRunner(precondition, precondition.MethodInfo);
                runSummary.Aggregate(await runner.RunAsync());
            }

            foreach (var testStep in _testCaseDefinition.Steps)
            {
                var runner = CreateTestRunner(testStep.Input, testStep.Input.MethodInfo);
                runSummary.Aggregate(await runner.RunAsync());

                if (testStep.ExpectedResult != null)
                {
                    runner = CreateTestRunner(testStep.ExpectedResult, testStep.ExpectedResult.MethodInfo);
                    runSummary.Aggregate(await runner.RunAsync());
                }
            }

            return runSummary;
        }
    }
}