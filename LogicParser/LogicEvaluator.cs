using System;
using System.Collections;
using System.Text;
using System.Globalization;

namespace LogicParser
{

    /// <summary>
    /// We have two enums, one for "stateValues", the tri-state logic states,
    /// the other for 'status' which includes these state values in addition to other states, such as errors
    /// and other status.
    /// </summary>
    public enum stateValues { istrue = 1, isfalse = 0, isundefined = -1}; // must be convertable directly to string!
    public enum status      { incomplete = -1, noexpression = -2, invalidexpression = -3, success = 0, isundefined = 1, istrue = 2, isfalse = 3, notOperator = -5, notValues = -6 };

    /// <summary>
    /// LogicExpressionEvaluator()
    /// Though most of the methods in this class (okay, all) are static
    /// the class is re-entrant and can be safely used with multiple threads
    /// (I hope!).
    ///
    /// 
    /// This project will parse complex logic expressions with the following properties:
    ///
    /// 1. Arbitrary nesting of precedence operators ()[]
    /// 2. Boolean expressions, AND, OR, XOR, NOT
    /// 3. Comparison operations: <, >, <>, =
    ///
    /// Precedence will be:
    /// - ()[]
    /// - <, >, <>, =
    /// - AND, OR, XOR, NOT
    /// An expression will look something like:
    ///
    /// 1 && [(0 || 1) && (? || 1) || (37 > 8)]
    /// </summary>
    public class LogicExpressionEvaluator
    {
        /// <summary>
        /// This method will determine if the expression
        /// is valid and warrants processing.
        /// </summary>
        /// <param name="Expression">the logic expression</param>
        /// <returns>=false, expression is invalid</returns>
        static private status ValidateExpression(string Expression)
        {
            if (null == Expression)
                return status.noexpression; // unknown, no expression supplied
            if (Expression.Length == 0)
                return status.noexpression; // still not much use

            int parenthesis = 0;
            int brackets    = 0;    // brackets have higher precedence, treat separately
            for (int i = 0; i < Expression.Length; i++)
            {
                string firstchar = Expression[i].ToString();
                if (Expression[i] == ' ') // spaces are ignored
                    continue;
                if (Element.precedenceType.Contains(firstchar))
                {
                    switch (Expression[i])
                    {
                        case '(': parenthesis++;    break;
                        case '[': brackets++;       break;
                        case ')': 
                            parenthesis--;
                            if (parenthesis < 0) 
                                return status.invalidexpression; 
                            break;
                        case ']': 
                            brackets--;
                            if (brackets < 0)
                                return status.invalidexpression;
                            break;
                    }
                    continue;
                }
                if (Element.valueType.Contains(firstchar))
                    continue;
                if (Element.operationType.Contains(firstchar))
                    continue;
                if (Element.unknownType.Contains(firstchar))
                    continue;

                if (Expression[i] == Element.trueType[0]) // start of the word 'true' ?
                {
                    string sTrue = Expression.Substring(i, Element.trueType.Length).ToLower(new CultureInfo("en-US", false));
                    if (sTrue == Element.trueType)
                    {
                        i += Element.trueType.Length;
                        continue;
                    }
                } else if (Expression[i] == Element.falseType[0]) // start of word 'false'
                {
                    string sFalse = Expression.Substring(i, Element.falseType.Length).ToLower(new CultureInfo("en-US", false));
                    if (sFalse == Element.falseType)
                    {
                        i += Element.falseType.Length;
                        continue;
                    }
                }

                return status.invalidexpression; // we found characters we don't support!
            }

            return ( ( ((parenthesis == 0) && (brackets == 0) ) ? status.success : status.invalidexpression) );
        }

        /// <summary>
        /// This method will parse the string expression into a list of "Elements"
        /// </summary>
        /// <param name="list">list of parsed values</param>
        /// <param name="Expression">the input expression that needs to be parsed</param>
        /// <returns></returns>
        static status ParseToList(Elements list, string Expression)
        {
            int i = 0; // starting position is first byte
            int l = Expression.Length;
            while (l > i)
            {
                Element e = new Element();
                i = e.ExtractElement(Expression, i);
                if (i <= 0) // There's been a problem
                    return status.invalidexpression;

                list.Add(e);
            }

            return status.success;
        }

