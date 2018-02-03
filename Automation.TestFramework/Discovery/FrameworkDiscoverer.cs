using System.Diagnostics;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Automation.TestFramework.Discovery
{
    internal class FrameworkDiscoverer : XunitTestFrameworkDiscoverer
    {
        public FrameworkDiscoverer(IAssemblyInfo assemblyInfo, ISourceInformationProvider sourceProvider, IMessageSink diagnosticMessageSink, IXunitTestCollectionFactory collectionFactory = null)
            : base(assemblyInfo, sourceProvider, diagnosticMessageSink, collectionFactory)
        {
            //Debugger.Launch();
        }

        protected override bool IsValidTestClass(ITypeInfo type)
        {
            // only classes marked as test cases are valid
            var isTestCase = type.GetCustomAttributes(typeof(TestCaseAttribute)).Any();
            if (!isTestCase)
                DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Class {type.Name} is not a test case."));
            return isTestCase;
        }

        protected override bool FindTestsForType(ITestClass testClass, bool includeSourceInformation, IMessageBus messageBus, ITestFrameworkDiscoveryOptions discoveryOptions)
        {
            var type = testClass.Class;
            var summaryMethods = type.GetMethods(includePrivateMethods: false).Where(m => m.GetCustomAttributes(typeof(SummaryAttribute)).Any()).ToList();
            if (summaryMethods.Count == 0)
            {
                DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Test case class {type.Name} does not have a Summary method. This will not be discovered."));
                return true;
            }

            if (summaryMethods.Count > 1)
            {
                DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Test case class {type.Name} has more than one Summary methods. This will not be discovered."));
                return true;
            }

            return base.FindTestsForType(testClass, includeSourceInformation, messageBus, discoveryOptions);
        }

        protected override bool FindTestsForMethod(ITestMethod testMethod, bool includeSourceInformation, IMessageBus messageBus, ITestFrameworkDiscoveryOptions discoveryOptions)
        {
            // skip methods decorated with TestCaseComponent
            var attribute = testMethod.Method.GetCustomAttributes(typeof(TestCaseComponentAttribute)).SingleOrDefault();
            if (attribute != null) return true;

            return base.FindTestsForMethod(testMethod, includeSourceInformation, messageBus, discoveryOptions);
        }
    }
}