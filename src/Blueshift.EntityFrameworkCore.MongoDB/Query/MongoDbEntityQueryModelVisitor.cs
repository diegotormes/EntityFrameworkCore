using System;
using System.Linq;
using System.Linq.Expressions;
using Blueshift.EntityFrameworkCore.MongoDB.Query.ExpressionVisitors;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;

namespace Blueshift.EntityFrameworkCore.MongoDB.Query
{
    /// <inheritdoc />
    public class MongoDbEntityQueryModelVisitor : EntityQueryModelVisitor
    {
        private readonly IProjectionExpressionVisitorFactory _projectionExpressionVisitorFactory;
        private readonly IMongoDbDenormalizedCollectionCompensatingVisitorFactory
            _mongoDbDenormalizedCollectionCompensatingVisitorFactory;

        /// <inheritdoc />
        public MongoDbEntityQueryModelVisitor(
            [NotNull] EntityQueryModelVisitorDependencies entityQueryModelVisitorDependencies,
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] MongoDbEntityQueryModelVisitorDependencies mongoDbEntityQueryModelVisitorDependencies)
            : base(
                Check.NotNull(entityQueryModelVisitorDependencies, nameof(entityQueryModelVisitorDependencies)),
                Check.NotNull(queryCompilationContext, nameof(queryCompilationContext))
            )
        {
            _projectionExpressionVisitorFactory = entityQueryModelVisitorDependencies
                .ProjectionExpressionVisitorFactory;
            _mongoDbDenormalizedCollectionCompensatingVisitorFactory
                = Check.NotNull(mongoDbEntityQueryModelVisitorDependencies, nameof(mongoDbEntityQueryModelVisitorDependencies))
                    .MongoDbDenormalizedCollectionCompensatingVisitorFactory;
        }

        /// <inheritdoc />
        public override void VisitSelectClause(
            SelectClause selectClause,
            QueryModel queryModel)
        {
            Check.NotNull(selectClause, nameof(selectClause));
            Check.NotNull(queryModel, nameof(queryModel));

            if (selectClause.Selector.Type == Expression.Type.GetSequenceType()
                && selectClause.Selector is QuerySourceReferenceExpression)
            {
                return;
            }

            Expression selector = ReplaceClauseReferences(
                _projectionExpressionVisitorFactory
                    .Create(this, queryModel.MainFromClause)
                    .Visit(selectClause.Selector),
                inProjection: true);

            if ((Expression.Type.TryGetSequenceType() != null || !(selectClause.Selector is QuerySourceReferenceExpression))
                && !queryModel.ResultOperators
                    .Select(ro => ro.GetType())
                    .Any(
                        t => t == typeof(GroupResultOperator)
                             || t == typeof(AllResultOperator)))
            {
                Expression = Expression.Call(
                    LinqOperatorProvider.Select
                        .MakeGenericMethod(CurrentParameter.Type, selector.Type),
                    Expression,
                    Expression.Lambda(ConvertToRelationshipAssignments(selector), CurrentParameter));
            }
        }

        private Expression ConvertToRelationshipAssignments(Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression
                && IncludeCompiler.IsIncludeMethod(methodCallExpression))
            {
                expression = (MethodCallExpression) _mongoDbDenormalizedCollectionCompensatingVisitorFactory
                    .Create()
                    .Visit(methodCallExpression);
            }
            return expression;
        }

        /// <summary>
        ///     Translates a re-linq query model expression into a compiled query expression.
        /// </summary>
        /// <param name="expression"> The re-linq query model expression. </param>
        /// <param name="querySource"> The query source. </param>
        /// <param name="inProjection"> True when the expression is a projector. </param>
        /// <returns>A compiled query expression fragment.</returns>
        public override Expression ReplaceClauseReferences(Expression expression, IQuerySource querySource = null, bool inProjection = false)
        {
            return base.ReplaceClauseReferences(expression, querySource, inProjection);
        }
    }
}