        //evalstatus MakeEvalStatus(status st)
        //{
        //    switch (st)
        //    {
        //        case status.incomplete:         return evalstatus.incomplete;
        //        case status.invalidexpression:  return evalstatus.invalidexpression;
        //        case status.isfalse:            return evalstatus.isfalse;
        //        case status.istrue:             return evalstatus.istrue;
        //        case status.isundefined:        return evalstatus.isundefined;
        //        case status.noexpression:       return evalstatus.noexpression;
        //        case status.notOperator:        return evalstatus.notOperator;
        //        case status.notValues:          return evalstatus.notValues;
        //    }
        //    return evalstatus.success;
        //}

        /// <summary>
        /// This is where the expression is evaluated.
        /// </summary>
        /// <param name="Expression">A logical expression</param>
        /// <returns>status.noexpression - missing expression, string empty
        /// status.invalidexpression - expression failed basic parsing test
        /// status.istrue - result of evaluation is "true"
        /// status.isfalse - result of evaluation is "false"
        /// status.isundefined - result of evaluation is "unknown"</returns>
        static public status Evaluate(string Expression)
        {
            // Okay we have an expression, first check to see if it's valid
            status vstat = ValidateExpression(Expression);
            if (vstat != status.success)    return vstat; // expression not valid

            // Okay we have a "valid" logic expression so it's time to parse it
            Elements list = new Elements();
            status parse_results = ParseToList(list, Expression);
            if (status.success != parse_results) return parse_results; // error parsing, return with problem

            status result = ProcessListWithPrecedence(list); // okay process the list
            return result;
        }

        /// <summary>
        /// This procedure will really process the list down to a single value, regardless
        /// of how complex it is, as long as there's no "precedence" (parenthesis & brackets)
        /// Which was taken care of by the caller :)
        /// </summary>
        /// <param name="items">list of "Elements" representing the parsed expression</param>
        /// <param name="startpos">start of the "sub-list" we should process</param>
        /// <param name="endpos">ending point of the sub-list we should process (actual end +1)</param>
        /// <returns></returns>
        static private status ProcessList(Elements items, int startpos, int endpos)
        {
            status val = status.incomplete;
            do
            {
                // Okay we have found a string that need to process...
                val = ProcessElements(items, startpos, ref endpos);
                if (val != status.success)
                    return val; // get out of here there's been a problem!
            } while ( (endpos - startpos) > 1);

            return val; // status.incomplete;
        }

        /// <summary>
        /// Given a list of parsed elements, this method will
        /// process them down to a tri-state result (1/0/x) or
        /// return error status. Here we only find the "sub-lists"
        /// within parenthesis/brackets (precedence operators)
        /// </summary>
        /// <param name="items">list of parsed elements</param>
        /// <returns>"status" 1/0/x or an error</returns>
        private static status ProcessListWithPrecedence(Elements items)
        {
            // Okay we have to find the precedence operators
            int itemCount = items.Count; // this is a "safety valve" to prevent a hang by looping
            while(itemCount > 1)
            {
                int start_pos = -1;
                bool foundPrecedence = false;
                for (int i = 0; i < itemCount; i++)
                {
                    if (items[i].IsPrecedenceStart())
                    {
                        start_pos = i;
                        foundPrecedence = true;
                        continue;
                    }

                    if (items[i].IsPrecedenceEnd() && start_pos != -1)
                    {
                        // We have the following:
                        //
                        //   Relative     val      index
                        //   ---------------------------
                        //   [startpos] = "("    - 2
                        //   [+1]       = "0"    - 3
                        //   [+2]       = "&&"   - 4
                        //   [+3]       = "1"    - 5
                        //   [+4]       = ")"    - 6
                        //
                        // When we are done removing the precedence we should have:
                        //
                        //   Relative     val      index
                        //   ---------------------------
                        //   [startpos] = "0"    - 2
                        //   [+1]       = "&&"   - 3
                        //   [+2]       = "1"    - 4
                        //   [+3]       = .....  - 5
                        //
                        // Since our "endpos" is the actual end + 1, we need to adjust the 
                        // ending point (which is the index) since we are removing 2 elements

                        // get rid of ()[] pairs
                        try
                        {
                            items.RemoveAt(i);
                            items.RemoveAt(start_pos);
                        }
                        catch (System.ArgumentOutOfRangeException ex)
                        {
                            string msg = ex.Message;
                            throw ex;
                        }

                        // process all the elements
                        status val = ProcessList(items, start_pos, i - 1);
                        if (val != status.success)
                            return val; // there has been an error processing

                        // since the list has been messed with we need to exit...
                        break;
                    }// end of if

                }// end of for

                // if we didn't find any precedence operators, process the list we have
                if (!foundPrecedence)
                {
                    status val = ProcessList(items, 0, items.Count);
                    if (val != status.success)
                        return val; // there has been an error processing
                }

                // This is just a safety relief in case we get stuck in a loop
                // (if we are properly processing the items[] it will get shorter)
                if (items.Count == itemCount)
                    return status.incomplete; // maybe we need a better error message?
                itemCount = items.Count;
            }// end of while

            // at this point items[] should have 1 value, make it a tri-state for the return
            return MakeTriState(items);
        }// end of ProcessList

