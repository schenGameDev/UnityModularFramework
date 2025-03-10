using System.Collections.Generic;

public class CircularList<T> : List<T>
{
    public CircularList() : base() {}
    public CircularList(IEnumerable<T> collection) : base(collection) {}

    public new T this[int index]
    {
        get { return base[GetIndex(index,Count)]; }
        set { base[GetIndex(index,Count)] = value; }
    }

    public T this[T item, int distance]
    {
        get
        {
            var index = IndexOf(item);
            return this[index + distance];
        }
        set
        {
            var index = IndexOf(item);
            this[index + distance] = value;
        }
    }

    private static int GetIndex(int index, int count) {
        index %= count;
        if(index<0) index+=count;
        return index;
    }

    public List<T> GetRangeBetween(int start, int end) {
        bool isLoop = start > end;
        List<T> list = this.GetRange(start, isLoop? (this.Count-start) : (end - start + 1));
        if(isLoop) {
            list.AddRange(this.GetRange(0, end + 1));
        }
        return list;
    }
}