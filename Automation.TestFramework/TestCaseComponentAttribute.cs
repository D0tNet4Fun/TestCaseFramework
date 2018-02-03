using System;
using Xunit;
using Xunit.Sdk;

namespace Automation.TestFramework
{
    /// <summary>
    /// Represents an attribute used to decorate a test method that represents a component of a test case.
    /// </summary>
    [XunitTestCaseDiscoverer("Automation.TestFramework.FakeDiscoverer", "Automation.TestFramework")]
    public abstract class TestCaseComponentAttribute : FactAttribute
    {
        /// <summary>
        /// Gets the order in which the test method needs to be executed.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseComponentAttribute"/> class.
        /// </summary>
        /// <param name="order">The order in which the target test method needs to be executed.</param>
        /// <param name="description">The user friendly description of the test method.</param>
        protected TestCaseComponentAttribute(int order = 1, string description = null)
        {
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "Order must be positive");

            Order = order;
            Description = description;
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public override string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(Description)) return base.DisplayName;
                return GetDisplayName(Description);
            }
        }

        /// <summary>
        /// Returns the display name based on the specified description.
        /// </summary>
        /// <param name="description">The description to use in the display name.</param>
        /// <returns>The display name.</returns>
        protected abstract string GetDisplayName(string description);
    }
}