﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GuardExtensions;
using LambdaSql.Field;
using LambdaSql.Filter;

namespace LambdaSql
{
    public abstract class SqlSelectBase
    {
        protected const string SEPARATOR = "\r\n";
        protected const string SEPARATOR_WITH_OFFSET = "\r\n    ";

        protected readonly IMetadataProvider MetadataProvider = LambdaSql.MetadataProvider.Instance;

        protected SqlSelectBase(SqlSelectInfo info)
        {
            Info = info;
        }

        public SqlSelectInfo Info { get; }

        private ISqlField CreateSqlField<TEntity>(string name, ISqlAlias alias)
        {
            Guard.IsNotEmpty(name);
            Guard.IsNotNull(alias);
            return new SqlField<TEntity>
            {
                Name = name,
                Alias = alias
            };
        }

        protected ISqlField[] CreateSqlFields<TEntity>(ISqlAlias alias, IEnumerable<Expression<Func<TEntity, object>>> fields)
        {
            return fields.Select(f => CreateSqlField<TEntity>(MetadataProvider.GetPropertyName(f), alias)).ToArray();
        }

        protected ISqlFilter GetJoinFilter<TLeft, TJoin>(BinaryExpression expression, SqlAlias<TLeft> leftAlias, SqlAlias<TJoin> joinAlias)
        {
            Guard.IsNotNull(leftAlias);
            Guard.IsNotNull(joinAlias);
            if (expression == null || expression.NodeType != ExpressionType.Equal)
                throw new JoinException("Invalid join expression");

            var leftExpr = LibHelper.GetMemberExpression(expression.Left);
            var rightExpr = LibHelper.GetMemberExpression(expression.Right);

            Guard.IsNotNull(leftExpr);
            Guard.IsNotNull(rightExpr);

            var rightField = leftExpr.Expression.Type == leftAlias.EntityType
                ? CreateSqlField<TJoin>(MetadataProvider.GetPropertyName(rightExpr), joinAlias)
                : (ISqlField)CreateSqlField<TLeft>(MetadataProvider.GetPropertyName(rightExpr), leftAlias);

            return leftExpr.Expression.Type == leftAlias.EntityType
                ? SqlFilter<TLeft>.From<object>(SqlField.From(typeof(TLeft), typeof(int),
                    leftAlias, MetadataProvider.GetPropertyName(leftExpr))).EqualTo(rightField)
                : (ISqlFilter)SqlFilter<TJoin>.From<object>(SqlField.From(typeof(TJoin), typeof(int),
                    joinAlias, MetadataProvider.GetPropertyName(leftExpr))).EqualTo(rightField);
        }

        protected SqlSelectInfo Join<TJoin>(JoinType joinType, ISqlFilter condition, SqlAlias<TJoin> joinAlias = null)
        {
            Guard.IsNotNull(condition);
            if (joinAlias == null)
                joinAlias = MetadataProvider.AliasFor<TJoin>();
            if (Info.Joins().Any(j => j.JoinAlias.Value == joinAlias.Value))
                throw new JoinException($"Alias '{joinAlias.Value}' is already registered");

            var join = new SqlJoin<TJoin>(joinType, condition, joinAlias);
            return Info.Joins(join);
        }
    }
}
