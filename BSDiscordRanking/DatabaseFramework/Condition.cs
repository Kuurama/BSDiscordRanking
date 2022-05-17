using System;
using System.Collections.Generic;
using System.Linq;

namespace BSDiscordRanking.DatabaseFramework
{
    /// <summary>
    /// Condition transform
    /// </summary>
    public enum ConditionTransform
    {
        None,
        Lower,
        Upper
    }

    /// <summary>
    /// Base condition class
    /// </summary>
    public class Condition
    {
        /// <summary>
        /// Condition layer
        /// </summary>
        public enum Layer
        {
            Where,
            OrWhere,
            WhereIn,
            MatchAgainst,
            GroupBy,
            Order,
            OrderByRand,
            Limit,
            MaxLayer
        }

        /// <summary>
        /// Layer ID
        /// </summary>
        public Layer LayerID;
        /// <summary>
        /// Misc Value
        /// </summary>
        public string Value;
        /// <summary>
        /// Arguments
        /// </summary>
        public Dictionary<string, object> Args = new Dictionary<string, object>();

        /// <summary>
        /// Filter data for queries cache
        /// </summary>
        public string CacheFilterData;
    }

    /// <summary>
    /// Where condition
    /// </summary>
    public class Where : Condition
    {
        /// <summary>
        /// Operator type
        /// </summary>
        public enum Operator
        {
            Equal,
            Different,
            Less,
            LessOrEqual,
            More,
            MoreOrEqual,
            Like,
            NotLike,
            And
        }

        /// <summary>
        /// Where condition constructor
        /// </summary>
        /// <param name="p_FieldName">Comparaison Field</param>
        /// <param name="p_Operator">Compairaison operator</param>
        /// <param name="p_Value">Compairaison value</param>
        public Where(string p_FieldName, Operator p_Operator, object p_Value, ConditionTransform p_Transform = ConditionTransform.None)
        {
            CacheFilterData = p_FieldName + ";" + p_Operator.ToString() + ";" + p_Value.ToString() + ";" + p_Transform.ToString();

            string l_Operator = "";
            switch (p_Operator)
            {
                case Operator.Equal:        l_Operator = "=";           break;
                case Operator.Different:    l_Operator = "!=";          break;
                case Operator.Less:         l_Operator = "<";           break;
                case Operator.LessOrEqual:  l_Operator = "<=";          break;
                case Operator.More:         l_Operator = ">";           break;
                case Operator.MoreOrEqual:  l_Operator = ">=";          break;
                case Operator.Like:         l_Operator = " LIKE ";      break;
                case Operator.NotLike:      l_Operator = " NOT LIKE ";  break;
                case Operator.And:          l_Operator = " & ";         break;
            }

            string l_Seed = Helper.Random.GenerateString(8);

            string l_FieldName  = p_FieldName;
            object l_Value      = p_Value;

            switch (p_Transform)
            {
                case ConditionTransform.Lower:
                    l_FieldName = "LOWER(" + p_FieldName + ")";

                    if (l_Value.GetType() == typeof(string) || l_Value.GetType() == typeof(String))
                        l_Value = ((string)l_Value).ToLower();

                    break;

                case ConditionTransform.Upper:
                    l_FieldName = "UPPER(" + p_FieldName + ")";

                    if (l_Value.GetType() == typeof(string) || l_Value.GetType() == typeof(String))
                        l_Value = ((string)l_Value).ToUpper();

                    break;

            }

            LayerID = Condition.Layer.Where;
            Value   = l_FieldName + l_Operator + "@Where" + l_Seed + p_FieldName;

            Args.Add("@Where" + l_Seed + p_FieldName, l_Value);
        }
    }

    /// <summary>
    /// Where condition
    /// </summary>
    public class WhereField : Condition
    {
        /// <summary>
        /// Operator type
        /// </summary>
        public enum Operator
        {
            Equal,
            Different,
            Less,
            LessOrEqual,
            More,
            MoreOrEqual,
            Like,
            NotLike,
            And
        }

        /// <summary>
        /// Where condition constructor between to table columns
        /// </summary>
        /// <param name="p_FieldName">Comparaison Field</param>
        /// <param name="p_Operator">Compairaison operator</param>
        /// <param name="p_OtherFieldName">Compairaison value</param>
        public WhereField(string p_FieldName, Operator p_Operator, string p_OtherFieldName, ConditionTransform p_Transform = ConditionTransform.None)
        {
            CacheFilterData = p_FieldName + ";" + p_OtherFieldName.ToString() + ";" + p_Transform.ToString();

            string l_Operator = "";
            switch (p_Operator)
            {
                case Operator.Equal:        l_Operator = "=";           break;
                case Operator.Different:    l_Operator = "!=";          break;
                case Operator.Less:         l_Operator = "<";           break;
                case Operator.LessOrEqual:  l_Operator = "<=";          break;
                case Operator.More:         l_Operator = ">";           break;
                case Operator.MoreOrEqual:  l_Operator = ">=";          break;
                case Operator.Like:         l_Operator = " LIKE ";      break;
                case Operator.NotLike:      l_Operator = " NOT LIKE ";  break;
                case Operator.And:          l_Operator = " & ";         break;
            }

            string l_FieldName      = p_FieldName;
            string l_OtherFieldName = p_OtherFieldName;

            switch (p_Transform)
            {
                case ConditionTransform.Lower:
                    l_FieldName         = "LOWER(" + p_FieldName + ")";
                    l_OtherFieldName    = "LOWER(" + p_OtherFieldName + ")";
                    break;

                case ConditionTransform.Upper:
                    l_FieldName         = "UPPER(" + p_FieldName + ")";
                    l_OtherFieldName    = "UPPER(" + p_OtherFieldName + ")";
                    break;
            }

            LayerID = Condition.Layer.Where;
            Value   = l_FieldName + l_Operator + l_OtherFieldName;
        }
    }