        /// <summary>
        /// This method will create a tristate value (1/0/x)
        /// from the single element passed in. If more than 
        /// a single element is passed in, well that's really
        /// bad and an error is generated.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private static status MakeTriState(Elements items)
        {
            if (items.Count == 1)
            {
                if (items[0].IsUnknown())
                    return status.isundefined;

                if (items[0].IsValue()) // we have a value!
                {
                    switch (items[0].Value)
                    {
                        case stateValues.isfalse:
                            return status.isfalse;
                        case stateValues.istrue:
                            return status.istrue;
                    }
                }
            }

            return status.incomplete;
        }

        /// <summary>
        /// For cases when we have exactly 3 items on the list, we know it's
        /// "operand-operator-operand" and we don't have to incur all the overhead
        /// of checking, so we'll just evaluate it.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="startpos"></param>
        /// <param name="endpos"></param>
        /// <returns></returns>
        private static status ProcessElements3(Elements items, int startpos, ref int endpos)
        {
            status retval = Evaluate(items[startpos-1], items[startpos], items[startpos + 1]);
            if (retval != status.isfalse && retval != status.istrue && retval != status.isundefined)
                return retval; // this is a problem

            // delete all the elements in the list that we just evaluated
            try
            {
                for (int j = startpos - 1; j <= startpos + 1; j++)
                    items.RemoveAt(startpos - 1);
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                string msg = ex.Message;
                throw ex;
            }

            Element e = new Element(Element.eType.value, StatusToStateValue(retval));
            items.InsertAt(startpos - 1, e);
            endpos -= 2;

            return status.success;
        }

        /// <summary>
        /// handles our "single operand" operators, such as the ! operator
        /// and any others we define later.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="startpos"></param>
        /// <param name="endpos"></param>
        /// <returns></returns>
        private static status ProcessSingleOperation(Elements items, int startpos, ref int endpos)
        {
            for (int i = startpos; i < endpos; i++)
            {
                if (items[i].Operation == Element.opType.Not) // found one!
                {
                    status nstat = Evaluate(items[i + 1], items[i]);
                    if (nstat != status.isfalse && nstat != status.istrue && nstat != status.isundefined)
                        return nstat; // this is a problem`

                    try
                    {
                        items.RemoveAt(i);
                        items.RemoveAt(i);
                    }
                    catch (System.ArgumentOutOfRangeException ex)
                    {
                        string msg = ex.Message;
                        throw ex;
                    }
                    // replace the elements we stripped out with the evaluated value
                    stateValues state = stateValues.isundefined;
                    if (nstat == status.isfalse)
                        state = stateValues.isfalse;
                    else if (nstat == status.istrue)
                        state = stateValues.istrue;
                    Element e = new Element(Element.eType.value, state);
                    items.InsertAt(i, e);
                    endpos--;
                }
            }

            return status.success;
        }

