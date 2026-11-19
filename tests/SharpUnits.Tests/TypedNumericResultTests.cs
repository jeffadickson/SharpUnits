using System.Numerics;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Recovery contracts independent of legacy English exception wording.</summary>
[Trait("WorkPackage", "1")]
public sealed class TypedNumericResultTests
{
    private static NumericError Error<T>(NumericResult<T> result) where T : notnull =>
        result.Match(_ => throw new InvalidOperationException("Expected failure"), error => error);

    [Theory]
    [InlineData(null, NumericErrorCode.NullInput, null)]
    [InlineData("", NumericErrorCode.TokenLength, null)]
    [InlineData(" ", NumericErrorCode.InvalidDigit, 0)]
    [InlineData("1.2.3", NumericErrorCode.InvalidDigit, 3)]
    [InlineData("+", NumericErrorCode.MissingDigits, 1)]
    [InlineData(".", NumericErrorCode.MissingDigits, 1)]
    [InlineData("e2", NumericErrorCode.MissingDigits, 0)]
    [InlineData("1e", NumericErrorCode.InvalidExponent, 2)]
    [InlineData("1e+", NumericErrorCode.InvalidExponent, 2)]
    [InlineData("1e2147483648", NumericErrorCode.InvalidExponent, 2)]
    [InlineData("1E4097", NumericErrorCode.ScaleBudget, null)]
    [InlineData("1E-4097", NumericErrorCode.ScaleBudget, null)]
    public void ParseReturnsActionableFailure(string? text, NumericErrorCode code, int? position)
    {
        var result = JavaNumber.ParseDecimal(text);
        Assert.False(result.IsSuccess);
        Assert.Equal(code, Error(result).Code);
        Assert.Equal("text", Error(result).Parameter);
        Assert.Equal(position, Error(result).Position);
    }

    [Theory]
    [InlineData("1.25", 5, 4)]
    [InlineData("-.125e1", -5, 4)]
    [InlineData("+12e2", 1200, 1)]
    public void ParseReturnsIndependentExactValue(string text, int numerator, int denominator)
    {
        var result = JavaNumber.ParseDecimal(text);
        Assert.True(result.IsSuccess);
        Assert.Equal(new Rational(numerator, denominator), result.Match(v => v.Fraction, _ => default));
    }

    [Theory]
    [InlineData(4096)]
    [InlineData(-4096)]
    public void ScaleBoundaryIsInclusive(int exponent)
    {
        var result = JavaNumber.ParseDecimal("1e" + exponent);
        var power = BigInteger.Pow(10, Math.Abs(exponent));
        Assert.Equal(exponent > 0 ? new Rational(power, 1) : new Rational(1, power),
            result.Match(v => v.Fraction, _ => default));
    }

    [Fact]
    public void LengthAndNumericBudgetsHaveDifferentRecoveryCodes()
    {
        Assert.Equal(NumericErrorCode.TokenLength, Error(JavaNumber.ParseDecimal(new string('1', 4097))).Code);
        var text = new string('9', 4089) + "e4096";
        Assert.Equal(NumericErrorCode.NumericBudget, Error(JavaNumber.ParseDecimal(text)).Code);
        var legacy = Assert.Throws<ArgumentOutOfRangeException>(() => JavaNumber.Decimal(text));
        Assert.Equal("value", legacy.ParamName);
        Assert.Equal("text", Error(JavaNumber.ParseDecimal(text)).Parameter);
    }

