using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Transactions;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(x => x.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(x => x.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (Root == null)
        {
            Root = CreateNode(key, value);
            Count = 1;
            OnNodeAdded(Root);
            return;
        }

        TNode? current = Root;
        TNode? parent = null;
        int comparison = 0; // результат сравнения

        while (current != null)
        {
            parent = current;
            comparison = Comparer.Compare(key, current.Key);

            if (comparison == 0)
            {
                current.Value = value;
                return;
            }

            if (comparison < 0)
            {
                current = current.Left;
            } 
            else
            {
                current = current.Right;
            }
        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (comparison < 0)
        {
            parent!.Left = newNode;
        } 
        else
        {
           parent!.Right = newNode; 
        }

        Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
        }
        else
        {
            var minRight = node.Right;
            while (minRight.Left != null)
            {
                minRight = minRight.Left;
            }

            if (minRight.Parent != node)
            {
                Transplant(minRight, minRight.Right);

                minRight.Right = node.Right;
                if (minRight.Right != null)
                {
                    minRight.Right.Parent = minRight;
                }
            }

            Transplant(node, minRight);

            minRight.Left = node.Left;
            if (minRight.Left != null)
            {
                minRight.Left.Parent = minRight;
            }

            OnNodeRemoved(minRight.Parent, minRight);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x == null || x.Right == null) return;

        var y = x.Right;

        x.Right = y.Left;
        if (y.Left != null)
        {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;

        if (x.Parent == null)
        {
            Root = y;
        }
        else if (x.IsLeftChild)
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y == null || y.Left == null) return;

        var x = y.Left;

        y.Left = x.Right;
        if (x.Right != null)
        {
            x.Right.Parent = y;
        }

        x.Parent = y.Parent;

        if (y.Parent == null)
        {
            Root = x;
        }
        else if (y.IsLeftChild)
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }

        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x?.Right == null) return;
        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y?.Left == null) return;
        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        if (x?.Right == null) return;

        TNode? second_node = x.Right;
        RotateLeft(x);
        RotateLeft(second_node);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y?.Left == null) return;

        TNode? second_node = y.Left;
        RotateRight(y);
        RotateRight(second_node);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    /* IEnumerable - интерфейс, который означает "последовательность элементов, по которой можно итерироваться"
     имеет метод IEnumerator:
     public interface IEnumerable {
        IEnumerator GetEnumerator()
        IEnumerator - интерфейс, определяет функционал для перебора внутренних объектов в контейнере (MoveNext, Current, Reset)
        GetEnumerator() - возвращает перечислитель, который выполняет итерацию по коллекции

     }
     */
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>

