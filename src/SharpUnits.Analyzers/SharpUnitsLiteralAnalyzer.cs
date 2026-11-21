using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SharpUnits.Analyzers
{
    /// <summary>
    /// Roslyn compile-time analyzer validating SharpUnits unit/quantity string literals,
    /// cross-unit dimensional compatibility, and affine temperature pitfalls.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SharpUnitsLiteralAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "SharpUnits.Usage";

        /// <summary>SU0001: Unknown or malformed unit literal.</summary>
        public static readonly DiagnosticDescriptor UnknownOrInvalidUnit = new DiagnosticDescriptor(
            id: "SU0001",
            title: "Unknown or invalid unit literal",
            messageFormat: "Unit literal '{0}' is invalid: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Validates compile-time unit symbols and unit expressions against the SharpUnits standard unit catalog and SI prefix rules.");

        /// <summary>SU0002: Invalid quantity literal.</summary>
        public static readonly DiagnosticDescriptor InvalidQuantityLiteral = new DiagnosticDescriptor(
            id: "SU0002",
            title: "Invalid quantity literal",
            messageFormat: "Quantity literal '{0}' is invalid: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Validates compile-time quantity literals passed to Quantity.Parse or QuantityTextFormat.Parse.");

        /// <summary>SU0003: Incompatible unit conversion dimensions.</summary>
        public static readonly DiagnosticDescriptor IncompatibleDimensions = new DiagnosticDescriptor(
            id: "SU0003",
            title: "Incompatible unit dimensions",
            messageFormat: "Cannot convert incompatible dimensions from '{0}' to '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Detects compile-time unit conversions between dimensionally incompatible units.");

        /// <summary>SU0004: Affine unit used in multiplicative unit expression.</summary>
        public static readonly DiagnosticDescriptor AffineUnitInExpression = new DiagnosticDescriptor(
            id: "SU0004",
            title: "Affine unit used in multiplicative unit expression",
            messageFormat: "Affine unit cannot be used in multiplicative unit expression '{0}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Offset affine units such as °C and °F cannot participate in unit products, quotients, or powers.");

        /// <summary>SU0005: Adding two absolute affine temperatures.</summary>
        public static readonly DiagnosticDescriptor AbsoluteAffineAddition = new DiagnosticDescriptor(
            id: "SU0005",
            title: "Adding two absolute affine temperatures",
            messageFormat: "Adding two absolute affine temperatures ('{0}' + '{1}') throws at runtime; use QuantityPoint<T> or Quantity.Delta(...)",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Adding two absolute affine temperature points is physically meaningless and throws InvalidOperationException at runtime.");

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                UnknownOrInvalidUnit,
                InvalidQuantityLiteral,
                IncompatibleDimensions,
                AffineUnitInExpression,
                AbsoluteAffineAddition);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            string typeName = method.ContainingType.ToDisplayString();

            if (!typeName.StartsWith("SharpUnits.", System.StringComparison.Ordinal))
                return;

            // 1. Quantity.Parse("...") / QuantityTextFormat.Parse("...") / UnitEnvironment.ParseQuantity("...")
            if ((typeName == "SharpUnits.Quantity" && method.Name == "Parse") ||
                (typeName == "SharpUnits.QuantityTextFormat" && method.Name == "Parse") ||
                (typeName == "SharpUnits.UnitEnvironment" && method.Name == "ParseQuantity"))
            {
                if (TryGetStringArg(invocation, 0, out string? literal, out var argLoc) && literal is not null)
                {
                    if (!CompileTimeUnitValidator.TryValidateQuantityLiteral(literal, out _, out _, out string? err, out bool affineErr))
                    {
                        var descriptor = affineErr ? AffineUnitInExpression : InvalidQuantityLiteral;
                        context.ReportDiagnostic(affineErr
                            ? Diagnostic.Create(descriptor, argLoc, literal)
                            : Diagnostic.Create(descriptor, argLoc, literal, err ?? "invalid syntax"));
                    }
                }
                return;
            }

            // 2. UnitRegistry.Convert(value, "from", "to")
            if (typeName == "SharpUnits.UnitRegistry" && method.Name == "Convert")
            {
                bool hasFrom = TryGetStringArg(invocation, 1, out string? fromLit, out var fromLoc) && fromLit is not null;
                bool hasTo = TryGetStringArg(invocation, 2, out string? toLit, out var toLoc) && toLit is not null;
                bool fromValid = false, toValid = false;
                UnitInfo fromInfo = default, toInfo = default;

                if (hasFrom)
                {
                    fromValid = CompileTimeUnitValidator.TryValidateUnitExpression(fromLit!, allowCompound: false, out fromInfo, out string? err, out _);
                    if (!fromValid)
                        context.ReportDiagnostic(Diagnostic.Create(UnknownOrInvalidUnit, fromLoc, fromLit!, err ?? "unknown unit"));
                }
                if (hasTo)
                {
                    toValid = CompileTimeUnitValidator.TryValidateUnitExpression(toLit!, allowCompound: false, out toInfo, out string? err, out _);
                    if (!toValid)
                        context.ReportDiagnostic(Diagnostic.Create(UnknownOrInvalidUnit, toLoc, toLit!, err ?? "unknown unit"));
                }
                if (fromValid && toValid && fromInfo.Dimension != toInfo.Dimension)
                {
                    context.ReportDiagnostic(Diagnostic.Create(IncompatibleDimensions, invocation.Syntax.GetLocation(), fromLit!, toLit!));
                }
                return;
            }

            // 3. UnitExpressionParser.Parse / ParseSymbolic / UnitEnvironment.ResolveUnit
            if ((typeName == "SharpUnits.UnitExpressionParser" && (method.Name == "Parse" || method.Name == "ParseSymbolic")) ||
                (typeName == "SharpUnits.UnitEnvironment" && method.Name == "ResolveUnit"))
            {
                if (TryGetStringArg(invocation, 0, out string? literal, out var argLoc) && literal is not null)
                {
                    bool allowSingleAffine = typeName == "SharpUnits.UnitEnvironment";
                    if (!CompileTimeUnitValidator.TryValidateUnitExpression(literal, allowCompound: true, out var parsed, out string? err, out bool affineErr))
                    {
                        var descriptor = affineErr ? AffineUnitInExpression : UnknownOrInvalidUnit;
                        context.ReportDiagnostic(affineErr
                            ? Diagnostic.Create(descriptor, argLoc, literal)
                            : Diagnostic.Create(descriptor, argLoc, literal, err ?? "invalid unit expression"));
                    }
                    else if (!allowSingleAffine && parsed.IsAffine)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(AffineUnitInExpression, argLoc, literal));
                    }
                }
                return;
            }

            // 4. UnitRegistry.Resolve("symbol")
            if (typeName == "SharpUnits.UnitRegistry" && method.Name == "Resolve")
            {
                if (TryGetStringArg(invocation, 0, out string? literal, out var argLoc) && literal is not null)
                {
                    if (!CompileTimeUnitValidator.TryValidateUnitExpression(literal, allowCompound: false, out _, out string? err, out _))
                        context.ReportDiagnostic(Diagnostic.Create(UnknownOrInvalidUnit, argLoc, literal, err ?? "unknown unit"));
                }
                return;
            }

            // 5. Quantity.Of(val, "symbol") / Quantity<T>.Of(val, "symbol") / val.WithUnit("symbol") / UnitEnvironment.CreateQuantity(val, "expr")
            if ((IsQuantityType(method.ContainingType) && method.Name == "Of") ||
                (typeName == "SharpUnits.QuantityExtensions" && method.Name == "WithUnit") ||
                (typeName == "SharpUnits.UnitEnvironment" && (method.Name == "CreateQuantity" || method.Name == "CreateUnifiedQuantity")))
            {
                bool allowCompound = typeName == "SharpUnits.UnitEnvironment";
                if (TryGetStringArg(invocation, 1, out string? literal, out var argLoc) && literal is not null)
                {
                    if (!CompileTimeUnitValidator.TryValidateUnitExpression(literal, allowCompound, out _, out string? err, out bool affineErr))
                    {
                        var descriptor = affineErr ? AffineUnitInExpression : UnknownOrInvalidUnit;
                        context.ReportDiagnostic(affineErr
                            ? Diagnostic.Create(descriptor, argLoc, literal)
                            : Diagnostic.Create(descriptor, argLoc, literal, err ?? "unknown unit"));
                    }
                }
                return;
            }

            // 6. q.In("target") / q.AsValueIn("target") / q.In(Units.X) / env.Convert(q, "target")
            if ((IsQuantityType(method.ContainingType) && (method.Name == "In" || method.Name == "AsValueIn")) ||
                (typeName == "SharpUnits.UnitEnvironment" && method.Name == "Convert"))
            {
                bool isEnvConvert = typeName == "SharpUnits.UnitEnvironment";
                int targetArgIndex = isEnvConvert ? 1 : 0;
                IOperation? sourceOp = isEnvConvert
                    ? (invocation.Arguments.Length > 0 ? invocation.Arguments[0].Value : null)
                    : invocation.Instance;

                UnitInfo targetInfo = default;
                string? targetLabel = null;
                bool hasTarget = false;

                if (TryGetStringArg(invocation, targetArgIndex, out string? targetLit, out var targetLoc) && targetLit is not null)
                {
                    if (!CompileTimeUnitValidator.TryValidateUnitExpression(targetLit, allowCompound: isEnvConvert, out targetInfo, out string? err, out bool affineErr))
                    {
                        var descriptor = affineErr ? AffineUnitInExpression : UnknownOrInvalidUnit;
                        context.ReportDiagnostic(affineErr
                            ? Diagnostic.Create(descriptor, targetLoc, targetLit)
                            : Diagnostic.Create(descriptor, targetLoc, targetLit, err ?? "unknown unit"));
                        return;
                    }
                    targetLabel = targetLit;
                    hasTarget = true;
                }
                else if (invocation.Arguments.Length > targetArgIndex &&
                         TryInferUnitFromOperation(invocation.Arguments[targetArgIndex].Value, out targetInfo))
                {
                    targetLabel = targetInfo.Symbol;
                    hasTarget = true;
                }

                if (hasTarget && sourceOp is not null &&
                    TryInferQuantityUnit(sourceOp, out var sourceInfo, out _) &&
                    sourceInfo.Dimension != targetInfo.Dimension)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        IncompatibleDimensions,
                        invocation.Syntax.GetLocation(),
                        sourceInfo.Symbol,
                        targetLabel!));
                }
            }
        }

        private static void AnalyzeBinary(OperationAnalysisContext context)
        {
            var binary = (IBinaryOperation)context.Operation;
            if (binary.OperatorKind != BinaryOperatorKind.Add) return;
            if (binary.OperatorMethod is null) return;
            if (binary.OperatorMethod.ContainingType.ToDisplayString() != "SharpUnits.Quantity") return;

            if (TryInferQuantityUnit(binary.LeftOperand, out var leftInfo, out bool leftRel) &&
                TryInferQuantityUnit(binary.RightOperand, out var rightInfo, out bool rightRel) &&
                leftInfo.IsAffine && !leftRel &&
                rightInfo.IsAffine && !rightRel)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AbsoluteAffineAddition,
                    binary.Syntax.GetLocation(),
                    leftInfo.Symbol,
                    rightInfo.Symbol));
            }
        }

        private static bool TryInferQuantityUnit(IOperation? op, out UnitInfo info, out bool isRelative)
        {
            info = default;
            isRelative = false;
            op = UnwrapConversion(op);
            if (op is not IInvocationOperation inv) return false;

            var method = inv.TargetMethod;
            string typeName = method.ContainingType.ToDisplayString();

            if (typeName == "SharpUnits.Quantity" && method.Name == "Parse" &&
                TryGetStringArg(inv, 0, out string? lit, out _) && lit is not null)
            {
                return CompileTimeUnitValidator.TryValidateQuantityLiteral(lit, out info, out isRelative, out _, out _);
            }

            if (typeName == "SharpUnits.Quantity" && method.Name == "Delta" && inv.Arguments.Length >= 2)
            {
                isRelative = true;
                return TryInferUnitFromOperation(inv.Arguments[1].Value, out info);
            }

            if ((IsQuantityType(method.ContainingType) && method.Name == "Of") ||
                (typeName == "SharpUnits.QuantityExtensions" && method.Name == "WithUnit"))
            {
                if (TryGetStringArg(inv, 1, out string? uLit, out _) && uLit is not null)
                    return CompileTimeUnitValidator.TryValidateUnitExpression(uLit, allowCompound: false, out info, out _, out _);
                if (inv.Arguments.Length >= 2)
                    return TryInferUnitFromOperation(inv.Arguments[1].Value, out info);
            }

            if (typeName == "SharpUnits.SiQuantities")
            {
                return method.Name switch
                {
                    "Volts" => CompileTimeUnitValidator.TryResolveSingle("V", out info),
                    "Ohms" => CompileTimeUnitValidator.TryResolveSingle("Ω", out info),
                    "Amperes" => CompileTimeUnitValidator.TryResolveSingle("A", out info),
                    "Watts" => CompileTimeUnitValidator.TryResolveSingle("W", out info),
                    "Hertz" => CompileTimeUnitValidator.TryResolveSingle("Hz", out info),
                    "Seconds" => CompileTimeUnitValidator.TryResolveSingle("s", out info),
                    "Meters" => CompileTimeUnitValidator.TryResolveSingle("m", out info),
                    "Kilograms" => CompileTimeUnitValidator.TryResolveSingle("kg", out info),
                    "Kelvin" => CompileTimeUnitValidator.TryResolveSingle("K", out info),
                    _ => false
                };
            }

            return false;
        }

        private static bool TryInferUnitFromOperation(IOperation? op, out UnitInfo info)
        {
            info = default;
            op = UnwrapConversion(op);
            if (op is IPropertyReferenceOperation prop &&
                prop.Property.ContainingType.ToDisplayString() == "SharpUnits.Units")
            {
                return CompileTimeUnitValidator.TryResolveUnitsProperty(prop.Property.Name, out info);
            }
            return false;
        }

        private static IOperation? UnwrapConversion(IOperation? op)
        {
            while (op is IConversionOperation conv) op = conv.Operand;
            return op;
        }

        private static bool IsQuantityType(INamedTypeSymbol type) =>
            type.ContainingNamespace?.ToDisplayString() == "SharpUnits" && type.Name == "Quantity";

        private static bool TryGetStringArg(IInvocationOperation invocation, int index, out string? value, out Location location)
        {
            if (invocation.Arguments.Length > index)
            {
                var argVal = invocation.Arguments[index].Value;
                if (argVal.Type?.SpecialType == SpecialType.System_String &&
                    argVal.ConstantValue.HasValue &&
                    argVal.ConstantValue.Value is string s)
                {
                    value = s;
                    location = invocation.Arguments[index].Syntax.GetLocation();
                    return true;
                }
            }
            value = null;
            location = invocation.Syntax.GetLocation();
            return false;
        }
    }
}