        /// <summary>
        /// Given a list of "elements" that consists of an arbitrary number of
        /// operands & operations, evaluate down to a single "element" representing
        /// the state of the operation.
        /// </summary>
        /// <param name="items">our parsed list of Elements</param>
        /// <param name="startpos">within this list the starting offset/index</param>
        /// <param name="endpos">within the list the ending offset/index (non-inclusive)</param>
        /// <returns></returns>
        private static status ProcessElements(Elements items, int startpos, ref int endpos)
        {
            // Okay we have a list of elements
            int items_remaining = (endpos - startpos);
            if (items_remaining == 1)
            {
                    if (items[startpos].IsValue())     // if it's a value, then it's good
                        return status.success;
                    if (!items[startpos].IsSingleOperandOperator())
                        return status.invalidexpression;
            }

            if (items_remaining == 3)
                return ProcessElements3(items, startpos+1, ref endpos);

            // say we have the following:
            //  1 && 0 || 1 || x && true
            // should be evaluated as:
            //  (1 && 0) || 1 || (x && true)

            // First process all the single operand operators
            status singleop = ProcessSingleOperation(items, startpos, ref endpos);
            if (status.success != singleop)
                return singleop;

            // At this point we have a list of operands & operators with no parenthesis/brackets (precedence) operators
            // So let's consume it all
            while ( (endpos - startpos) > 2)
            {
                // process the operations in the "order of operations" order
                int to_process = OperationOrder(items, startpos, endpos);
                if (0 == to_process)
                    return status.incomplete; // this shouldn't happen...

                status retval = ProcessElements3(items, to_process, ref endpos);
                if (retval != status.success)
                    return retval;
            }

            return status.success;
        }

        /// <summary>
        /// This method will determine from a list of parsed elements
        /// which should be processed first based on the "order-of-operations"
        /// which weights each operator with a significance and states that in the
        /// case of a "tie" process left-to-right.
        /// 
        /// We can be processing a "sub-section" of the list demarked by the start/end
        /// positions.
        /// </summary>
        /// <param name="items">list of parsed elements, NO PRECENDENCE (parenthesis/brackets) or SINGLE OPERAND-OPERATORS</param>
        /// <param name="startpos">position in element list to start</param>
        /// <param name="endpos">position in element list to end</param>
        /// <returns></returns>
        private static int OperationOrder(Elements items, int startpos, int endpos)
        {
            int to_process = 0;
            int highest_OrderOfOperations = 0;
            for (int i = startpos+1; i < endpos; i+=2)
            {
                if (items[i].IsOperator())
                {
                    if (items[i].OrderOfOperations() > highest_OrderOfOperations)
                    {
                        highest_OrderOfOperations = items[i].OrderOfOperations();
                        to_process = i;
                    }

                }
                else
                    return 0; // this is an error
            }
            return to_process;
        }

        /// <summary>
        /// Convert "status" enums to stateValues. Since status is a "superset"
        /// we can end up with "undefineds" a lot.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static stateValues StatusToStateValue(status val)
        {
            stateValues state = stateValues.isundefined;
            if (val == status.isfalse)
                state = stateValues.isfalse;
            else if (val == status.istrue)
                state = stateValues.istrue;
            return state;
        }

        /// <summary>
        /// Evaluate the > operator for any 2 operands.
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private static status EvaluateGreaterThan(Element operand1, Element operand2)
        {
            if (operand1.IsUnknown() || operand2.IsUnknown())
                return status.isundefined;
            if (!operand1.IsValue() || !operand2.IsValue())
                return status.notValues;
            if (operand1.IsDouble() || operand2.IsDouble())
            {
                if (operand1.ToDouble() > operand2.ToDouble())
                    return status.istrue;
            }
            else
                if (operand1.ToInt() > operand2.ToInt())
                    return status.istrue;
            return status.isfalse;
        }

        /// <summary>
        /// Evaluate the < operator for any two operands
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private static status EvaluateLessThan(Element operand1, Element operand2)
        {
            if (operand1.IsUnknown() || operand2.IsUnknown())
                return status.isundefined;
            if (!operand1.IsValue() || !operand2.IsValue())
                return status.notValues;
            if (operand1.IsDouble() || operand2.IsDouble())
            {
                if (operand1.ToDouble() < operand2.ToDouble())
                    return status.istrue;
            }
            else
                if (operand1.ToInt() < operand2.ToInt())
                    return status.istrue;
            return status.isfalse;
        }

        /// <summary>
        /// evaluate the = operator for any 2 operands
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private static status EvaluateEqual(Element operand1, Element operand2)
        {
            if (operand1.IsUnknown() || operand2.IsUnknown())
                return status.isundefined;
            if (!operand1.IsValue() || !operand2.IsValue())
                return status.notValues;

            if (operand1.IsDouble() || operand2.IsDouble())
            {
                if (operand1.ToDouble() == operand2.ToDouble())
                    return status.istrue;
            }
            else
                if (operand1.ToInt() == operand2.ToInt())
                    return status.istrue;
            return status.isfalse;
        }

