using System.Linq.Expressions;



namespace Business.Helpers
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            if (left == null) return right;
            if (right == null) return left;

            var param = Expression.Parameter(typeof(T), "x");
            var leftBody = ReplaceParameter(left.Body, left.Parameters[0], param);
            var rightBody = ReplaceParameter(right.Body, right.Parameters[0], param);
            var body = Expression.AndAlso(leftBody, rightBody);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static Expression ReplaceParameter(Expression expression, ParameterExpression toReplace, ParameterExpression replaceWith)
        {
            return new ParameterRewriter(toReplace, replaceWith).Visit(expression);
        }

        private sealed class ParameterRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;
            public ParameterRewriter(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _from ? _to : base.VisitParameter(node);
            }
        }
    }
}