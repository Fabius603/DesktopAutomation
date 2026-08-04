using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Contracts.Geometry;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Vergleicht eine Liste von Punkten entweder gegen einen Referenzpunkt mit Toleranz (Offset)
    /// oder gegen Achsen-Ausdrücke (Expression). Gibt zurück, ob Alle oder Mindestens ein Punkt passt.
    /// </summary>
    public sealed class PointComparisonStepHandler : JobStepHandler<PointComparisonStep, PointComparisonResult>
    {
        protected override Task<PointComparisonResult> ExecuteCoreAsync(
            PointComparisonStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var settings = step.Settings;

            var points = CollectPoints(settings.Points, ctx);
            if (points.Count == 0)
            {
                ctx.Logger.LogInformation(
                    "PointComparisonStepHandler: Keine Punkte aufgelöst – Ergebnis: false.");
                return Task.FromResult(
                    new PointComparisonResult { WasExecuted = true, Matches = false, MatchCount = 0, TotalCount = 0 });
            }

            int matchCount = 0;
            foreach (var point in points)
            {
                bool matches = settings.Mode switch
                {
                    PointComparisonMode.Offset     => EvaluateOffset(point, settings.OffsetSettings, ctx),
                    PointComparisonMode.Expression => EvaluateExpression(point, settings.ExpressionSettings),
                    _                              => false
                };
                if (matches) matchCount++;
            }

            bool result = settings.MatchRequirement switch
            {
                PointMatchRequirement.All => matchCount == points.Count,
                PointMatchRequirement.Any => matchCount > 0,
                _                        => false
            };

            ctx.Logger.LogInformation(
                "PointComparisonStepHandler: {MatchCount}/{Total} Punkte stimmen überein – Ergebnis: {Result}.",
                matchCount, points.Count, result);

            return Task.FromResult(
                new PointComparisonResult { WasExecuted = true, Matches = result, MatchCount = matchCount, TotalCount = points.Count });
        }

        private static List<PixelPoint> CollectPoints(List<PointEntry> entries, IStepPipelineContext ctx)
        {
            var result = new List<PixelPoint>();
            foreach (var entry in entries)
            {
                if (entry.Source == PointEntrySource.Manual)
                {
                    result.Add(entry.ManualPoint);
                }
                else
                {
                    var resolved = ResultBindingResolver.Resolve<PixelPoint>(ctx.Results, entry.PointsSource);
                    if (resolved.IsSuccess) result.AddRange(resolved.Values);
                }
            }
            return result;
        }

        private static bool EvaluateOffset(
            PixelPoint point,
            OffsetComparisonSettings settings,
            IStepPipelineContext ctx)
        {
            PixelPoint refPoint;

            if (settings.ReferenceSource == PointEntrySource.Manual)
            {
                refPoint = settings.ReferencePoint;
            }
            else
            {
                var resolved = ResultBindingResolver.Resolve<PixelPoint>(ctx.Results, settings.ReferencePointsSource);
                if (!resolved.IsSuccess) return false;
                refPoint = resolved.FirstOrDefault;
            }

            return Math.Abs(point.X - refPoint.X) <= settings.OffsetX
                && Math.Abs(point.Y - refPoint.Y) <= settings.OffsetY;
        }

        private static bool EvaluateExpression(
            PixelPoint point,
            ExpressionComparisonSettings settings)
        {
            if (settings.Expressions.Count == 0) return true;

            var results = settings.Expressions.Select(expr =>
            {
                int axisValue = string.Equals(expr.Axis, "Y", StringComparison.OrdinalIgnoreCase)
                    ? point.Y : point.X;

                return expr.Operator switch
                {
                    PointAxisOperator.LessThan           => axisValue < expr.Value,
                    PointAxisOperator.LessThanOrEqual    => axisValue <= expr.Value,
                    PointAxisOperator.GreaterThan        => axisValue > expr.Value,
                    PointAxisOperator.GreaterThanOrEqual => axisValue >= expr.Value,
                    PointAxisOperator.Equal              => axisValue == expr.Value,
                    PointAxisOperator.NotEqual           => axisValue != expr.Value,
                    _                                    => false
                };
            });

            return settings.CombineMode == ExpressionCombineMode.And
                ? results.All(r => r)
                : results.Any(r => r);
        }

        protected override PointComparisonResult CreateDefault() => PointComparisonResult.Default;
    }
}
