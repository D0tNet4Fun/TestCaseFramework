using System;
using Xunit;

namespace Automation.TestFramework.Tests
{
    [TestCase("id")]
    public class ExpectedResultTestCaseWithAssert : IDisposable
    {
        private bool _called;

        [Summary("Expected result with assert")]
        public void Summary() { }

        [Input]
        public void Input() { }

        [ExpectedResult]
        public void ExpectedResult()
        {
            TestStep.Current.ExpectedResult
                .Assert("This should fail", () => Assert.False(1 == 2))
                .Verify("This could work but it should not be called", () =>
                {
                    _called = true;
                    Assert.True(1 != 2);
                });
        }

        public void Dispose()
        {
            Assert.False(_called);
        }
    }
}