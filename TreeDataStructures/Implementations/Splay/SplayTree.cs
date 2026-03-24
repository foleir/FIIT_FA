using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    private void Splay(BstNode<TKey, TValue> node)
    {
        if (node == null) return;
        
        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grandParent = parent.Parent;
            
            if (grandParent == null)
            {
                // Zig: родитель — корень
                if (node.IsLeftChild)
                    RotateRight(node);
                else
                    RotateLeft(node);
            }
            else if (node.IsLeftChild && parent.IsLeftChild)
            {
                // Zig-Zig: оба левые
                RotateRight(parent);
                RotateRight(node);
            }
            else if (node.IsRightChild && parent.IsRightChild)
            {
                // Zig-Zig: оба правые
                RotateLeft(parent);
                RotateLeft(node);
            }
            else if (node.IsLeftChild && parent.IsRightChild)
            {
                // Zig-Zag: узел левый, родитель правый
                RotateBigLeft(node);
            }
            else // node.IsRightChild && parent.IsLeftChild
            {
                // Zig-Zag: узел правый, родитель левый
                RotateBigRight(node);
            }
        }
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child, BstNode<TKey, TValue> deletedNode) 
    {
        if (parent != null)
            Splay(parent);
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            Splay(node);
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
    
}