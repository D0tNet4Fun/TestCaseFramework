﻿namespace Automation.TestFramework
{
    /// <summary>
    /// Identifies a test method as the input of a test case step.
    /// </summary>
    public class InputAttribute : TestCaseComponentAttribute
    {
        public InputAttribute(int order, string description)
            : base(order, description)
        {

        }

        protected override string GetDisplayName(string description)
            => $"{Order}. [Input] {description}";
    }
}