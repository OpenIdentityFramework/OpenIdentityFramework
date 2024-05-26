using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Extensions;

namespace OpenIdentityFramework.Tests.Unit.Extensions;

public class FormCollectionExtensionsTests
{
    [Test]
    public void AsReadOnlyDictionary_ThrowsArgumentNullException_WhenFormCollectionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => FormCollectionExtensions.AsReadOnlyDictionary(null!));
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsEmptyDictionary_WhenFormCollectionContainsNullDictionary()
    {
        var actual = new FormCollection(new()).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsEmptyDictionary_WhenFormCollectionContainsEmptyDictionary()
    {
        var actual = new FormCollection(new()).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsDictionary_WhenFormCollectionIsNormal()
    {
        var input = new Dictionary<string, string>
        {
            { "a", "1" },
            { "b", "2" }
        };
        var expected = input.ToDictionary(
            static x => x.Key,
            static x => new StringValues(x.Value));

        var actual = new FormCollection(expected).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsDictionary_WhenFormCollectionContainsEmptyOrNullValues()
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

        var actual = new FormCollection(expected).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void AsReadOnlyDictionary_ReturnsDictionary_WhenFormCollectionContainsMultipleValues()
    {
        var expected = new Dictionary<string, StringValues>
        {
            { "a", new StringValues("1") },
            { "b", new StringValues("2") },
            { "c", new(["3", "4"]) },
            { "d", new(["5", "6", "7"]) }
        };

        var actual = new FormCollection(expected).AsReadOnlyDictionary();

        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Is.EquivalentTo(expected));
    }
}