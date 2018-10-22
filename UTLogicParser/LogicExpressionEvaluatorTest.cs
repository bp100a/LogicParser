using LogicParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.JScript;
using System;

namespace UTLogicParser
{
    
    public struct TestInput{
        public status expected; public string e;

        public TestInput(status expectedresult, string expression)
        {
            expected = expectedresult; e = expression;
        }
    }

    
    /// <summary>
    ///This is a test class for LogicExpressionEvaluatorTest and is intended
    ///to contain all LogicExpressionEvaluatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LogicExpressionEvaluatorTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for ValidateExpression
        ///</summary>
        [TestMethod()]
        [DeploymentItem("LogicParser.dll")]
        public void ValidateExpressionTest()
        {
            TestInput[] ti = {

                              // good expressions
                              new TestInput(status.success,           "[]()(())[[()]]"),
                              new TestInput(status.success,           "50 < 10043"),
                              new TestInput(status.success,           "1234 > 57"),
                              new TestInput(status.success,           "-1234 < 57"),
                              new TestInput(status.success,           "123.4 > 57.5"),
                              new TestInput(status.success,           "123.2 > 123.1"),

                              // bad expressions
                              new TestInput(status.invalidexpression, "(5 > 3) && (1 || 0))("),
                              new TestInput(status.invalidexpression, "[(15 < 2) && (0 && 1)]]["),
                              new TestInput(status.invalidexpression, "abcdeftrue")
                             };

            LogicExpressionEvaluator_Accessor target = new LogicExpressionEvaluator_Accessor(); // TODO: Initialize to an appropriate value

            for (int i = 0; i < ti.Length; i++)
            {
                status retval = LogicExpressionEvaluator_Accessor.ValidateExpression(ti[i].e);
                Assert.IsTrue((ti[i].expected == retval), ti[i].e);
            }
        }

