using SharpUnits.Formulas;
using SharpUnits.Metrology;

namespace SharpUnits.SysMl;

/// <summary>Validates Cameo SysML parametric constraint blocks and evaluates instrument test observations against Cameo requirement limits.</summary>
public static class CameoModelValidator
{
    /// <summary>
    /// Statically validates a Cameo <see cref="QudvConstraintBlock"/> using <see cref="ProcedureValidator"/>,
    /// inferring undeclared parameter dimensions and checking dimensional and ISQ kind consistency.
    /// </summary>
    public static ProcedureValidationResult ValidateConstraintBlock(
        QudvConstraintBlock block,
        UnitEnvironment env,
        IReadOnlyDictionary<string, QuantitySpec>? customSpecs = null)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(env);

        var variables = new List<ProcedureVariable>(block.Parameters.Count);
        foreach (var p in block.Parameters)
        {
            RationalDimension? dim = null;
            QuantityKind? kind = null;

            if (!string.IsNullOrWhiteSpace(p.QuantityKindId))
            {
                if (customSpecs is not null && customSpecs.TryGetValue(p.QuantityKindId, out var cs))
                {
                    dim = cs.Dimension;
                    kind = cs.ToKind();
                }
                else if (Isq.TryGetById(p.QuantityKindId, out var isqSpec) && isqSpec is not null)
                {
                    dim = isqSpec.Dimension;
                    kind = isqSpec.ToKind();
                }
                else
                {
                    var qk = new QuantityKind(p.QuantityKindId);
                    dim = env.Kinds.DimensionOf(qk);
                    kind = qk;
                }
            }

            if (!string.IsNullOrWhiteSpace(p.UnitSymbol))
            {
                var unit = env.ResolveUnit(p.UnitSymbol);
                if (dim is not null && dim.Value != unit.Dimension)
                    throw new InvalidOperationException($"Constraint parameter '{p.Name}' unit '{p.UnitSymbol}' disagrees with quantity kind '{p.QuantityKindId}'.");
                dim ??= unit.Dimension;
                kind ??= unit.DefaultSpec?.ToKind();
            }

            variables.Add(new ProcedureVariable(p.Name, dim, kind));
        }

        var formulas = block.Equations.Select(eq => new ProcedureFormula(eq)).ToArray();
        return ProcedureValidator.Validate(variables, formulas);
    }

    /// <summary>
    /// Converts a Cameo <see cref="QudvRequirementLimit"/> into a <see cref="QuantityAcceptanceLimit"/> and
    /// <see cref="MeasurementDecisionRule"/>, then evaluates <paramref name="observation"/> against <paramref name="calibration"/>.
    /// </summary>
    public static MeasurementDisposition VerifyRequirement(
        QudvRequirementLimit requirement,
        InstrumentMeasurement observation,
        CalibrationRecord calibration,
        UnitEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(env);

        var unit = env.Registry.Resolve(requirement.UnitSymbol);
        var kind = new QuantityKind(requirement.QuantityKindId);
        var minSem = new SemanticQuantity(unit.ToQuantity(requirement.Minimum), kind, env.Kinds);
        var maxSem = new SemanticQuantity(unit.ToQuantity(requirement.Maximum), kind, env.Kinds);

        var limits = new QuantityAcceptanceLimit(minSem, maxSem);
        double guardBandSi = requirement.GuardBand * unit.Converter.Scale;
        var rule = new MeasurementDecisionRule(requirement.CoverageFactor, guardBandSi);
        return rule.Evaluate(observation, limits, calibration);
    }
}
