using System.Reflection;
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Structural .NET analogues of JSR 385 TCK 4.2.5 public method requirements.</summary>
/// <remarks>Java method names and CLR operators differ; this suite documents explicit .NET equivalents.</remarks>
public sealed class TckDimensionApiSurfaceTests
{
 [Fact] public void Tck425A1_EqualsIsPublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("Equals",new[]{typeof(object)}));
  Assert.NotNull(typeof(RationalDimension).GetMethod("Equals",new[]{typeof(object)}));
 }
 [Fact] public void Tck425A2_HashCodeIsPublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("GetHashCode",Type.EmptyTypes));
  Assert.NotNull(typeof(RationalDimension).GetMethod("GetHashCode",Type.EmptyTypes));
 }
 [Fact] public void Tck425A3_MultiplyOperatorIsPublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("op_Addition",BindingFlags.Public|BindingFlags.Static));
  Assert.NotNull(typeof(RationalDimension).GetMethod("op_Addition",BindingFlags.Public|BindingFlags.Static));
 }
 [Fact] public void Tck425A4_DivideOperatorIsPublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("op_Subtraction",BindingFlags.Public|BindingFlags.Static));
  Assert.NotNull(typeof(RationalDimension).GetMethod("op_Subtraction",BindingFlags.Public|BindingFlags.Static));
 }
 [Fact] public void Tck425A5_RootIsPublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("Root",new[]{typeof(int)}));
  Assert.NotNull(typeof(RationalDimension).GetMethod("Root",new[]{typeof(int)}));
 }
 [Fact] public void Tck425A6_PowIsPublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("Pow",new[]{typeof(int)}));
  Assert.NotNull(typeof(RationalDimension).GetMethod("Pow",new[]{typeof(int)}));
 }
 [Fact] public void Tck425A7_BaseDimensionsArePublic()
 {
  Assert.NotNull(typeof(Dimension).GetMethod("GetBaseDimensions",Type.EmptyTypes));
  Assert.NotNull(typeof(RationalDimension).GetMethod("GetBaseDimensions",Type.EmptyTypes));
 }
}
