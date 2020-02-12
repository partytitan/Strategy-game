using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ListPool<T>
{
    static Stack<List<T>> _stack = new Stack<List<T>>();

    public static List<T> Get()
    {
        if (_stack.Count > 0)
        {
            return _stack.Pop();
        }
        return new List<T>();
    }

    public static void Add(List<T> list)
    {
        list.Clear();
        _stack.Push(list);
    }
}