        /// <summa]ry>
        ///A test for Evaluate
        ///</summary>
        [TestMethod()]
        [DeploymentItem("LogicParser.dll")]
        public void EvaluateTest()
        {
            TestInput[] ti = {
                              new TestInput(status.istrue,         "1 && !0"),
                              new TestInput(status.istrue,         "!0"),
                              new TestInput(status.istrue,         "!0.0"),
                              new TestInput(status.isfalse,        "!1"),
                              new TestInput(status.isfalse,        "!37.42385"),
                              new TestInput(status.isfalse,        "!(1)"),
                              new TestInput(status.istrue,         "!(0)"),
                              new TestInput(status.istrue,         "57 > !63"),
                              new TestInput(status.istrue,         "!(0) || !(1) && (57 > 42.3) || !47"),
                              new TestInput(status.isfalse,        "[!(0) || !(1) && (57 > 42.3) || !47] && 0"),
                              new TestInput(status.isfalse,        "!(!(0) || !(1) && (57 > 42.3) || !47)"),

                              new TestInput(status.istrue,         "1 != 0"),
                              new TestInput(status.isfalse,        "25.3 != 25.3"),

                              new TestInput(status.istrue,         "121.347"),
                              new TestInput(status.isfalse,        "0.0"),
                              new TestInput(status.istrue,         "58"),
                              new TestInput(status.isfalse,        "0"),
                              new TestInput(status.istrue,         "321.2 = 321.2"),
                              new TestInput(status.istrue,         "(121.347)"),
                              new TestInput(status.isfalse,        "(0.0)"),
                              new TestInput(status.istrue,         "(58)"),
                              new TestInput(status.isfalse,        "(0)"),
                              new TestInput(status.istrue,         "(321.2 = 321.2)"),

                              new TestInput(status.istrue,         "123.2 > 123.1"),
                              new TestInput(status.istrue,         "537.89 < 788.47"),
                              new TestInput(status.istrue,         "321.2 = 321.2"),
                              new TestInput(status.isfalse,        "321.2 = 321.1"),
                              new TestInput(status.istrue,         "57 = 57"),
                              new TestInput(status.isfalse,        "57 = -57"),
                              new TestInput(status.isundefined,    "57 = x"),
                              new TestInput(status.isfalse,        "6380.5672 < 1043.61"),
                              new TestInput(status.isfalse,        "57.8973 > 100.42"),

                              new TestInput(status.istrue,         "-5 < 2"),
                              new TestInput(status.istrue,         "5 > 2"),
                              new TestInput(status.istrue,         "50 < 10043"),

                              new TestInput(status.isfalse,        "12 < 4"),
                              new TestInput(status.isfalse,        "47 > 89"),

                              new TestInput(status.istrue,         "(1 && 0) || (1 && 1)"),
                              new TestInput(status.isfalse,        "((1 && 0) || (1 && 1)) && 0"),
                              new TestInput(status.isundefined,    "((1 && 0) || (1 && 1)) && x"),
                              new TestInput(status.isfalse,        "(1 && 0) || (1 && 0)"),
                              new TestInput(status.istrue,         "1 || 0 && 0"),
                              new TestInput(status.isfalse,        "(1 || 0) && 0"),

                              new TestInput(status.istrue,         "1 && 1"),
                              new TestInput(status.isfalse,        "1 && 0"),
                              new TestInput(status.istrue,         "1 || 0 && 1"),
                              new TestInput(status.istrue,         "1 && 1 || 1 && 0"),
                              new TestInput(status.istrue,         "1 || 0 && 1 && 1"),
                              new TestInput(status.isfalse,        "1 && 1 && 0 && 1"),

                              new TestInput(status.isundefined,    "1 && x"),
                              new TestInput(status.isfalse,        "0 && x && 0 && 1"),
                              new TestInput(status.isundefined,    "0 || x"),
                              new TestInput(status.istrue,         "1 || x"),

                              new TestInput(status.istrue,         "1 || true"),
                              new TestInput(status.isfalse,        "1 && false"),
                              new TestInput(status.istrue,         "1 && true"),
                              new TestInput(status.isundefined,    "1 && true && x"),
                              new TestInput(status.isfalse,        "1 && false && x"),

                              new TestInput(status.incomplete,   "1 & false & x"),
                              new TestInput(status.incomplete,   "1 | false | x"),
                              
                              new TestInput(status.isundefined,    "57 > x"),
                              new TestInput(status.isundefined,    "x > x"),
                              new TestInput(status.isundefined,    "x > 57"),
                              
                              new TestInput(status.isundefined,    "57 < x"),
                              new TestInput(status.isundefined,    "x < x"),
                              new TestInput(status.isundefined,    "x < 57"),
                              
                              new TestInput(status.isundefined,    "57 = x"),
                              new TestInput(status.isundefined,    "x = x"),
                              new TestInput(status.isundefined,    "x = 57"),
                              
                              new TestInput(status.isundefined,    "57 != x"),
                              new TestInput(status.isundefined,    "x != x"),
                              new TestInput(status.isundefined,    "x != 57"),

                              new TestInput(status.isundefined,    "!x"),

                              new TestInput(status.isfalse,        "0 && 1"),

                              new TestInput(status.invalidexpression,  "ABCD***"),
                              new TestInput(status.noexpression,  "")
                             };

            LogicExpressionEvaluator_Accessor target = new LogicExpressionEvaluator_Accessor();
            for (int i = 0; i < ti.Length; i++)
            {
                try
                {
                    status retStatus = LogicExpressionEvaluator_Accessor.Evaluate(ti[i].e);
                    Assert.AreEqual(ti[i].expected, retStatus, ti[i].e);

                    if (!ti[i].e.Contains("x") && (ti[i].expected == status.istrue || ti[i].expected == status.isfalse) )
                    {
                        string response = (LogicExpressionEvaluatorTest.EvalJScript(ti[i].e)).ToString();
                        status jscript_status = status.isfalse;
                        if (response.ToLower() == "true")
                            jscript_status = status.istrue;

                        // Since the JavaScript engine can return things other than true/false, we'll only verify true/false responses
                        if ( (response.ToLower() == "true") || (response.ToLower() == "false") )
                            Assert.AreEqual(ti[i].expected, jscript_status, "JScript evaluation was \"" + response + "\" for expression \"" + ti[i].e + "\"");
                    }
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    throw;
                }
            }
        }

#pragma warning disable
        public static Microsoft.JScript.Vsa.VsaEngine Engine = Microsoft.JScript.Vsa.VsaEngine.CreateEngine();
#pragma warning restore
        public static object EvalJScript(string JScript)
        {
            object Result = null;
            try
            {
                Result = Microsoft.JScript.Eval.JScriptEvaluate(JScript, Engine);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return Result;
        }

