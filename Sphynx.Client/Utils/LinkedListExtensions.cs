namespace Sphynx.Client.Utils
{
    internal static class LinkedListExtensions
    {
        public static LinkedListNode<T>? GetNode<T>(this LinkedList<T> list, int i)
        {
            if (i >= list.Count || -i > list.Count) return null;

            LinkedListNode<T>? it = null;
    
            if (i >= 0)
            {
                if (i > list.Count / 2)
                {
                    it = list.Last!;
                    for (int n = 0; n < list.Count - i - 1; n++, it = it.Previous!) { }
                }
                else
                {
                    it = list.First!;
                    for (int n = 0; n < i; n++, it = it.Next!) { }
                }
            }

            return it ?? GetNode(list, list.Count - (-i - 1))!;
        }
    }
}
