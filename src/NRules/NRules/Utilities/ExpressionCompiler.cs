using System;
using System.Collections.Generic;
using System.Linq;
using NRules.AgendaFilters;
using NRules.Aggregators;
using NRules.Extensibility;
using NRules.Rete;
using NRules.RuleModel;
using Tuple = NRules.Rete.Tuple;

namespace NRules.Utilities
{
    internal static class ExpressionCompiler
    {
        public static ILhsExpression<TResult> CompileLhsExpression<TResult>(ExpressionElement element, List<Declaration> declarations)
        {
            if (element.Imports.Count() == 1 &&
                Equals(element.Imports.Single(), declarations.Last()))
            {
                return CompileLhsFactExpression<TResult>(element);
            }
            return CompileLhsTupleFactExpression<TResult>(element, declarations);
        }

        public static ILhsFactExpression<TResult> CompileLhsFactExpression<TResult>(ExpressionElement element)
        {
            var optimizedExpression = ExpressionOptimizer.Optimize<Func<Fact, TResult>>(
                element.Expression, IndexMap.Unit, tupleInput: false, factInput: true);
            var @delegate = optimizedExpression.Compile();
            var expression = new LhsFactExpression<TResult>(element.Expression, @delegate);
            return expression;
        }

        public static ILhsTupleExpression<TResult> CompileLhsTupleExpression<TResult>(ExpressionElement element, List<Declaration> declarations)
        {
            var factMap = IndexMap.CreateMap(element.Imports, declarations);
            var optimizedExpression = ExpressionOptimizer.Optimize<Func<Tuple, TResult>>(
                element.Expression, factMap, tupleInput: true, factInput: false);
            var @delegate = optimizedExpression.Compile();
            var expression = new LhsTupleExpression<TResult>(element.Expression, @delegate);
            return expression;
        }

        public static ILhsExpression<TResult> CompileLhsTupleFactExpression<TResult>(ExpressionElement element, List<Declaration> declarations)
        {
            var factMap = IndexMap.CreateMap(element.Imports, declarations);
            var optimizedExpression = ExpressionOptimizer.Optimize<Func<Tuple, Fact, TResult>>(
                element.Expression, factMap, tupleInput: true, factInput: true);
            var @delegate = optimizedExpression.Compile();
            var expression = new LhsExpression<TResult>(element.Expression, @delegate);
            return expression;
        }

        public static IActivationExpression<TResult> CompileActivationExpression<TResult>(ExpressionElement element,
            List<Declaration> declarations, IndexMap tupleFactMap)
        {
            var activationFactMap = IndexMap.CreateMap(element.Imports, declarations);
            var factMap = IndexMap.Compose(tupleFactMap, activationFactMap);
            var optimizedExpression = ExpressionOptimizer.Optimize<Func<Tuple, TResult>>(
                element.Expression, factMap, tupleInput: true, factInput: false);
            var @delegate = optimizedExpression.Compile();
            var expression = new ActivationExpression<TResult>(element.Expression, @delegate);
            return expression;
        }

        public static IRuleAction CompileAction(ActionElement element, List<Declaration> declarations,
            List<DependencyElement> dependencies, IndexMap tupleFactMap)
        {
            var activationFactMap = IndexMap.CreateMap(element.Imports, declarations);
            var factMap = IndexMap.Compose(tupleFactMap, activationFactMap);

            var dependencyIndexMap = IndexMap.CreateMap(element.Imports, dependencies.Select(x => x.Declaration));
            if (dependencyIndexMap.HasData)
            {
                var optimizedExpression = ExpressionOptimizer
                    .Optimize<Action<IContext, Tuple, IDependencyResolver, IResolutionContext>>(
                        element.Expression, factMap, dependencies, dependencyIndexMap);
                var @delegate = optimizedExpression.Compile();
                var action = new RuleActionWithDependencies(element.Expression, @delegate, element.ActionTrigger);
                return action;
            }
            else
            {
                var optimizedExpression = ExpressionOptimizer.Optimize<Action<IContext, Tuple>>(
                    element.Expression, 1, factMap, tupleInput: true, factInput: false);
                var @delegate = optimizedExpression.Compile();
                var action = new RuleAction(element.Expression, @delegate, element.ActionTrigger);
                return action;
            }
        }
        
        public static IAggregateExpression CompileAggregateExpression(NamedExpressionElement element, List<Declaration> declarations)
        {
            var compiledExpression = CompileLhsExpression<object>(element, declarations);
            var expression = new AggregateExpression(element.Name, compiledExpression);
            return expression;
        }
    }
}