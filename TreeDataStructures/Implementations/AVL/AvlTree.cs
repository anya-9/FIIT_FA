using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        BalanceFromNode(newNode);
    }
    
    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        if (parent != null)
            BalanceFromNode(parent); // у детей высота не изменится, она же снизу считается
        else if (child != null)
            BalanceFromNode(child);
    }
    
    private void BalanceFromNode(AvlNode<TKey, TValue> node)
    {
        var current = node;
        while (current != null)
        {
            UpdateHeight(current);
            var parent = current.Parent;
            Balance(current);
            current = parent;
        }
    }

    private void Balance(AvlNode<TKey, TValue> node)
    {
        int balance = GetBalance(node);
    
        if (balance > 1)
        {
            if (GetBalance(node.Left) < 0)
                RotateBigRight(node);
            else
                RotateRight(node);
        }
        else if (balance < -1)
        {
            if (GetBalance(node.Right) > 0)
                RotateBigLeft(node);
            else
                RotateLeft(node);
        }
    }
    
    private int GetHeight(AvlNode<TKey, TValue>? node)
        => node?.Height ?? 0; // если null то вернет 0
    
    private int GetBalance(AvlNode<TKey, TValue>? node)
        => GetHeight(node?.Left) - GetHeight(node?.Right);
    
    private void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }
    
    protected new void RotateLeft(AvlNode<TKey, TValue> x)
    {
        base.RotateLeft(x);
        UpdateHeight(x);
        if (x.Parent != null) UpdateHeight(x.Parent);
    }
    
    protected new void RotateRight(AvlNode<TKey, TValue> y)
    {
        base.RotateRight(y);
        UpdateHeight(y);
        if (y.Parent != null) UpdateHeight(y.Parent);
    }
}