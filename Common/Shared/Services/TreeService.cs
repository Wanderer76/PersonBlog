using Shared.Models;

namespace Shared.Services
{
    public static class TreeService
    {
        public static IEnumerable<TreeItem<T>> ToTree<T, K>(this IEnumerable<T> collection, Func<T, K> idSelector, Func<T, K> parentSelector, K rootId = default(K))
        {
            foreach (var c in collection.Where(c => EqualityComparer<K>.Default.Equals(parentSelector(c), rootId)))
            {
                yield return new TreeItem<T>
                {
                    Item = c,
                    Children = collection.ToTree(idSelector, parentSelector, idSelector(c))
                };
            }
        }
    }
}
