using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);
        if (Comparer.Compare(root.Key, key) <= 0)
        {
            (TreapNode<TKey, TValue>? newLeft, TreapNode<TKey, TValue>? newRight) = Split(root.Right, key);
            root.Right = newLeft;
            root.Right?.Parent = root;
            return (root, newRight);
        } else
        {
            (TreapNode<TKey, TValue>? newLeft, TreapNode<TKey, TValue>? newRight) = Split(root.Left, key);
            root.Left = newRight;
            root.Left?.Parent = root;
            return (newLeft, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        if (left.Priority >= right.Priority)
        {
            left.Right = Merge(left.Right, right);
            left.Right?.Parent = left;
            return left;
        } else
        {
            right.Left = Merge(left, right.Left);
            right.Left?.Parent = right;
            return right;
        }
    }


    public override void Add(TKey key, TValue value)
    {
        TreapNode<TKey, TValue>? existingNode = FindNode(key);
        if (existingNode != null)
        {
            existingNode.Value = value;
            return;
        }

        TreapNode<TKey, TValue> node = CreateNode(key, value);

        (TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right) = Split(Root, key);
        Root = Merge(Merge(left, node), right);
        Root?.Parent = null;
        this.Count++;
        OnNodeAdded(node);
    }

    public override bool Remove(TKey key)
{
    // Находим узел для удаления
    TreapNode<TKey, TValue>? nodeToRemove = FindNode(key);
    if (nodeToRemove == null) 
        return false;
    
    // Сливаем левое и правое поддеревья удаляемого узла
    TreapNode<TKey, TValue>? mergedChildren = Merge(nodeToRemove.Left, nodeToRemove.Right);
    
    // Заменяем удаляемый узел на результат слияния его детей
    if (nodeToRemove.Parent == null)
    {
        // Удаляем корень
        Root = mergedChildren;
        if (Root != null) Root.Parent = null;
    }
    else if (nodeToRemove.IsLeftChild)
    {
        nodeToRemove.Parent.Left = mergedChildren;
        if (mergedChildren != null) mergedChildren.Parent = nodeToRemove.Parent;
    }
    else
    {
        nodeToRemove.Parent.Right = mergedChildren;
        if (mergedChildren != null) mergedChildren.Parent = nodeToRemove.Parent;
    }
    
    Count--;
    OnNodeRemoved(nodeToRemove.Parent, mergedChildren, nodeToRemove);
    
    return true;
}

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child, TreapNode<TKey, TValue> deletedNode) { }
}
