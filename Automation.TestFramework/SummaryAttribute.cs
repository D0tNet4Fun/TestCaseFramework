using System;
using Xunit;
using Xunit.Sdk;

namespace Automation.TestFramework
{
    /// <summary>
    /// Identifies a test method as the summary of a test case.
    /// </summary>
    //[XunitTestCaseDiscoverer("Automation.TestFramework.Discovery.TestCaseDiscoverer", "Automation.TestFramework")]
    public class SummaryAttribute : FactAttribute
    {
        private readonly string _description;

        /// <summary>
        /// Initializes a new instance of the <see cref="SummaryAttribute"/> class.
        /// </summary>
        /// <param name="description">The user friendly description of the target test method.</param>
        public SummaryAttribute(string description = null)
        {
            _description = description;
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public override string DisplayName
        {
            get => _description;
            set => throw new NotSupportedException("Overridden by constructor");
        }
    }
}