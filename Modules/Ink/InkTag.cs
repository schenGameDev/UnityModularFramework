using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Commons;
using UnityEngine;

[Serializable]
public class InkTag {
    static readonly string CONDITION_TAG_PREFIX = "COND_";
    static readonly string CHOICE_GROUP_TAG_PREFIX = "GRP_";
    static readonly string CHOICE_INDEX_OVERRIDE_TAG_PREFIX = "IDX_";
    static readonly string CHOICE_HIDE_TEXT_TAG = "HIDE";
    static readonly string INTERRUPTED_TAG = "INTERRUPT";
    static readonly char[] ESCAPE_MATH_CHARS = new char[] {'+','-','*','/','%'};
    static readonly char[] ESCAPE_LOGIC_CHARS = new char[] {'|','&'};
    static readonly char[] ESCAPE_COMPARE_CHARS = new char[] {'=','!','>','<'};
    static readonly string TRUE = "T";
    static readonly string FALSE = "F";
    
    [SerializeField] public string[] codes;
    public InkTagType type;

    public InkTag(InkTagType type, params string[] codes) {
        this.type = type;
        this.codes = codes;
    }

    public static InkTag Of(string tagText, InkTagDefBucket[] tagDefBuckets, Func<string,Optional<Keeper>> getKeeperFunc) {
        if(tagText.StartsWith(CONDITION_TAG_PREFIX)) {
            string originExpression = tagText[CONDITION_TAG_PREFIX.Length..];
            string expression = originExpression.Replace(" ","").Replace("true","T").Replace("false","F");

            int right;
            do {
                right = expression.IndexOf(")", StringComparison.Ordinal);
                if(right!=-1) {
                    int left = expression.LastIndexOf("(",0,right, StringComparison.Ordinal);
                    if(left == -1) {
                        throw new ArgumentException("Wrong format expression: " + expression);
                    }
                    string unitExpression = expression.SubstringBetween(left,right + 1);
                    if(ESCAPE_LOGIC_CHARS.Any(c=>unitExpression.Contains(c))) {
                        expression = expression.Replace(unitExpression, SimpleBoolEvaluator(unitExpression,getKeeperFunc));
                    } else if(ESCAPE_COMPARE_CHARS.Any(c=>unitExpression.Contains(c))) {
                        expression = expression.Replace(unitExpression, SimpleCompareEvaluator(unitExpression,getKeeperFunc));
                    } else if(ESCAPE_MATH_CHARS.Any(c=>unitExpression.Contains(c))) {
                        expression = expression.Replace(unitExpression, SimpleMathEvaluator(unitExpression,getKeeperFunc));
                    } else {
                        expression.Remove(right);
                        expression.Remove(left);
                    }

                    continue;
                }
                expression = ESCAPE_LOGIC_CHARS.Any(c=>expression.Contains(c))?
                                SimpleBoolEvaluator(expression, getKeeperFunc): SimpleCompareEvaluator(expression, getKeeperFunc);
            } while(right != -1);

            if(expression.Length!=1) throw new ArgumentException(expression);
            return new InkTag(InkTagType.CONDITION, expression==TRUE? "true" : "false", originExpression);
        }

        if(tagText.StartsWith(CHOICE_GROUP_TAG_PREFIX)) {
            return new InkTag(InkTagType.GROUP, tagText[CHOICE_GROUP_TAG_PREFIX.Length..].Split('_'));
        }

        if (tagText.StartsWith(CHOICE_INDEX_OVERRIDE_TAG_PREFIX))
        {
            return new InkTag(InkTagType.INDEX, tagText[CHOICE_INDEX_OVERRIDE_TAG_PREFIX.Length..]);
        }

        if (tagText == CHOICE_HIDE_TEXT_TAG)
        {
            return new InkTag(InkTagType.HIDE_CHOICE_TEXT);
        }
        if (tagText == INTERRUPTED_TAG)
        {
            return new InkTag(InkTagType.INTERRUPTED);
        }

        var tag = tagDefBuckets.Select(x=>x.Get(tagText)).First(op => op.HasValue).Get();
        if (tag.type == InkTagType.CHARACTER && tag.codes[1] == "false")
        {
            getKeeperFunc("Know" + tagText.FirstCharToUpper()).Do(x =>
            {
                if (x) tag.codes[1] = "true";
            });
        }

        return tag;
    }

