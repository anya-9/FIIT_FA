using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;
public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new RbNode<TKey, TValue>(key, value) { Color = RbColor.Red };

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
        => FixAfterInsert(newNode);

    private void FixAfterInsert(RbNode<TKey, TValue> z)
    {
        // корень
        if (z == Root)
        {
            z.Color = RbColor.Black;
            return;
        }

        // пока нарушено свойство красный-красный (z красный т.к вставляемый красный)
        while (z != Root && z.Parent?.Color == RbColor.Red)
        {
            var parent = z.Parent;
            var grandparent = parent?.Parent;
            
            if (grandparent == null) break;

            // определяем дядю
            var uncle = parent!.IsLeftChild ? grandparent.Right : grandparent.Left;

            // дядя красный
            if (uncle?.Color == RbColor.Red)
            {
                // перекрас - родитель и дядя в черный, дед в красный
                parent.Color = RbColor.Black;
                uncle.Color = RbColor.Black;
                grandparent.Color = RbColor.Red;
                z = grandparent; // поднимаемся к деду и проверяем дальше (вдруг опять нарушение)
                continue;
            }

            // дядя черный или нет дяди
            if (parent.IsLeftChild)
            {
                // z - правый ребенок (будет zig-zag)
                if (z.IsRightChild)
                {
                    z = parent; // после поворота ребенок и родитель поменяются ролями
                    RotateLeft(z);
                    parent = z.Parent!;
                    grandparent = parent.Parent!;
                }
                
                // если не zig-zag, то просто малый поворот
                parent.Color = RbColor.Black;  // +перекрас родитель в черный, а дед в красный
                grandparent.Color = RbColor.Red;
                RotateRight(grandparent);
            }
            else // parent - правый ребенок
            {
                // zig-zag
                if (z.IsLeftChild)
                {
                    z = parent;
                    RotateRight(z);
                    parent = z.Parent!;
                    grandparent = parent.Parent!;
                }
                
                // zig-zig получается 
                parent.Color = RbColor.Black;
                grandparent.Color = RbColor.Red;
                RotateLeft(grandparent);
            }
        }

        // корень обязательно черный
        if (Root != null)
            Root.Color = RbColor.Black;
    }

    public override bool Remove(TKey key)
    {
        var nodeToDelete = FindNode(key);
        if (nodeToDelete == null) return false;

        var removedNode = nodeToDelete; // удаляемый узел
        RbNode<TKey, TValue>? child = null; // ребенок, вставший на место удаленного
        var parent = nodeToDelete.Parent;

        // 0 или 1 ребенок (если удаляемый вдруг черный, то потом проверим все)
        if (nodeToDelete.Left == null || nodeToDelete.Right == null)
        {
            child = nodeToDelete.Left ?? nodeToDelete.Right;
            removedNode = nodeToDelete;
            parent = nodeToDelete.Parent;
            Transplant(nodeToDelete, child); // вместо удаляемого теперь ребенок
        }
        else // 2 ребенка
        {
            // ищем преемника (минимальный в правом поддереве)
            var successor = GetMinimum(nodeToDelete.Right);
            removedNode = successor;
            child = successor.Right;
            parent = successor.Parent;

            // если преемник не прямой ребенок удаляемого
            if (successor.Parent != nodeToDelete)
            {
                // поднимаем правого ребенка преемника
                Transplant(successor, successor.Right);
                successor.Right = nodeToDelete.Right;
                if (successor.Right != null) 
                    successor.Right.Parent = successor;
            }

            // ставим преемника на место удаляемого
            Transplant(nodeToDelete, successor);
            successor.Left = nodeToDelete.Left;
            if (successor.Left != null)
                successor.Left.Parent = successor;
            
            // копируем цвет
            successor.Color = nodeToDelete.Color;
            removedNode = successor;
            child = successor.Right;
            parent = successor;
        }

        // если удалили черный узел - нужно проверять все
        if (nodeToDelete.Color == RbColor.Black)
        {
            FixAfterDelete(child, parent);
        }

        Count--;
        return true;
    }
    private RbNode<TKey, TValue> GetMinimum(RbNode<TKey, TValue> node)
    {
        while (node.Left != null)
            node = node.Left;
        return node;
    }

    private void FixAfterDelete(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        while (node != Root && (node == null || node.Color == RbColor.Black))
        {
            if (node == parent?.Left)
            {
                var brother = parent!.Right;
            
                // брат красный
                if (brother?.Color == RbColor.Red)
                {
                    brother.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    brother = parent.Right;
                }
            
                // оба ребенка брата черные (даже если их нет - они черные)
                if (brother != null)
                {
                    if ((brother.Left == null || brother.Left.Color == RbColor.Black) &&
                        (brother.Right == null || brother.Right.Color == RbColor.Black))
                    {
                        brother.Color = RbColor.Red; // у родителя с этой стороны уменьшилась черная высота, так что переходим к родителю разбираться
                        node = parent;
                        parent = node.Parent;
                    }
                    else
                    {
                        // правый ребенок брата черный (при условии что node - левый)
                        if (brother.Right == null || brother.Right.Color == RbColor.Black)
                        {
                            if (brother.Left != null)
                                brother.Left.Color = RbColor.Black;
                            brother.Color = RbColor.Red;
                            RotateRight(brother);
                            brother = parent.Right;
                        }
                    
                        if (brother != null) {
                            brother!.Color = parent.Color;
                            parent.Color = RbColor.Black;
                            if (brother.Right != null)
                                brother.Right.Color = RbColor.Black;
                            RotateLeft(parent);
                        }
                        node = Root;
                    }
                }
                else
                {
                    node = parent;
                    parent = node?.Parent;
                }
            }
            else // node - правый ребенок
            {
                var brother = parent?.Left;
            
                if (brother?.Color == RbColor.Red)
                {
                    brother.Color = RbColor.Black;
                    parent!.Color = RbColor.Red;
                    RotateRight(parent);
                    brother = parent.Left;
                }
            
                if (brother != null)
                {
                    if ((brother.Left == null || brother.Left.Color == RbColor.Black) &&
                        (brother.Right == null || brother.Right.Color == RbColor.Black))
                    {
                        brother.Color = RbColor.Red;
                        node = parent;
                        parent = node?.Parent;
                    }
                    else
                    {
                        // левый ребенок черный (при условии, что node - правый)
                        if (brother.Left == null || brother.Left.Color == RbColor.Black)
                        {
                            if (brother.Right != null)
                                brother.Right.Color = RbColor.Black;
                            brother.Color = RbColor.Red;
                            RotateLeft(brother);
                            brother = parent!.Left;
                        }
                    
                        if (brother != null)
                        {
                            brother.Color = parent!.Color;
                            parent.Color = RbColor.Black;
                            if (brother.Left != null)
                                brother.Left.Color = RbColor.Black;
                            RotateRight(parent);
                        }
                        node = Root;
                    }
                }
                else
                {
                    node = parent;
                    parent = node?.Parent;
                }
            }
        }
    
        if (node != null)
            node.Color = RbColor.Black;
    }
}