private struct TreeIterator : 
    IEnumerable<TreeEntry<TKey, TValue>>,
    IEnumerator<TreeEntry<TKey, TValue>>
{
    private readonly TraversalStrategy _strategy;
    private readonly TNode? _root;
    private TNode? _current;
    private bool _started;
    private TreeEntry<TKey, TValue> _currentEntry;
    
    public TreeIterator(TNode? root, TraversalStrategy strategy)
    {
        _root = root;
        _strategy = strategy;
        _current = null;
        _started = false;
        _currentEntry = default;
    }
    
    public bool MoveNext()
    {
        if (_root == null) return false;
        
        if (!_started)
        {
            _current = GetFirstNode(_root, _strategy);
            _started = true;
        }
        else
        {
            if (_current == null) return false;
            _current = GetNextNode(_current, _strategy);
        }
        
        if (_current == null) return false;
        
        _currentEntry = new TreeEntry<TKey, TValue>(
            _current.Key, 
            _current.Value, 
            GetDepth(_current)
        );
        return true;
    }

    
    private TNode? GetFirstNode(TNode? root, TraversalStrategy strategy)
    {
        return strategy switch
        {
            TraversalStrategy.InOrder => FindLeftmost(root),
            TraversalStrategy.InOrderReverse => FindRightmost(root),
            TraversalStrategy.PreOrder => root,
            TraversalStrategy.PreOrderReverse => FindPreOrderLast(root),
            TraversalStrategy.PostOrder => FindPostOrderFirst(root),
            TraversalStrategy.PostOrderReverse => root,
            _ => root
        };
    }
    
    private TNode? GetNextNode(TNode current, TraversalStrategy strategy)
    {
        return strategy switch
        {
            TraversalStrategy.InOrder => GetNextInOrder(current),
            TraversalStrategy.InOrderReverse => GetNextInOrderReverse(current),
            TraversalStrategy.PreOrder => GetNextPreOrder(current),
            TraversalStrategy.PreOrderReverse => GetNextPreOrderReverse(current),
            TraversalStrategy.PostOrder => GetNextPostOrder(current),
            TraversalStrategy.PostOrderReverse => GetNextPostOrderReverse(current),
            _ => null
        };
    }
    
    private TNode? FindLeftmost(TNode? node)
    {
        while (node?.Left != null)
            node = node.Left;
        return node;
    }
    
    private TNode? FindRightmost(TNode? node)
    {
        while (node?.Right != null)
            node = node.Right;
        return node;
    }
    
    private TNode? FindPostOrderFirst(TNode? node)
    {
        if (node == null) return null;
        
        while (node!.Left != null || node!.Right != null)
        {
            if (node.Left != null)
                node = node.Left;
            else
                node = node.Right;
        }
        return node;
    }
    
    private TNode? FindPreOrderLast(TNode? node)
    {
        if (node == null) return null;
        
        while (node!.Right != null || node!.Left != null)
        {
            if (node!.Right != null)
                node = node.Right;
            else
                node = node.Left;
        }
        return node;
    }
    
    private TNode? GetNextInOrder(TNode current)
    {
        if (current.Right != null)
            return FindLeftmost(current.Right);
        
        while (current.Parent != null && current == current.Parent.Right)
            current = current.Parent;
        
        return current.Parent;
    }
    
    private TNode? GetNextInOrderReverse(TNode current)
    {
        if (current.Left != null)
            return FindRightmost(current.Left);
        
        while (current.Parent != null && current == current.Parent.Left)
            current = current.Parent;
        
        return current.Parent; 
    }

    
    private TNode? GetNextPreOrder(TNode current)
    {
        if (current.Left != null)
            return current.Left;
        
        if (current.Right != null)
            return current.Right;
        
        while (current.Parent != null)
        {
            if (current == current.Parent.Left && current.Parent.Right != null)
                return current.Parent.Right;
            current = current.Parent;
        }
        
        return null;
    }

    private TNode? GetNextPreOrderReverse(TNode current)
    {
        if (current.Parent == null)
            return null;
        
        if (current == current.Parent.Right && current.Parent.Left != null) 
            return FindPreOrderLast(current.Parent.Left);
        return current.Parent;
    }


    private TNode? GetNextPostOrder(TNode current)
    {
        if (current.Parent == null)
            return null;
        
        if (current == current.Parent.Left && current.Parent.Right != null)
            return FindPostOrderFirst(current.Parent.Right);
        
        return current.Parent;
    }

    private TNode? GetNextPostOrderReverse(TNode current)
    {
        if (current.Right != null) return current.Right;
        if (current.Left != null) return current.Left;
        while (current.Parent != null)
        {
            if (current == current.Parent.Right && current.Parent.Left != null) return current.Parent.Left;
            current = current.Parent;
        }
        return null;
    }

    private int GetDepth(TNode node)
    {
        int depth = 0;
        var current = node;
        while (current.Parent != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }
    
    public TreeEntry<TKey, TValue> Current => _currentEntry;
    object IEnumerator.Current => Current;
    
    public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
    IEnumerator IEnumerable.GetEnumerator() => this;
    
    public void Reset()
    {
        _current = null;
        _started = false;
        _currentEntry = default;
    }
    
    public void Dispose()
    {
            Reset();
    }
}
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
    InOrder().Select(e => KeyValuePair.Create(e.Key, e.Value)).GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        int index = arrayIndex;
        foreach (var entry in InOrder())
        {
            if (index >= array.Length) throw new ArgumentException();
            array[index++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}