        /// <summa]ry>
        ///A test for Evaluate
        ///</summary>
        [TestMethod()]
        [DeploymentItem("LogicParser.dll")]
        public void PerformanceTest()
        {
            const int MAXLOOPS = 4000;
            TestInput[] ti = {
                              new TestInput(status.istrue,         "1 && (0 || 1) && 5 > 2"),
                              new TestInput(status.isfalse,        "1 && (!0 || 1) && 2 > 2")
                             };

            LogicExpressionEvaluator_Accessor target = new LogicExpressionEvaluator_Accessor();
            int startms = System.Environment.TickCount;
            for (int i = 0; i < ti.Length; i++)
            {
                status retStatus = status.incomplete;
                for (int loop = 0; loop < MAXLOOPS; loop++)
                    retStatus = LogicExpressionEvaluator_Accessor.Evaluate(ti[i].e);

                Assert.AreEqual(ti[i].expected, retStatus, ti[i].e);
            }
            int endms = System.Environment.TickCount;
            int elapsedms_evaluate = endms - startms;

            startms = System.Environment.TickCount;
            for (int i = 0; i < ti.Length; i++)
            {
                for (int loop = 0; loop < MAXLOOPS; loop++)
                    LogicExpressionEvaluator_Accessor.ValidateExpression(ti[i].e);
            }
            endms = System.Environment.TickCount;
            int elapsedms_validate = endms - startms;
            int evaluations = MAXLOOPS * ti.Length;
            int elapsedmicroseconds = elapsedms_evaluate * 1000;
            int microsecondsperevaluation = elapsedmicroseconds / evaluations;

            // How fast is the regular expression in .NET ?
            int js_startms = System.Environment.TickCount;
            object response = new object();
            for (int i = 0; i < ti.Length; i++)
            {
                for (int loop = 0; loop < MAXLOOPS; loop++)
                {
                    response =  LogicExpressionEvaluatorTest.EvalJScript(ti[i].e);
                }
            }
            int js_endms = System.Environment.TickCount;
            int js_elapsedms_evaluate = js_endms - js_startms;
            int js_elapsedmicroseconds = js_elapsedms_evaluate * 1000;
            int js_microsecondsperevaluation = js_elapsedmicroseconds / evaluations;

            string msg = string.Format("{0} ms elapsed, {1} ms for validate ({2} expressions evaluated) {3} microseconds/eval [JS {4} microseconds/eval]", elapsedms_evaluate, elapsedms_validate, evaluations, microsecondsperevaluation, js_microsecondsperevaluation);

            // we want to be at least 50% faster than the JavaScript engine (with evaluation no less!)
            int targetmicrosecondsperevaluation = microsecondsperevaluation / 2 + microsecondsperevaluation;
            Assert.IsTrue((js_microsecondsperevaluation > targetmicrosecondsperevaluation), msg);
        }

