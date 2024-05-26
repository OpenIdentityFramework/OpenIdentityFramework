using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace OpenIdentityFramework.Extensions;

public static class FormCollectionExtensions
{
    public static IReadOnlyDictionary<string, StringValues> AsReadOnlyDictionary(this IFormCollection formCollection)
    {
        ArgumentNullException.ThrowIfNull(formCollection);
        return new FormCollectionReadOnlyDictionary(formCollection);
    }

    private sealed class FormCollectionReadOnlyDictionary : IReadOnlyDictionary<string, StringValues>
    {
        private readonly IFormCollection _formCollection;

        public FormCollectionReadOnlyDictionary(IFormCollection formCollection)
        {
            _formCollection = formCollection;
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return _formCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _formCollection).GetEnumerator();
        }

        public int Count => _formCollection.Count;

        public bool ContainsKey(string key)
        {
            return _formCollection.ContainsKey(key);
        }

        public bool TryGetValue(string key, out StringValues value)
        {
            return _formCollection.TryGetValue(key, out value);
        }

        public StringValues this[string key] => _formCollection[key];

        public IEnumerable<string> Keys => _formCollection.Keys;
        public IEnumerable<StringValues> Values => throw new NotImplementedException();
    }
}