        /// <summary>
        /// Evaluate the not equal to operator for any 2 operands
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private static status EvaluateNotEqual(Element operand1, Element operand2)
        {
            if (operand1.IsUnknown() || operand2.IsUnknown())
                return status.isundefined;
            if (!operand1.IsValue() || !operand2.IsValue())
                return status.notValues;
            if (operand1.IsDouble() || operand2.IsDouble())
            {
                if (operand1.ToDouble() != operand2.ToDouble())
                    return status.istrue;
            }
            else
                if (operand1.ToInt() != operand2.ToInt())
                    return status.istrue;
            return status.isfalse;
        }

        /// <summary>
        /// Evaluate the "not" operator (logic) for any two operands
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        private static status EvaluateNot(Element operand)
        {
            if (operand.IsUnknown())
                return status.isundefined;
            if (!operand.IsValue())
                return status.notValues;
            if (operand.IsDouble())
            {
                if (operand.ToDouble() == 0.0)
                    return status.istrue;
            }
            else
                if (operand.ToInt() == 0)
                    return status.istrue;
            return status.isfalse;
        }

        /// <summary>
        /// evaluate the logical AND operator for any two operands
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private static status EvaluateLogicalAND(Element operand1, Element operand2)
        {
            // Okay we are ANDing...

            // 0 & 1 / 0 & 0 / 1 & 0 / 0 & x / x & 0
            if ((operand1.IsValue() && operand1.ToInt() == 0) ||
                (operand2.IsValue() && operand2.ToInt() == 0))
                return status.isfalse;

            // 1 & 1
            if ((operand1.IsValue() && operand1.ToInt() == 1) &&
                (operand2.IsValue() && operand2.ToInt() == 1))
                return status.istrue;

            // x & 1 / 1 & x / x & x
            return status.isundefined;

        }

        private static status EvaluateBitWiseAND(Element operand1, Element operand2)
        {
            return status.incomplete;
        }

        private static status EvaluateBitWiseOR(Element operand1, Element operand2)
        {
            return status.incomplete;
        }

        /// <summary>
        /// This method evaluates a set of operands for the Logical OR condition
        /// </summary>
        /// <param name="operand1"></param>
        /// <param name="operand2"></param>
        /// <returns></returns>
        private static status EvaluateLogicalOR(Element operand1, Element operand2)
        {

            // 1 | 1 / 1 | 0 / 0 | 1 / 1 | x / x | 1
            if ((operand1.IsValue() && operand1.ToInt() == 1) ||
                 (operand2.IsValue() && operand2.ToInt() == 1))
                return status.istrue;

            // 0 | 0
            if ((operand1.IsValue() && operand1.ToInt() == 0) &&
                 (operand2.IsValue() && operand2.ToInt() == 0))
                return status.isfalse;

            return status.isundefined;

        }

        /// <summary>
        /// Evaluate single operand operators
        /// </summary>
        /// <param name="operand"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        private static status Evaluate(Element operand, Element operation)
        {
            if (!operation.IsOperator())
                return status.notOperator;

            if ((!operand.IsValue() && !operand.IsUnknown()) )
                return status.notValues; // or "unknowns"

            if (operation.Operation == Element.opType.Not)
                return EvaluateNot(operand);

            return status.incomplete;
        }

        /// <summary>
        /// This method will evaluate to a tri-state result (1/0/x)
        /// two values and their operator.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="operation"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        private static status Evaluate(Element value1, Element operation, Element value2)
        {
            // Do some argument checking!
            if (!operation.IsOperator())
                return status.notOperator;
            if ( (!value1.IsValue() && !value1.IsUnknown() ) || (!value2.IsValue() && !value2.IsUnknown() ) )
                return status.notValues; // or "unknowns"

            switch (operation.Operation)
            {
                case Element.opType.LogicalAND:     return EvaluateLogicalAND(value1, value2);
                case Element.opType.BitAND:         return EvaluateBitWiseAND(value1, value2);
                case Element.opType.LogicalOR:      return EvaluateLogicalOR(value1,  value2);
                case Element.opType.BitOR:          return EvaluateBitWiseOR(value1,  value2);
                case Element.opType.GreaterThan:    return EvaluateGreaterThan(value1, value2);
                case Element.opType.LessThan:       return EvaluateLessThan(value1, value2);
                case Element.opType.Equal:          return EvaluateEqual(value1, value2);
                case Element.opType.NotEqual:       return EvaluateNotEqual(value1, value2);
            }

            return status.incomplete;
        }

    }// end of class

