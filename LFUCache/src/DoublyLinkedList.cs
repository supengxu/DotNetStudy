namespace LFUCache;

internal sealed class DoublyLinkedList<TKey, TValue>
{
    public LFUNode<TKey, TValue>? Head { get; private set; }
    public LFUNode<TKey, TValue>? Tail { get; private set; }
    public int Count { get; private set; }
    
    public DoublyLinkedList()
    {
        Head = null;
        Tail = null;
        Count = 0;
    }
    
    public void AddToHead(LFUNode<TKey, TValue> node)
    {
        if (Head == null)
        {
            Head = node;
            Tail = node;
            node.Prev = null;
            node.Next = null;
        }
        else
        {
            node.Prev = null;
            node.Next = Head;
            Head.Prev = node;
            Head = node;
        }
        Count++;
    }
    
    public void Remove(LFUNode<TKey, TValue> node)
    {
        if (node.Prev != null)
        {
            node.Prev.Next = node.Next;
        }
        else
        {
            Head = node.Next;
        }
        
        if (node.Next != null)
        {
            node.Next.Prev = node.Prev;
        }
        else
        {
            Tail = node.Prev;
        }
        
        node.Prev = null;
        node.Next = null;
        Count--;
    }
    
    public LFUNode<TKey, TValue>? RemoveFromTail()
    {
        if (Tail == null)
        {
            return null;
        }
        
        var node = Tail;
        Remove(node);
        return node;
    }
}