using System.ComponentModel.DataAnnotations;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;
public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new RbNode<TKey, TValue>(key, value) { Color = RbColor.Red };

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        // корень
        if (newNode == Root)
        {
            newNode.Color = RbColor.Black;
            return;
        }

        // пока нарушено свойство красный-красный (newNode красный т.к вставляемый красный)
        while (newNode != Root && newNode.Parent?.Color == RbColor.Red)
        {
            var parent = newNode.Parent;
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
                newNode = grandparent; // поднимаемся к деду и проверяем дальше (вдруг опять нарушение)
            }

            // дядя черный или нет дяди
            else if (parent.IsLeftChild)
            {
                // newNode - правый ребенок (будет zig-zag)
                if (newNode.IsRightChild)
                {
                    newNode = parent; // после поворота ребенок и родитель поменяются ролями
                    RotateLeft(newNode);
                    parent = newNode.Parent!;
                }
                
                // если не zig-zag, то просто малый поворот
                parent.Color = RbColor.Black;  // +перекрас родитель в черный, а дед в красный
                grandparent.Color = RbColor.Red;
                RotateRight(grandparent);
                break;
            }
            else // parent - правый ребенок
            {
                // zig-zag
                if (newNode.IsLeftChild)
                {
                    newNode = parent;
                    RotateRight(newNode);
                    parent = newNode.Parent!;
                }
                
                // zig-zig получается 
                parent.Color = RbColor.Black;
                grandparent.Color = RbColor.Red;
                RotateLeft(grandparent);
                break;
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

        // 0 или 1 ребенок (если удаляемый вдруг черный, то потом проверим все)
        if (nodeToDelete.Left == null || nodeToDelete.Right == null)
        {
            var parent = nodeToDelete.Parent;
            var child = nodeToDelete.Left ?? nodeToDelete.Right;
            Transplant(nodeToDelete, child); // вместо удаляемого теперь ребенок

            if (nodeToDelete.Color == RbColor.Black)
            {
                OnNodeRemoved(parent, child);
            }
        }

        else // 2 ребенка
        {
            // ищем преемника (минимальный в правом поддереве)
            var successor = GetMinimum(nodeToDelete.Right);
            var originalColor = successor.Color;
            var childOfSuccessor = successor.Right;
            var parentOfSuccessor = successor.Parent;

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
            
            successor.Color = nodeToDelete.Color;

            // если удалили черный узел - нужно проверять все
            if (originalColor == RbColor.Black)
            {
                var actualParent = (parentOfSuccessor == nodeToDelete) ? successor : parentOfSuccessor; // если был прямым наследником, то родитель nodeToDelete, а ее уже нет. новый родитель - это сам successor
                OnNodeRemoved(actualParent, childOfSuccessor);
            }
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

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        while (child != Root && (child == null || child.Color == RbColor.Black))
        {
            if (child == parent!.Left)
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
                        child = parent;
                        parent = child.Parent;
                    }
                    else
                    {
                        // правый ребенок брата черный (при условии что node - левый т.е дальний черный) - приводим к случаю дальний красный
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
                        child = Root;
                    }
                }
                else
                {
                    child = parent;
                    parent = child?.Parent;
                }
            }
            else // child - правый ребенок
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
                        child = parent;
                        parent = child?.Parent;
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
                        child = Root;
                    }
                }
                else
                {
                    child = parent;
                    parent = child?.Parent;
                }
            }
        }
    
        if (child != null) // вышли из цикла либо потому что child корень (тогда его точно надо  в черный), либо потому что нашли красный узел (можно его перекрасить в черный чтоб восстановить баланс)
            child.Color = RbColor.Black;
    }
} 