    /// <summary>
    /// An "Element" is a logic unit parsed from a logic expression.
    /// An Element can one of 4 follow types:
    ///    1. precedence operator : []()
    ///    2. Operation operator  : <, >, <>, >=, <=, &&, ||
    ///    3. value               : numeric
    ///    4. unknown             : ?, x
    /// </summary>
    class Element
    {

        /// <summary>
        /// We can initialize an element to 3 states:
        ///    0 / 1 / x
        /// </summary>
        /// <param name="type">"type" of value written, invariably "value"</param>
        /// <param name="state">the state of the value, 1/0/x</param>
        public Element(eType type, stateValues state)
        {
            this.elementType = type;
            switch (state)
            {
                case stateValues.istrue:
                    this.elementValue = "1";
                    break;
                case stateValues.isfalse:
                    this.elementValue = "0";
                    break;
                default:
                    this.elementType = eType.unknown;
                    this.elementValue = "x"; // undefined
                    break;
            }
        }

        /// <summary>
        /// constructor, initialize the element so we can parse into it.
        /// </summary>
        public Element()
        {
            this.Init();
        }

        /// <summary>
        /// Classify our "Element Types" (eType)
        /// </summary>
        public enum eType {undefined = 0, precedence = 1, operation = 2, value = 3, unknown = 4, boolean = 5};

        /// <summary>
        /// Classify our "Operator types (opType)
        /// </summary>
        public enum opType { undefined = -1, Unknown = 0, LogicalAND = 1, LogicalOR = 2, BitAND = 3, BitOR = 4, Equal = 5, GreaterThan = 6, LessThan = 7, NotEqual = 8, Not = 9};

        /// <summary>
        /// This structure contains our "order of operations" so we can define
        /// the order of operations easily.
        /// </summary>
        struct OrderOp 
        { 
            public opType operation;
            public int order;

            public OrderOp(opType ot, int o)
            {
                operation = ot;
                order = o;
            }
        };

        #region static variables
        // larger the value, the more important the operation
        static readonly OrderOp []OrderOperations = { new OrderOp(opType.LogicalAND, 2),
                                    new OrderOp(opType.BitAND,      2),
                                    new OrderOp(opType.LogicalOR,   1),
                                    new OrderOp(opType.BitOR,       1),
                                    new OrderOp(opType.GreaterThan, 3),
                                    new OrderOp(opType.LessThan,    3),
                                    new OrderOp(opType.Equal,       3),
                                    new OrderOp(opType.NotEqual,    3),
                                    new OrderOp(opType.Not,         4)
                                  };

        opType operation = opType.undefined;

        // These strings represent our allowed values for various expression element types
        public static readonly string precedenceType    = "[]()";
        public static readonly string operationType     = "<>=&|!";
        public static readonly string valueType         = "0123456789-."; // handle negatives & decimal!!
        public static readonly string unknownType       = "?x";
        public readonly static string trueType          = "true";
        public readonly static string falseType         = "false";
        #endregion

        #region member variables
        private eType  elementType  = eType.undefined;
        private string elementValue;          // empty string until defined...
        private bool   precedenceStart;       // will be valid if elementType is "precedence"
        #endregion

        /// <summary>
        /// convert the value of this element to a three-state value
        /// </summary>
        public stateValues Value
        {
            get
            {
                if (IsUnknown())
                    return stateValues.isundefined;
                if (elementType != eType.value)
                    return stateValues.isundefined;
                if (IsDouble()) // doubles are special!
                {
                    if (0.0 == this.ToDouble())
                        return stateValues.isfalse;
                    else
                        return stateValues.istrue;
                }
                if (0 == this.ToInt())
                    return stateValues.isfalse;
                if (0 != this.ToInt())
                    return stateValues.istrue;

                return stateValues.isundefined;
            }
        }

        /// <summary>
        /// Convert the Element value to an integer...
        /// </summary>
        /// <returns></returns>
        public int ToInt()
        {
            if ( (elementType != eType.value) && (elementType != eType.boolean) )
                return 0;
            return Convert.ToInt32(elementValue);
        }