    private static string SimpleMathEvaluator(string expression, Func<string,Optional<Keeper>> getKeeperFunc) {
        if(expression.StartsWith("(")) {
            expression = expression.SubstringBetween(1,expression.Length - 1);
        }
        List<string> operands = expression.Where(ch => ESCAPE_MATH_CHARS.Contains(ch)).Select(ch=>""+ch).ToList();

        List<string> tokens = expression.Split(ESCAPE_MATH_CHARS).ToList();

        tokens.Compute(tk => {
            Optional<Keeper> k = getKeeperFunc(tk);
            if(k.HasValue) {
                return k.Get().ToString();
            }
            return tk;
        });

        while(tokens.Count > 1) {
            int i = operands.IndexOf("*");
            int temp = operands.IndexOf("/");
            if(i==-1 || temp<i) i = temp;
            temp = operands.IndexOf("%");
            if(i==-1 || temp<i) i = temp;
            if(i==-1) {
                i = operands.IndexOf("+");
                if(i==-1 || temp<i) i = operands.IndexOf("-");
            }

            string opr = operands.RemoveAtAndReturn(i);

            string astr = tokens.RemoveAtAndReturn(i);
            string bstr = tokens[i];

            if(int.TryParse(astr, out int aint) && int.TryParse(bstr, out int bint)) {
                tokens[i] = opr switch
                {
                    "+" => (aint + bint).ToString(),
                    "-" => (aint - bint).ToString(),
                    "*" => (aint * bint).ToString(),
                    "/" => (aint / bint).ToString(),
                    "%" => (aint % bint).ToString(),
                    _ => throw new ArgumentException(opr+ " unknown")
                };
            } else {
                float a = float.Parse(tokens[0]);
                float b = float.Parse(tokens[1]);
                tokens[i] = opr switch
                {
                    "+" => (a + b).ToString("0.00"),
                    "-" => (a - b).ToString("0.00"),
                    "*" => (a * b).ToString("0.00"),
                    "/" => (a / b).ToString("0.00"),
                    "%" => (a % b).ToString("0.00"),
                    _ => throw new ArgumentException(opr+ " unknown")
                };
            }
        }
        return tokens[0];

    }

    private static string SimpleCompareEvaluator(string expression, Func<string,Optional<Keeper>> getKeeperFunc) {
        if(expression.StartsWith("(")) {
            expression = expression.SubstringBetween(1,expression.Length - 1);
        }

        string operand = "";
        foreach(var ch in expression) {
            if(ESCAPE_COMPARE_CHARS.Contains(ch)) {
                operand+=ch;
            } else if(operand.NonEmpty()) {
                break;
            }
        }

        if(operand.IsEmpty()) { // one variable
            if(expression == TRUE || expression == FALSE) return expression;
            Keeper k =getKeeperFunc(expression).OrElseThrow(new KeyNotFoundException(expression));
            return k? TRUE : FALSE;
        }

        List<string> tokens = expression.Split(operand).ToList();
        if(tokens.Count!=2) throw new ArgumentException("Cannot parse " + expression);

        tokens.Compute(tk => {
            if(tk.Any(ch => ESCAPE_MATH_CHARS.Contains(ch))) {
                return SimpleMathEvaluator(tk,getKeeperFunc);
            }

            Optional<Keeper> k = getKeeperFunc(tk);
            if(k.HasValue) {
                return k.Get().ToString();
            }
            return tk;
        });
        float a = float.Parse(tokens[0]);
        float b = float.Parse(tokens[1]);

        bool res = operand switch
            {
                "==" => a==b,
                "!=" => a!=b,
                ">=" => a>=b,
                "<=" => a<=b,
                ">" => a>b,
                "<" => a<b,
                _ => throw new ArgumentException(operand + " unknown")
            };
        return res? TRUE : FALSE;
    }

    private static string SimpleBoolEvaluator(string expression, Func<string,Optional<Keeper>> getKeeperFunc) {
        if(expression.StartsWith("(")) {
            expression = expression.SubstringBetween(1,expression.Length - 1);
        }
        List<string> operands = new();
        string operand = "";
        expression.ForEach(ch => {
            if(ESCAPE_LOGIC_CHARS.Contains(ch)) {
                operand+=ch;
            } else if(operand.NonEmpty()) {
                operands.Add(operand);
                operand = "";
            }
        });
        if(operand.NonEmpty()) operands.Add(operand);

        if(operands.IsEmpty()) { // one variable
            if(expression == TRUE || expression == FALSE) return expression;
            Keeper k = getKeeperFunc(expression).OrElseThrow(new KeyNotFoundException(expression));
            return k? TRUE : FALSE;
        }

        List<string> tokens = expression.Split(ESCAPE_LOGIC_CHARS).Where(x=>x.NonEmpty()).ToList();

        tokens.Compute(tk => {
            if(tk.Any(ch => ESCAPE_COMPARE_CHARS.Contains(ch))) {
                return SimpleCompareEvaluator(tk, getKeeperFunc);
            }
            if(tk.Any(ch => ESCAPE_MATH_CHARS.Contains(ch))) {
                return SimpleMathEvaluator(tk, getKeeperFunc);
            }

            Optional<Keeper> k = getKeeperFunc(tk);
            if(k.HasValue) {
                return k.Get().ToString();
            }
            return tk;
        });

        while(tokens.Count > 1) {
            string expr = tokens.RemoveAtAndReturn(0) + operands.RemoveAtAndReturn(0) + tokens[0];

            tokens[0] = expr switch
            {
                "T&&T" => TRUE,
                "T||T" => TRUE,
                "F&&F" => FALSE,
                "F||F" => FALSE,
                "T&&F" => FALSE,
                "T||F" => TRUE,
                "F&&T" => FALSE,
                "F||T" => TRUE,
                _ => throw new ArgumentException(expr + " unknown")
            };
        }
        return tokens[0];
    }

}

public enum InkTagType {
    CHARACTER, // {character_name, is_character_known}
    PORTRAIT, // character portrait [id, position_on_screen]
    CONDITION, // in-text format: COND_{expression}${explanation_id} e.g. COND_player_health>1$12
    GROUP, // in-text format: GRP_{id} e.g. GRP_12 If not set, the line will display at default dialog box, the choices will display at default button group
    EFFECT, // word effect, font size change, print speed, color
    INDEX,
    
    HIDE_CHOICE_TEXT,
    INTERRUPTED
}