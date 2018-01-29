using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit.Abstractions;

namespace Automation.TestFramework.Entities
{
    internal class TestCaseDefinition
    {
        private readonly ITestCase _testCase;
        private readonly object _testClassInstance;
        private readonly Dictionary<Type, object> _classFixtureMappings;
        private readonly ITestClass _testClass;
        private readonly List<ITest> _setups = new List<ITest>();
        private readonly List<ITest> _preconditions = new List<ITest>();
        private readonly List<ITest> _inputs = new List<ITest>();
        private readonly List<ITest> _expectedResults = new List<ITest>();
        private readonly List<ITest> _cleanups = new List<ITest>();
        private readonly Dictionary<IMethodInfo, IMethodInfo> _duplicates = new Dictionary<IMethodInfo, IMethodInfo>();

        public TestCaseDefinition(ITestCase testCase, object testClassInstance, Dictionary<Type, object> classFixtureMappings)
        {
            _testCase = testCase;
            _testClassInstance = testClassInstance;
            _classFixtureMappings = classFixtureMappings;
            _testClass = testCase.TestMethod.TestClass;
        }

        public IList<ITest> Setups => _setups;

        public IList<ITest> Preconditions => _preconditions;

        public IList<TestStep> Steps
            => Enumerable.Range(0, _inputs.Count).Select(i => new TestStep
            {
                Input = _inputs[i],
                ExpectedResult = _expectedResults[i] // can be null
            }).ToList();

        public IList<ITest> Cleanups => _cleanups;

        public void DiscoverTestCaseComponents()
        {
            var testMethods = GetTestMethods();

            // get the test methods which are test case components, and their attribute
            var testCaseComponents =
                (from testMethod in testMethods
                 let attributeInfo = testMethod.Method.GetCustomAttributes(typeof(TestCaseComponentAttribute)).SingleOrDefault()
                 where attributeInfo != null
                 let attribute = (TestCaseComponentAttribute)(attributeInfo as IReflectionAttributeInfo)?.Attribute
                 where attribute != null
                 select new { testMethod, attribute })
                .ToList();

            // initialize the lists
            var count = testCaseComponents.Count(x => x.attribute is SetupAttribute);
            InitializeList(_setups, count);
            count = testCaseComponents.Count(x => x.attribute is PreconditionAttribute);
            InitializeList(_preconditions, count);
            count = testCaseComponents.Count(x => x.attribute is InputAttribute);
            InitializeList(_inputs, count);
            InitializeList(_expectedResults, count); // same count as input, as there may be inputs without expected results but not the other way around
            count = testCaseComponents.Count(x => x.attribute is CleanupAttribute);
            InitializeList(_cleanups, count);

            // distribute the test case components into the lists
            foreach (var pair in testCaseComponents)
            {
                var attribute = pair.attribute;
                var index = pair.attribute.Order - 1;
                var test = CreateTest(pair.testMethod.TestClassInstance, pair.testMethod.Method, attribute);

                if (attribute is SetupAttribute) TryUpdateList(_setups, index, test);
                else if (attribute is PreconditionAttribute) TryUpdateList(_preconditions, index, test);
                else if (attribute is InputAttribute) TryUpdateList(_inputs, index, test);
                else if (attribute is ExpectedResultAttribute) TryUpdateList(_expectedResults, index, test);
                else if (attribute is CleanupAttribute) TryUpdateList(_cleanups, index, test);
            }

            CheckForDuplicates(); // this can throw

            //UpdateDisplayNames(testCaseComponents.Count);
        }

        private void TryUpdateList(IList<ITest> list, int index, ITest test)
        {
            // if the list contains a test at index, then the input test is considered a duplicate of that one and it will not be inserted in the list
            var existingTest = list[index];
            if (existingTest != null)
                _duplicates.Add(test.MethodInfo, existingTest.MethodInfo);
            else
                list[index] = test;
        }

        private void CheckForDuplicates()
        {
            if (_duplicates.Count > 0)
            {
                var errorMessages = _duplicates.Select(dup =>
                    $"Method {dup.Key.Name} in class {dup.Key.ToRuntimeMethod().DeclaringType.Name} " +
                    "is not allowed to have the same order as " +
                    $"method {dup.Value.Name} in class {dup.Value.ToRuntimeMethod().DeclaringType.Name}.")
                    .ToList();
                errorMessages.Insert(0, "Invalid test case definition:");
                throw new TestCaseFailedException(string.Join(Environment.NewLine, errorMessages));
            }
        }

        private void UpdateDisplayNames(int testCount)
        {
            var testIndex = 0;
            foreach (var setup in Setups)
            {
                UpdateTestDisplayName(setup, ref testIndex, testCount);
            }
            foreach (var precondition in Preconditions)
            {
                UpdateTestDisplayName(precondition, ref testIndex, testCount);
            }
            foreach (var step in Steps)
            {
                UpdateTestDisplayName(step.Input, ref testIndex, testCount);
                if (step.ExpectedResult != null)
                {
                    UpdateTestDisplayName(step.ExpectedResult, ref testIndex, testCount);
                }
            }
            foreach (var cleanup in Cleanups)
            {
                UpdateTestDisplayName(cleanup, ref testIndex, testCount);
            }
        }

        private IEnumerable<TestMethod> GetTestMethods()
        {
            // if the class has dependencies then handle them first
            var dependencies = _testClass.Class.GetDependencies().ToList();
            foreach (var testClassDependency in dependencies.Where(d => d.Type == DependencyType.Aggregation))
            {
                _classFixtureMappings.TryGetValue(testClassDependency.Class.ToRuntimeType(),
                     out var testClassInstance); // will be null if not found
                var testMethods = GetTestMethodsFromClass(testClassDependency.Class);
                foreach (var testMethod in testMethods)
                {
                    yield return new TestMethod
                    {
                        TestClassInstance = testClassInstance,
                        Method = testMethod
                    };
                }
            }

            // now handle the class and its hierarchy
            foreach (var methodInfo in GetTestMethodsFromClass(_testClass.Class))
            {
                yield return new TestMethod
                {
                    TestClassInstance = _testClassInstance,
                    Method = methodInfo
                };
            }
        }

        private static void InitializeList<T>(List<T> list, int count)
            => list.AddRange(Enumerable.Range(0, count).Select(_ => default(T)));

        private static IEnumerable<IMethodInfo> GetTestMethodsFromClass(ITypeInfo @class)
            => @class.GetMethods(includePrivateMethods: true).Where(m => m.GetCustomAttributes(typeof(TestCaseComponentAttribute)).Any());

        private ITest CreateTest(object testClassInstance, IMethodInfo testMethod, TestCaseComponentAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.Description))
                attribute.Description = testMethod.GetDisplayNameFromName();
            return new Test(_testCase, testClassInstance, testMethod, attribute.DisplayName); // assign the test to the test case
        }

        private static void UpdateTestDisplayName(ITest test, ref int index, int count)
        {
            var prefix = $"[{(++index).ToString("D" + GetMaxNumberOfDigits(count))}/{count}]";
            test.DisplayName = prefix + " " + test.DisplayName;
        }

        private static int GetMaxNumberOfDigits(int value)
        {
            // https://stackoverflow.com/questions/4483886/how-can-i-get-a-count-of-the-total-number-of-digits-in-a-number
            return (int)Math.Floor(Math.Log10(value) + 1);
        }

        [DebuggerDisplay("Method {Method.Name} on {TestClassInstance.GetType().Name}")]
        private class TestMethod
        {
            public object TestClassInstance { get; set; }
            public IMethodInfo Method { get; set; }
        }
    }
}