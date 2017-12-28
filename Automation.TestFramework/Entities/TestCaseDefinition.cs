﻿using System;
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
        private readonly object[] _constructorArguments;
        private readonly ITestClass _testClass;
        private readonly List<ITest> _preconditions = new List<ITest>();
        private readonly List<TestStep> _testSteps = new List<TestStep>();

        public TestCaseDefinition(ITestCase testCase, object testClassInstance, object[] constructorArguments)
        {
            _testCase = testCase;
            _testClassInstance = testClassInstance;
            _constructorArguments = constructorArguments;
            _testClass = testCase.TestMethod.TestClass;
        }

        public IList<ITest> Preconditions => _preconditions;

        public IList<TestStep> Steps => _testSteps;

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
            var count = testCaseComponents.Count(x => x.attribute is PreconditionAttribute);
            _preconditions.AddRange(Enumerable.Range(0, count).Select(_ => (ITest)null));
            count = testCaseComponents.Count(x => x.attribute is InputAttribute);
            _testSteps.AddRange(Enumerable.Range(0, count).Select(_ => new TestStep()));

            // distribute the test case components into the lists
            foreach (var pair in testCaseComponents)
            {
                var attribute = pair.attribute;
                var index = pair.attribute.Order - 1;
                var test = CreateTest(pair.testMethod.TestClassInstance, pair.testMethod.Method, attribute);

                if (attribute is PreconditionAttribute)
                {
                    _preconditions[index] = test;
                }
                else if (attribute is InputAttribute)
                {
                    _testSteps[index].Input = test;
                }
                else if (attribute is ExpectedResultAttribute)
                {
                    _testSteps[index].ExpectedResult = test;
                }
            }

            // update the display names of the tests
            var testIndex = 0;
            var testCount = testCaseComponents.Count;
            foreach (var precondition in _preconditions)
            {
                UpdateTestDisplayName(precondition, ++testIndex, testCount);
            }
            foreach (var testStep in _testSteps)
            {
                UpdateTestDisplayName(testStep.Input, ++testIndex, testCount);
                if (testStep.ExpectedResult != null)
                {
                    UpdateTestDisplayName(testStep.ExpectedResult, ++testIndex, testCount);
                }
            }
        }

        private IEnumerable<TestMethod> GetTestMethods()
        {
            // if the class has dependencies then handle them first
            var dependencies = _testClass.Class.GetDependencies().ToList();
            foreach (var testClassDependency in dependencies)
            {
                var testClassInstance = GetTestClassInstance(testClassDependency.Class, testClassDependency.Type);
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

            // now handle the class
            foreach (var methodInfo in GetTestMethodsFromClass(_testClass.Class))
            {
                yield return new TestMethod
                {
                    TestClassInstance = _testClassInstance,
                    Method = methodInfo
                };
            }
        }

        private object GetTestClassInstance(ITypeInfo dependencyClass, DependencyType dependencyType)
        {
            switch (dependencyType)
            {
                case DependencyType.Inheritance:
                    return _testClassInstance;
                case DependencyType.Aggregation:
                    var testClassInstance = _constructorArguments.FirstOrDefault(x => x.GetType() == dependencyClass.ToRuntimeType());
                    // todo what if not found?
                    return testClassInstance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependencyType), dependencyType, null);
            }
        }

        private static IEnumerable<IMethodInfo> GetTestMethodsFromClass(ITypeInfo @class)
            => @class.GetMethods(includePrivateMethods: true)
                    .Where(m => m.GetCustomAttributes(typeof(TestCaseComponentAttribute)).Any());

        private ITest CreateTest(object testClassInstance, IMethodInfo testMethod, TestCaseComponentAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.Description))
                attribute.Description = testMethod.GetDisplayNameFromName();
            return new Test(_testCase, testClassInstance, testMethod, attribute.DisplayName); // assign the test to the test case
        }

        private static void UpdateTestDisplayName(ITest test, int index, int count)
            => test.DisplayName = $"[{index}/{count}] {test.DisplayName}";

        [DebuggerDisplay("Method {Method.Name} on {TestClassInstance.GetType().Name}")]
        class TestMethod
        {
            public object TestClassInstance { get; set; }
            public IMethodInfo Method { get; set; }
        }
    }
}