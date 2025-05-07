using System.Linq.Expressions;
using Gml.Core.Services.Storage;
using NUnit.Framework;

namespace GmlCore.Tests;

[TestFixture]
public class ReplaceParameterVisitorTests
{
    private ParameterExpression _oldParameter;
    private ParameterExpression _newParameter;
    private ReplaceParameterVisitor _visitor;

    [SetUp]
    public void Setup()
    {
        _oldParameter = Expression.Parameter(typeof(int), "oldParam");
        _newParameter = Expression.Parameter(typeof(int), "newParam");
        _visitor = new ReplaceParameterVisitor(_oldParameter, _newParameter);
    }

    [Test]
    public void VisitParameter_OldParameter_ReturnsNewParameter()
    {
        var result = _visitor.Visit(_oldParameter);
        Assert.That(result, Is.EqualTo(_newParameter));
    }

    [Test]
    public void VisitParameter_DifferentParameter_ReturnsSameParameter()
    {
        var differentParam = Expression.Parameter(typeof(string), "different");
        var result = _visitor.Visit(differentParam);
        Assert.That(result, Is.EqualTo(differentParam));
    }
}
