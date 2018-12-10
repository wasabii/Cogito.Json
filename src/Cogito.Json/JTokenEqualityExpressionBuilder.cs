﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Newtonsoft.Json.Linq;

namespace Cogito.Json
{

    /// <summary>
    /// Provides the capability of generating LINQ Expression trees to test whether a <see cref="JToken"/> instance is
    /// equal with another.
    /// </summary>
    public class JTokenEqualityExpressionBuilder
    {

        /// <summary>
        /// Returns an expression that returns <c>true</c> if all of the given expressions returns <c>true</c>.
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
        static Expression AllOf(IEnumerable<Expression> expressions)
        {
            Expression e = null;

            foreach (var i in expressions)
                e = e == null ? i : Expression.AndAlso(i, e);

            return e;
        }

        /// <summary>
        /// Returns an expression that returns <c>true</c> if all of the given expressions returns <c>true</c>.
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
        static Expression AllOf(params Expression[] expressions) =>
            AllOf((IEnumerable<Expression>)expressions);

        /// <summary>
        /// Builds an expression tree that implements validation of JSON.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public Expression<Func<JToken, bool>> Build(JToken template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var t = Expression.Parameter(typeof(JToken), "target");
            var e = Build(template, t);
            return Expression.Lambda<Func<JToken, bool>>(e, t);
        }

        /// <summary>
        /// Builds an expression tree which evaluates whether the <see cref="JToken"/> refered to by <paramref name="target"/>
        /// is the same as the <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public Expression Build(JToken template, Expression target)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            switch (template.Type)
            {
                case JTokenType.Array:
                    return BuildArray((JArray)template, target);
                case JTokenType.Boolean:
                    return BuildBoolean((JValue)template, target);
                case JTokenType.Float:
                    return BuildFloat((JValue)template, target);
                case JTokenType.Integer:
                    return BuildInteger((JValue)template, target);
                case JTokenType.Null:
                    return BuildNull((JValue)template, target);
                case JTokenType.Object:
                    return BuildObject((JObject)template, target);
                case JTokenType.String:
                    return BuildString((JValue)template, target);
                default:
                    throw new InvalidOperationException("Unsupported token type in template.");
            }
        }

        Expression BuildArray(JArray template, Expression target)
        {
            return AllOf(BuildArrayEval(template, target));
        }

        /// <summary>
        /// Iterates an expression that compares each position of each array.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        IEnumerable<Expression> BuildArrayEval(JArray template, Expression target)
        {
            var t = Expression.Convert(target, typeof(JArray));

            yield return Expression.Equal(Expression.Constant(JTokenType.Array), Expression.Property(target, nameof(JToken.Type)));
            yield return Expression.Equal(Expression.Constant(template.Count), Expression.Property(t, nameof(JArray.Count)));

            for (var i = 0; i < template.Count; i++)
                yield return Build(template[i], Expression.Property(t, "Item", Expression.Constant(i)));
        }

        Expression BuildBoolean(JValue template, Expression target)
        {
            return BuildValue<bool>(template, target);
        }

        Expression BuildFloat(JValue template, Expression target)
        {
            return BuildValue<double>(template, target);
        }

        Expression BuildInteger(JValue template, Expression target)
        {
            return BuildValue<long>(template, target);
        }

        Expression BuildNull(JValue template, Expression target)
        {
            return BuildValue<object>(template, target);
        }

        Expression BuildObject(JObject template, Expression target)
        {
            return AllOf(BuildObjectEval(template, target));
        }

        /// <summary>
        /// Iterates an expression that compares each position of each array.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        IEnumerable<Expression> BuildObjectEval(JObject template, Expression target)
        {
            var t = Expression.Convert(target, typeof(JObject));

            yield return Expression.Equal(Expression.Constant(JTokenType.Object), Expression.Property(target, nameof(JToken.Type)));
            yield return Expression.Equal(Expression.Constant(template.Count), Expression.Property(t, nameof(JObject.Count)));

            foreach (var p in template.Properties())
                yield return Expression.AndAlso(
                    Expression.IsTrue(Expression.Call(t, nameof(JObject.ContainsKey), new Type[0], new[] { Expression.Constant(p.Name) })),
                    Build(p.Value, Expression.Call(t, nameof(JObject.GetValue), new Type[0], new[] { Expression.Constant(p.Name) })));
        }

        Expression BuildString(JValue template, Expression target)
        {
            return BuildValue<string>(template, target);
        }

        Expression BuildValue<T>(JValue template, Expression target)
        {
            var t = Expression.Convert(target, typeof(JValue));
            var v = Expression.Convert(Expression.Property(t, nameof(JValue.Value)), typeof(T));

            return Expression.AndAlso(
                Expression.Equal(Expression.Constant(template.Type), Expression.Property(t, nameof(JValue.Type))),
                Expression.Equal(Expression.Constant((T)template.Value), v));
        }

    }

}