        private int order_of_operations = 0;
        /// <summary>
        /// we'll only look up the order of operations for this operator
        /// once, then we'll save the result...
        /// </summary>
        /// <returns>0=there was a problem</returns>
        public int OrderOfOperations()
        {
            if (0 == order_of_operations)
            {
                for (int i = 0; i < OrderOperations.Length; i++)
                {
                    if (this.Operation == OrderOperations[i].operation)
                    {
                        order_of_operations = OrderOperations[i].order;
                        break;
                    }
                }
            }

            return order_of_operations; // This is an error!!!
        }

        /// <summary>
        /// indicates that the operation is a "single operand" type
        /// </summary>
        /// <returns></returns>
        public Boolean IsSingleOperandOperator()
        {
            return ( (this.elementType == eType.operation) && (this.Operation == Element.opType.Not) );
        }
        /// <summary>
        /// Indicates the element is the start of a precedence operation
        /// </summary>
        /// <returns></returns>
        public Boolean IsPrecedenceStart()
        {
            return precedenceStart;
        }

        public Boolean IsPrecedenceEnd()
        {
            return (this.elementType == eType.precedence && !IsPrecedenceStart() );
        }

        public Boolean IsUnknown()
        {
            return elementType == eType.unknown;
        }
         /// <summary>
        /// We have two types of "values", one is a digit we parsed,
        /// the other is a true/false that we synthesized into a value.
        /// keeping the "elementType" around for debugging mostly
        /// </summary>
        /// <returns>=true, the element is a value</returns>
        public Boolean IsValue()
        {
            return (elementType == eType.boolean || elementType == eType.value);
        }

        public Boolean IsDouble()
        {
            // Okay only true "values" can be a double
            return this.elementValue.Contains(".") && (elementType == eType.value);
        }

        public double ToDouble()
        {
            return Convert.ToDouble(this.elementValue);
        }

        public Boolean IsOperator()
        {
            return (elementType == eType.operation);
        }

        struct opTypeMapping
        {
            public opType ot;  // operation type
            public string operation;   // the actual operations

            public opTypeMapping(opType o, string op)
            {
                this.ot = o; this.operation = op;
            }
        }

        // Note: organize array in frequency of usage for optimal performance.
        opTypeMapping[] operationMapping = {
                                            new opTypeMapping(opType.LogicalAND,     "&&"),
                                            new opTypeMapping(opType.LogicalOR,      "||"),
                                            new opTypeMapping(opType.GreaterThan,    ">"),
                                            new opTypeMapping(opType.LessThan,       "<"),
                                            new opTypeMapping(opType.Equal,          "="),
                                            new opTypeMapping(opType.NotEqual,       "!="),
                                            new opTypeMapping(opType.Not,            "!"),
                                            new opTypeMapping(opType.BitAND,         "&"),
                                            new opTypeMapping(opType.BitOR,          "|")
                                            };

        private opType GetOpType()
        {
            for (int i = 0; i < operationMapping.Length; i++)
            {
                if (elementValue == operationMapping[i].operation)
                    return operationMapping[i].ot;
            }
                return opType.Unknown;
        }

        public opType Operation
        {
            get
            {
                if (this.operation == opType.undefined)
                    operation = GetOpType();
                return operation;
            }
        }

        public string ElementValue
        {
            get
            {
                return this.elementValue;
            }
        }

        private void Init()
        {
            this.elementType        = eType.undefined;
            this.precedenceStart    = false;
            this.elementValue       = "";
        }

        static readonly string DoubleCharacterOperators = "<>>=<=!=&&||";
        private Boolean IsDoubleCharacterOperator(string expression, int inPos)
        {
            string dC = expression.Substring(inPos, 2);
            if (DoubleCharacterOperators.Contains(dC))
            {
                this.elementValue = expression.Substring(inPos, 2);
                return true;
            }

            return false;
        }

