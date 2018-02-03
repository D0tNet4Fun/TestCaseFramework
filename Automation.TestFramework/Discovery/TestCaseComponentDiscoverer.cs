using System.Collections.Generic;
using System.Linq;
using Automation.TestFramework.Entities;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Automation.TestFramework.Discovery
{
    internal class TestCaseComponentDiscoverer
    {
        private readonly ISourceInformationProvider _sourceInformationProvider;
        public ITestClass TestClass { get; }
        public IMessageSink DiagnosticMessageSink { get; }
        public TestMethodDisplay MethodDisplay { get; }

        public TestCaseComponentDiscoverer(ITestClass testClass, IMessageSink diagnosticMessageSink, TestMethodDisplay methodDisplay, ISourceInformationProvider sourceInformationProvider)
        {
            _sourceInformationProvider = sourceInformationProvider;
            TestClass = testClass;
            DiagnosticMessageSink = diagnosticMessageSink;
            MethodDisplay = methodDisplay;
        }

        public IEnumerable<IXunitTestCase> Discover(IMessageBus messageBus)
        {
            var testCases = new List<IXunitTestCase>();
            foreach (var methodInfo in TestClass.Class.GetMethods(includePrivateMethods: true))
            {
                Discover(methodInfo, messageBus, testCases);
            }
            return testCases;
        }

        private void Discover(IMethodInfo methodInfo, IMessageBus messageBus, List<IXunitTestCase> testCases)
        {
            var attribute = methodInfo.GetCustomAttributes(typeof(TestCaseComponentAttribute)).SingleOrDefault();
            if (attribute == null) return;

            var testCase = new TestCase(DiagnosticMessageSink, MethodDisplay, new TestMethod(TestClass, methodInfo));
            testCase.SourceInformation = _sourceInformationProvider.GetSourceInformation(testCase);

            if (!messageBus.QueueMessage(new TestCaseDiscoveryMessage(testCase))) return;

            testCases.Add(testCase);
        }
    }
}