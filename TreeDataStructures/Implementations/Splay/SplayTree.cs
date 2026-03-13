using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
            Splay(parent);
        else if (child != null)
            Splay(child);
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }
        
        value = default;
        return false;
    }
    
    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }
        
        return false;
    }
    
    private void Splay(BstNode<TKey, TValue> x)
    {
        while (x.Parent != null)
        {
            var p = x.Parent;
            var g = p.Parent;
            
            if (g == null)
            {
                if (x.IsLeftChild)
                    RotateRight(p);
                else
                    RotateLeft(p);
            }
            else if (x.IsLeftChild && p.IsLeftChild)  // zig-zig (левый-левый)
            {
                RotateDoubleRight(g);
            }
            else if (x.IsRightChild && p.IsRightChild)  // zig-zig (правый-правый)
            {
                RotateDoubleLeft(g);
            }
            else if (x.IsLeftChild && p.IsRightChild)  // zig-zag (правый-левый)
            {
                RotateBigLeft(g);
            }
            else  // zig-zag (левый-правый)
            {
                RotateBigRight(g);
            }
        }
    }
}