        [TestMethod()]
        [DeploymentItem("LogicParser.dll")]
        public void ExtractElementTest()
        {
            Element_Accessor e = new Element_Accessor();

            string expression = "[(1 && 37) || (4 > 5) || x && 1] && (true || false) && 1 || true";
            int i = 0;
            int previous_index = 0;
            int length = expression.Length;
            while (i < length)
            {
                previous_index = i;
                i = e.ExtractElement(expression, previous_index);
                Assert.IsFalse(i < 0);
                if (i == previous_index)
                    break;
            }
        }
        [TestMethod()]
        [DeploymentItem("LogicParser.dll")]
        public void EvaluateElementTest()
        {
            TestInput[] ti = {
                              // AND tests
                              new TestInput(status.istrue,         "1 && 1"),
                              new TestInput(status.isfalse,        "1 && 0"),
                              new TestInput(status.isfalse,        "0 && 1"),
                              new TestInput(status.isfalse,        "0 && x"),
                              new TestInput(status.isfalse,        "x && 0"),
                              new TestInput(status.isfalse,        "0 && 0"),
                              new TestInput(status.isundefined,    "x && 1"),
                              new TestInput(status.isundefined,    "1 && x"),
                              new TestInput(status.isundefined,    "x && x"),

                              new TestInput(status.istrue,         "true && true"),
                              new TestInput(status.isfalse,        "true && false"),
                              new TestInput(status.isfalse,        "false && true"),
                              new TestInput(status.isfalse,        "false && x"),
                              new TestInput(status.isfalse,        "x && false"),
                              new TestInput(status.isfalse,        "false && false"),
                              new TestInput(status.isundefined,    "x && true"),
                              new TestInput(status.isundefined,    "true && x"),
                              new TestInput(status.isundefined,    "x && x"),

                              // OR tests
                              new TestInput(status.istrue,         "1 || 1"),
                              new TestInput(status.istrue,         "1 || 0"),
                              new TestInput(status.istrue,         "0 || 1"),
                              new TestInput(status.isundefined,    "0 || x"),
                              new TestInput(status.isundefined,    "x || 0"),
                              new TestInput(status.isfalse,        "0 || 0"),
                              new TestInput(status.istrue,         "x || 1"),
                              new TestInput(status.istrue,         "1 || x"),
                              new TestInput(status.isundefined,    "x || x"),

                              new TestInput(status.istrue,         "true || true"),
                              new TestInput(status.istrue,         "true || false"),
                              new TestInput(status.istrue,         "false || true"),
                              new TestInput(status.isundefined,    "false || x"),
                              new TestInput(status.isundefined,    "x || false"),
                              new TestInput(status.isfalse,        "false || false"),
                              new TestInput(status.istrue,         "x || true"),
                              new TestInput(status.istrue,         "true || x"),
                              new TestInput(status.isundefined,    "x || x")
                            };
           
            // Now iterate through our test data
            for (int idx = 0; idx < ti.Length; idx++)
            {

                Element_Accessor v1 = new Element_Accessor();
                string e = ti[idx].e;
                int i = 0;
                i = v1.ExtractElement(e, i);
                Element_Accessor op = new Element_Accessor();
                i = op.ExtractElement(e, i);
                Element_Accessor v2 = new Element_Accessor();
                i = v2.ExtractElement(e, i);

                status retval = LogicExpressionEvaluator_Accessor.Evaluate(v1, op, v2);
                Assert.IsTrue( (retval == ti[idx].expected), ti[idx].e + " failed!");
            }

        }// end of test

        /// <summary>
        ///A test for MakeTriState
        ///</summary>
        [TestMethod()]
        [DeploymentItem("LogicParser.dll")]
        public void MakeTriStateTest()
        {
            TestInput[] ti = {
                              new TestInput(status.incomplete,         "1 && 0")
                             };
            for (int idx = 0; idx < ti.Length; idx++)
            {
                Element_Accessor v1 = new Element_Accessor();
                string e = ti[idx].e;
                int i = 0;
                i = v1.ExtractElement(e, i);
                Element_Accessor op = new Element_Accessor();
                i = op.ExtractElement(e, i);
                Element_Accessor v2 = new Element_Accessor();
                i = v2.ExtractElement(e, i);

                Elements_Accessor list = new Elements_Accessor();
                list.Add(v1);
                list.Add(op);
                list.Add(v2);

                status retval = LogicExpressionEvaluator_Accessor.MakeTriState(list);
                Assert.IsTrue((retval == ti[idx].expected), ti[idx].e + " failed!");

                retval = LogicExpressionEvaluator_Accessor.MakeTriState(list);
                Assert.IsTrue((retval == status.incomplete),  "MakeTriState(operand) failed!");

                string se = v1.elementValue + op.elementValue + v2.elementValue;
                Assert.IsTrue(list.ToString() == se, "error Element.ToString() for [" + e + "], \"" + se + "\" doesn't equal \"" + list.ToString() + "\"");
            }
        }
    }
}