    [Fact]
    public void ConversionReportsInputFailureWithoutCallingUserCode()
    {
        Assert.Equal(NumericErrorCode.NullInput, Error(JavaNumber.ConvertNumeric(null)).Code);
        Assert.Equal(NumericErrorCode.UnsupportedNumericType, Error(JavaNumber.ConvertNumeric(new HostileObject())).Code);
        Assert.Equal(NumericErrorCode.ZeroDenominator, Error(JavaNumber.ConvertNumeric(default(Rational))).Code);
        var error = Error(JavaNumber.ConvertNumeric(BigInteger.One << 16384));
        Assert.Equal(NumericErrorCode.NumericBudget, error.Code);
        Assert.Equal("value", error.Parameter);
        Assert.Null(error.Position);
        Assert.Equal(NumericErrorCode.NumericBudget,
            Error(JavaNumber.ConvertNumeric(new Rational(1, BigInteger.One << 16384))).Code);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonfiniteConversionIsExpectedFailure(double value)
    {
        Assert.Equal(NumericErrorCode.NonFinite, Error(JavaNumber.ConvertNumeric(value)).Code);
        Assert.Equal(NumericErrorCode.NonFinite, Error(JavaNumber.ConvertNumeric((float)value)).Code);
        Assert.Throws<ArgumentOutOfRangeException>(() => JavaNumber.FromNumeric(value));
    }

    [Fact]
    public void BindForwardsOriginalErrorWithoutRunningDownstream()
    {
        var failure = JavaNumber.ParseDecimal("bad");
        var first = Error(failure);
        var result = failure.Bind<string>(_ => throw new Exception("Must not execute"));
        Assert.Same(first, Error(result));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void BindExecutesSuccessAndPropagatesUnexpectedFaultsAndCancellation()
    {
        var input = JavaNumber.ParseDecimal("1.25");
        var result = input.Bind(v => NumericResult<Rational>.Success(v.Fraction * 4));
        Assert.Equal(new Rational(5, 1), result.Match(v => v, _ => default));
        var fault = new InvalidOperationException("unexpected");
        Assert.Same(fault, Assert.Throws<InvalidOperationException>(() => input.Bind<string>(_ => throw fault)));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = Assert.Throws<OperationCanceledException>(() => input.Bind<string>(_ =>
        { cancellation.Token.ThrowIfCancellationRequested(); return NumericResult<string>.Success("unreachable"); }));
        Assert.Equal(cancellation.Token, cancelled.CancellationToken);
    }

    [Fact]
    public void ResultCannotRepresentNullSuccessOrFailureOrReturnNullFromBind()
    {
        Assert.Throws<ArgumentNullException>(() => NumericResult<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => NumericResult<string>.Failure(null!));
        var success = NumericResult<string>.Success("ok");
        Assert.Throws<ArgumentNullException>(() => success.Bind<string>(null!));
        Assert.Throws<InvalidOperationException>(() => success.Bind<string>(_ => null!));
        Assert.Throws<ArgumentNullException>(() => success.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => success.Match(_ => 0, null!));
        var failure = NumericResult<string>.Failure(new NumericError(NumericErrorCode.InvalidDigit, "text", 0));
        Assert.Throws<ArgumentNullException>(() => failure.Bind<string>(null!));
        Assert.Throws<ArgumentNullException>(() => failure.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => failure.Match(_ => 0, null!));
        Assert.Equal("InvalidDigit", failure.Match(_ => "wrong", e => e.Code.ToString()));
    }

    [Fact]
    public void ErrorMetadataRejectsInvalidConstruction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumericError((NumericErrorCode)0, "text"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumericError((NumericErrorCode)999, "text"));
        Assert.Throws<ArgumentException>(() => new NumericError(NumericErrorCode.InvalidDigit, ""));
        Assert.Throws<ArgumentNullException>(() => new NumericError(NumericErrorCode.InvalidDigit, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumericError(NumericErrorCode.InvalidDigit, "text", -1));
    }

    [Fact]
    public void NumericConversionSupportsEveryExistingInputFamily()
    {
        object[] values = { (sbyte)-2, (byte)2, (short)-2, (ushort)2, -2, 2u, -2L, 2ul,
            BigInteger.Parse("123456789012345678901234567890"), new Rational(1, 3),
            0.125m, 0.125f, 0.125d, JavaNumber.One };
        foreach (var value in values)
        {
            var actual = JavaNumber.ConvertNumeric(value).Match(n => n, _ => throw new Exception("Unexpected failure"));
            Assert.Equal(JavaNumber.FromNumeric(value), actual);
        }
        Assert.Same(JavaNumber.One, JavaNumber.ConvertNumeric(JavaNumber.One).Match(n => n, _ => null!));
        Assert.True(JavaNumber.ConvertNumeric(BigInteger.One << 16383).IsSuccess);
        Assert.True(JavaNumber.ConvertNumeric(new Rational(1, BigInteger.One << 16383)).IsSuccess);
    }

    [Fact]
    public void LegacyFailureTypesAndParameterNamesRemainCompatible()
    {
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => JavaNumber.Decimal(null!)).ParamName);
        Assert.Throws<FormatException>(() => JavaNumber.Decimal("1e"));
        Assert.Equal("text", Assert.Throws<ArgumentOutOfRangeException>(() => JavaNumber.Decimal("1e4097")).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentException>(() => JavaNumber.FromNumeric("1")).ParamName);
        Assert.Throws<DivideByZeroException>(() => JavaNumber.FromNumeric(default(Rational)));
    }

    [Property(MaxTest = 300, Replay = "385,1101")]
    public bool InvalidSuffixIsLocatedAndNeverReachesDownstream(int input)
    {
        var text = input.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var result = JavaNumber.ParseDecimal(text + "!").Bind<string>(_ => throw new Exception("unreachable"));
        var error = Error(result);
        return error.Code == NumericErrorCode.InvalidDigit && error.Position == text.Length;
    }

    [Property(MaxTest = 300, Replay = "385,1103")]
    public bool BoxedLongRetainsExactValueAndPromotion(long value)
    {
        var result = JavaNumber.ConvertNumeric(value);
        return result.Match(n => n.Fraction == new Rational(value, 1)
            && n.Multiply(JavaNumber.Decimal("0.1234567890123456789012345678901234")).Fraction
            == new Rational((BigInteger)value * BigInteger.Parse("1234567890123456789012345678901234"), BigInteger.Pow(10, 34)), _ => false);
    }

    private sealed class HostileObject
    {
        public override string ToString() => throw new Exception("Do not inspect caller objects");
    }
}
