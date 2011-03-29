// ReSharper disable CheckNamespace
using System.Linq;

namespace System.Collections.Generic
// ReSharper restore CheckNamespace
{
    public class SortedList<TKey, TValue> : Dictionary<TKey, TValue>
    {
        #region INSTANCE MEMBERS

        private readonly IComparer<TKey> mComparer;

        #endregion

        #region CONSTRUCTORS

        public SortedList(IComparer<TKey> iComparer)
        {
            mComparer = iComparer;
        }

        #endregion

        #region PROPERTIES

        public new IList<TValue> Values
        {
            get { return base.Values.ToList(); }
        }

        #endregion
        
        #region METHODS

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            this.OrderBy(kv => kv.Key, mComparer);
        }

        public void RemoveAt(int i)
        {
            Remove(this.ElementAt(i).Key);
        }

        #endregion
    }
}
