using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Extensions;

namespace OpenIdentityFramework.Tests.Unit.Extensions;

public class QueryCollectionExtensionsTests
{
    [Test]
    public void AsReadOnlyDictionary_ThrowsArgumentNullException_WhenQueryCollectionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => QueryCollectionExtensions.AsReadOnlyDictionary(null!));
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsEmptyDictionary_WhenQueryCollectionContainsNullDictionary()
    {
        var actual = new QueryCollection((Dictionary<string, StringValues>) null!).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsEmptyDictionary_WhenQueryCollectionContainsEmptyDictionary()
    {
        var actual = new QueryCollection(new Dictionary<string, StringValues>()).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsDictionary_WhenQueryCollectionIsNormal()
    {
        var input = new Dictionary<string, string>
        {
            { "a", "1" },
            { "b", "2" }
        };
        var expected = input.ToDictionary(
            static x => x.Key,
            static x => new StringValues(x.Value));

        var actual = new QueryCollection(expected).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsDictionary_WhenQueryCollectionContainsEmptyOrNullValues()
    {
        var input = new Dictionary<string, string>
        {
            { "a", "1" },
            { "b", "2" }
        };
        var expected = input.ToDictionary(
            static x => x.Key,
            static x => new StringValues(x.Value));
        expected.Add("c", new((string?) null));
        expected.Add("d", new(string.Empty));

        var actual = new QueryCollection(expected).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsDictionary_WhenQueryCollectionContainsMultipleValues()
    {
        var expected = new Dictionary<string, StringValues>
        {
            { "a", new StringValues("1") },
            { "b", new StringValues("2") },
            { "c", new(["3", "4"]) },
            { "d", new(["5", "6", "7"]) }
        };

        var actual = new QueryCollection(expected).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.EquivalentTo(expected));
    }
}