        static readonly string SingleCharacterOperators = "<>!&|=";
        private Boolean IsSingleCharacterOperator(string expression, int inPos)
        {
            if (SingleCharacterOperators.Contains(expression[inPos].ToString()))
            {
                elementValue = expression.Substring(inPos, 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This method will extract the value from the current expression 
        /// based on the starting offset.
        /// </summary>
        /// <param name="expression">the entire expression</param>
        /// <param name="inPos">start of the value string</param>
        /// <returns></returns>
        private int ParseOutValue(string expression, int inPos)
        {
            int endvaluepos;
            for (endvaluepos = inPos; endvaluepos < expression.Length; endvaluepos++)
            {
                if (!Element.valueType.Contains(expression[endvaluepos].ToString()))
                    break;
            }
            elementValue = expression.Substring(inPos, (endvaluepos - inPos) );
            return endvaluepos;
        }

        private int ParsePrecedence(string expression, int inPos)
        {
            this.elementValue = expression.Substring(inPos, 1);
            this.precedenceStart = (elementValue == "(") || (elementValue == "[");
            return inPos + 1; // single character always!
        }

        /// <summary>
        /// Once we have identified a parsed item we want to extract it
        /// for future reference.
        /// </summary>
        /// <param name="elementType">what element type we know this to be</param>
        /// <param name="expression">our input string</param>
        /// <param name="inPos">position of start of the expression element we can extract</param>
        /// <returns></returns>
        private int ParseElementValue(eType elementType, string expression, int inPos)
        {
            switch (elementType)
            {
                case eType.operation:
                    if (IsDoubleCharacterOperator(expression,inPos))
                        return inPos + 2;

                    // Not double character, check for single character
                    if (IsSingleCharacterOperator(expression,inPos))
                        return inPos + 1;

                    return -4; // something weird!!!

                case eType.value:
                    return ParseOutValue(expression, inPos);
                case eType.precedence:
                    return ParsePrecedence(expression, inPos);
                case eType.unknown:
                    elementValue = "x";
                    return inPos + 1; // single character always!
            }

            return -2; // just default error
        }

        /// <summary>
        /// The purpose of this method is to extract the complex text
        /// operands that exist, such as true/false strings.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="startpos"></param>
        /// <returns></returns>
        private int ExtractComplex(string expression, int startpos)
        {
            // Special case, looking for true/false
            if ((expression.Length - startpos) >= trueType.Length)
            {
                string sTrue = expression.Substring(startpos, trueType.Length).ToLower(new CultureInfo("en-US", false));
                if (sTrue == trueType)
                {
                    elementValue = "1";
                    elementType = eType.boolean;
                    return startpos + trueType.Length;
                }
            }
            if ((expression.Length - startpos) >= falseType.Length)
            {
                string sFalse = expression.Substring(startpos, falseType.Length).ToLower(new CultureInfo("en-US", false));
                if (sFalse == falseType)
                {
                    elementValue = "0";
                    elementType = eType.boolean;
                    return startpos + falseType.Length;
                }
            }

            return 0;
        }

        /// <summary>
        /// ExtractElement takes in the expression and returns the index
        /// position of where the parsed element ends. The element class
        /// is set to the element value
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public int ExtractElement(string expression, int startpos)
        {
            // Okay we have been passed an expression string, we need
            // to extract a single element

            this.Init();

            // Skip the spaces...
            int i = startpos;
            for (; i < expression.Length && expression[i] == ' '; i++);

            string firstChar = expression[i].ToString();
            if (precedenceType.Contains(firstChar))
                elementType = eType.precedence;
            else if (operationType.Contains(firstChar))
                elementType = eType.operation;
            else if (valueType.Contains(firstChar))
                elementType = eType.value;
            else if (unknownType.Contains(firstChar))
                elementType = eType.unknown;
            else
            {
                // pull out the phrase true/false (or others)
                int pos = this.ExtractComplex(expression, i);
                if (pos != 0) return pos;
            }

            return ParseElementValue(elementType, expression, i);
        }
    }// end of class Element

    /// <summary>
    /// A lightweight collection class to hold our parsed elements.
    /// </summary>
    class Elements : CollectionBase
    {
        override public string ToString()
        {
            string s = "";
            for (int i = 0; i < this.Count; i++)
            {
                s += this[i].ElementValue;
            }

            return s;
        }

        public void InsertAt(int index, Element NewElement)
        {
            if (index > this.List.Count)
                this.Add(NewElement);
            else
                this.List.Insert(index, NewElement);
        }

        public virtual void Add(Element NewElement)
        {
            this.List.Add(NewElement);
        }

        public virtual Element this[int Index]
        {
            get
            {
                //return the Element at IList[Index]
                return (Element)this.List[Index];

            }
        }
    }// end of class Elements

}// end of namespace
