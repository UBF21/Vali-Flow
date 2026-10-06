using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Firestore.Extensions;
using Vali_Flow.NoSql.Firestore.Tests.Models;

namespace Vali_Flow.NoSql.Firestore.Tests.Extensions;

/// <summary>Tests for <see cref="ValiFlowFirestoreExtensions"/>.</summary>
public sealed class ValiFlowFirestoreExtensionsTests
{
    [Fact]
    public void ToFirestore_ValiFlow_SimpleCondition_ReturnsFilter()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.IsActive, true);

        var filter = flow.ToFirestore();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void ToFirestore_ValiFlow_CombinedConditions_ReturnsFilter()
    {
        var flow = new ValiFlow<TestDocument>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Age, 18);

        var filter = flow.ToFirestore();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void ToFirestore_NullValiFlow_ThrowsArgumentNullException()
    {
        ValiFlow<TestDocument>? flow = null;

        Action act = () => flow!.ToFirestore();

        act.Should().Throw<ArgumentNullException>().WithParameterName("flow");
    }

    [Fact]
    public void ToFirestore_Expression_ReturnsFilter()
    {
        Expression<Func<TestDocument, bool>> expression = x => x.Age > 18;

        var filter = expression.ToFirestore();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void ToFirestore_NullExpression_ThrowsArgumentNullException()
    {
        Expression<Func<TestDocument, bool>>? expression = null;

        Action act = () => expression!.ToFirestore();

        act.Should().Throw<ArgumentNullException>().WithParameterName("expression");
    }

    [Fact]
    public void ToFirestore_CustomConverter_IsForwardedToTranslator()
    {
        var flow = new ValiFlow<TestDocument>().EqualTo(x => x.Price, 9.99m);

        var filter = flow.ToFirestore(v => v is decimal d ? (double)d : null);

        filter.Should().NotBeNull();
    }
}
