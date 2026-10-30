using System.Collections.Concurrent;
using System.Text.Json;
using FsCheck.Xunit;
using Xunit;
namespace SharpUnits.Tests;

/// <summary>Bounded hash-quality engineering check, separate from Java conformance.</summary>
[Trait("WorkPackage","1")]
public sealed class MutationDebtHashQualityTests
{
    private static readonly ConcurrentQueue<object> Trials=new();
    static MutationDebtHashQualityTests()
    {
        AppDomain.CurrentDomain.ProcessExit+=(_,_)=>File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"hash-quality-trials.json"),
            JsonSerializer.Serialize(new{seed="385,905",maxTests=100,criterion="32 distinct converters; at least8 buckets",qualification="Bounded engineering criterion; enclosing test exit required; failures may include shrinking attempts",trials=Trials.ToArray()},new JsonSerializerOptions{WriteIndented=true}));
    }
    private sealed class CountingComparer(bool constant) : IEqualityComparer<JavaUnitConverterAdapter>
    {
        internal int Comparisons;
        public bool Equals(JavaUnitConverterAdapter? a,JavaUnitConverterAdapter? b){Comparisons++;return EqualityComparer<JavaUnitConverterAdapter>.Default.Equals(a,b);}
        public int GetHashCode(JavaUnitConverterAdapter value)=>constant?1:value.GetHashCode();
    }
    [Property(MaxTest=100,Replay="385,905")]
    public bool DistinctConverterWorkloadAvoidsCatastrophicallyConstantHashing(int input)
    {
        var firstScale=1+(input&1023);
        var converters=Enumerable.Range(firstScale,32).Select(value=>JavaUnitConverterAdapter.Multiply(JavaNumber.Integer(value))).ToArray();
        var distinct=converters.Distinct().Count();var buckets=converters.Select(value=>value.GetHashCode()).Distinct().Count();
        int Measure(bool constant)
        {
            var comparer=new CountingComparer(constant);var dictionary=new Dictionary<JavaUnitConverterAdapter,int>(comparer);
            for(var i=0;i<converters.Length;i++)dictionary.Add(converters[i],i);
            for(var i=0;i<converters.Length;i++)Assert.Equal(i,dictionary[converters[i]]);
            return comparer.Comparisons;
        }
        var normalComparisons=Measure(false);var constantComparisons=Measure(true);
        Trials.Enqueue(new{input,firstScale,converterCount=32,distinct,hashBuckets=buckets,normalComparisons,constantComparisons});
        return distinct==32&&buckets>=8;
    }
}