    /// <summary>
    /// Where In condition
    /// </summary>
    public class WhereIn<T> : Condition
    {
        /// <summary>
        /// Where in condition constructor
        /// </summary>
        /// <param name="p_FieldName">Compairaison field</param>
        /// <param name="p_Values">Compairaison values</param>
        public WhereIn(string p_FieldName, bool p_Not, params T[] p_Values)
        {
            LayerID = Condition.Layer.WhereIn;
            Value   = p_FieldName + (p_Not ? " NOT " : " ") + "IN (";

            string l_Seed = Helper.Random.GenerateString(15);
            string l_ValueCollectionForCache = "";

            for (int l_I = 0; l_I < p_Values.Length; l_I++)
            {
                if (l_I != 0)
                    Value += ", ";

                string l_VarName = "@WhereIn" + l_Seed + "_" + l_I.ToString();

                Value += l_VarName;
                Args.Add(l_VarName, p_Values[l_I]);

                l_ValueCollectionForCache += "#" + p_Values[l_I].ToString();
            }

            CacheFilterData = p_FieldName + ";" + p_Not.ToString() + ";" + l_ValueCollectionForCache.ToString();

            Value += ")";
        }
    }

    /// <summary>
    /// Order condition
    /// </summary>
    public class Order : Condition
    {
        /// <summary>
        /// Order condition constructor
        /// </summary>
        /// <param name="p_Desc">Is reverse order</param>
        /// <param name="p_FieldNames">Ordering fields</param>
        public Order(bool p_Desc, params string[] p_FieldNames)
        {
            LayerID = Condition.Layer.Order;
            Value = "ORDER BY ";

            string l_ValueCollectionForCache = "";
            for (int l_I = 0; l_I < p_FieldNames.Length; l_I++)
            {
                if (l_I != 0)
                    Value += ", ";

                Value += p_FieldNames[l_I];
                Value += " " + (p_Desc ? "DESC" : "ASC");

                l_ValueCollectionForCache += "#" + p_FieldNames[l_I].ToString();
            }

            CacheFilterData = p_Desc + ";" + l_ValueCollectionForCache.ToString();
        }
    }

    /// <summary>
    /// Order by rand condition
    /// </summary>
    public class OrderByRand : Condition
    {
        /// <summary>
        /// Order condition constructor
        /// </summary>
        /// <param name="p_Desc">Is reverse order</param>
        /// <param name="p_FieldNames">Ordering fields</param>
        public OrderByRand()
        {
            LayerID = Condition.Layer.Order;
            Value = "ORDER BY RAND()";
        }
    }

    /// <summary>
    /// OrWHere condition
    /// </summary>
    public class OrWhere : Condition
    {
        /// <summary>
        /// OrWHere condition constructor
        /// </summary>
        /// <param name="p_Desc">Is reverse order</param>
        /// <param name="p_FieldNames">Ordering fields</param>
        public OrWhere(params Where[] p_WhereFields)
        {
            LayerID = Condition.Layer.OrWhere;
            Value = "(";
            bool l_IsFirt = true;
            foreach (var l_CurrentWhere in p_WhereFields)
            {
                if (!l_IsFirt)
                    Value += " OR ";

                Value += l_CurrentWhere.Value;
                Args.Add(l_CurrentWhere.Args.FirstOrDefault().Key, l_CurrentWhere.Args.FirstOrDefault().Value);
                l_IsFirt = false;
            }

            Value += ")";
        }
    }

    /// <summary>
    /// Limit condition
    /// </summary>
    public class Limit : Condition
    {
        /// <summary>
        /// Limit condition constructor
        /// </summary>
        /// <param name="p_Begin">begin limit</param>
        /// <param name="p_CountLines">Count lines</param>
        public Limit(uint p_Begin, uint p_CountLines)
        {
            LayerID = Condition.Layer.Limit;
            Value = "LIMIT " + p_Begin.ToString() + ", " + p_CountLines.ToString();

            CacheFilterData = p_Begin.ToString() + ";" + p_CountLines.ToString();
        }
    }

    /// <summary>
    /// Group BY condition
    /// </summary>
    public class GroupBy : Condition
    {
        public GroupBy(String p_Field)
        {
            LayerID = Condition.Layer.GroupBy;
            Value = "GROUP BY " + p_Field;
        }
    }

}
