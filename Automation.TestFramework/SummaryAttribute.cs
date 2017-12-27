﻿using System;
using Xunit;

namespace Automation.TestFramework
{
    /// <summary>
    /// Identifies a test method as the summary of a test case.
    /// </summary>
    public class SummaryAttribute : FactAttribute
    {
        private readonly string _description;

        public SummaryAttribute(string description)
        {
            if (string.IsNullOrEmpty(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));

            _description = description;
        }

        public override string DisplayName
        {
            get => _description;
            set => throw new NotSupportedException("Overridden by constructor");
        }
    }
}