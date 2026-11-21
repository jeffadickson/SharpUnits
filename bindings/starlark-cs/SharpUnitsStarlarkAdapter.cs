// Drop-in adapter for jeffadickson/StarLark-cs (net11.0).
// Reference SharpUnits + SharpUnits.Interop in your StarLark-cs host project and include this file.

using System.Numerics;
using SharpUnits;
using SharpUnits.Interop.Starlark;
using Starlark.Eval;
using Starlark.Syntax;

namespace SharpUnits.Starlark;

/// <summary>Extension methods registering SharpUnits as first-class thread-safe Starlark values.</summary>
public static class StarlarkUnitsExtensions
{
    /// <summary>
    /// Defines the <c>units</c> module (<c>units.unit</c>, <c>units.qty</c>, <c>units.parse</c>, <c>units.point</c>, <c>units.series</c>)
    /// on <paramref name="interpreter"/> with an isolated deterministic <see cref="UnitEnvironment"/>.
    /// </summary>
    public static Interpreter AddSharpUnits(this Interpreter interpreter, UnitEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        var module = new StarUnitsModule(
            environment ?? UnitEnvironment.CreateStandard(),
            onAllocateBytes: bytes => MemoryBudget.Check(bytes));
        interpreter.DefineValue("units", new StarHostAdapter(module));
        return interpreter;
    }
}

/// <summary>Adapts any immutable <see cref="IStarUnitHostValue"/> to <see cref="IThreadSafeHostValue"/>.</summary>
public sealed class StarHostAdapter : IThreadSafeHostValue
{
    private readonly IStarUnitHostValue inner;

    /// <summary>Wraps a SharpUnits Starlark host value.</summary>
    public StarHostAdapter(IStarUnitHostValue inner) =>
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>Exposes the underlying SharpUnits host value.</summary>
    public IStarUnitHostValue Inner => inner;

    /// <inheritdoc/>
    public string TypeName => inner.TypeName;

    /// <inheritdoc/>
    public string Repr() => inner.Repr();

    /// <inheritdoc/>
    public bool Truthy() => inner.Truthy();

    /// <inheritdoc/>
    public IEnumerable<string> Attributes() => inner.Attributes();

    /// <inheritdoc/>
    public bool TryGetAttribute(string name, out object? value)
    {
        if (!inner.TryGetAttribute(name, out var raw))
        {
            value = null;
            return false;
        }
        value = Wrap(raw);
        return true;
    }

    /// <inheritdoc/>
    public bool TryBinary(BinOp op, object? other, bool reflected, out object? result)
    {
        StarBinOp mapped;
        switch (op)
        {
            case BinOp.Add: mapped = StarBinOp.Add; break;
            case BinOp.Subtract: mapped = StarBinOp.Subtract; break;
            case BinOp.Multiply: mapped = StarBinOp.Multiply; break;
            case BinOp.Divide: mapped = StarBinOp.Divide; break;
            default: result = null; return false;
        }
        try
        {
            object? unwrapped = other is StarHostAdapter a ? a.inner : other;
            if (!inner.TryBinary(mapped, unwrapped, reflected, out var raw))
            {
                result = null;
                return false;
            }
            result = Wrap(raw);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or KeyNotFoundException or DivideByZeroException or ArgumentException)
        {
            throw new StarlarkException(ex.Message);
        }
    }

    /// <inheritdoc/>
    public bool TryNegate(out object? result)
    {
        if (!inner.TryNegate(out var raw)) { result = null; return false; }
        result = Wrap(raw);
        return true;
    }

    /// <inheritdoc/>
    public bool EqualsValue(object? other) =>
        inner.EqualsValue(other is StarHostAdapter a ? a.inner : other);

    /// <inheritdoc/>
    public int HashValue() => inner.HashValue();

    /// <inheritdoc/>
    public int? CompareTo(object? other) =>
        inner.CompareTo(other is StarHostAdapter a ? a.inner : other);

    /// <inheritdoc/>
    public BigInteger? Length() => inner.Length();

    /// <inheritdoc/>
    public bool TryIndex(object? index, out object? result)
    {
        if (!inner.TryIndex(index, out var raw)) { result = null; return false; }
        result = Wrap(raw);
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<object?>? Iterate()
    {
        var seq = inner.Iterate();
        if (seq is null) return null;
        return seq.Select(Wrap);
    }

    /// <inheritdoc/>
    public void Freeze() { }

    private static object? Wrap(object? raw) => raw switch
    {
        IStarUnitHostValue hv => new StarHostAdapter(hv),
        _ => raw
    };
}
