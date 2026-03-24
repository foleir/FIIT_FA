using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
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

    public ICollection<TKey> Keys
    {
        get
        {
            List<TKey> key = new List<TKey>();
            IEnumerable<TreeEntry<TKey, TValue>> elements = InOrder();
            foreach (var element in elements)
            {
                key.Add(element.Key);
            }
            return key;
        }
    }
    public ICollection<TValue> Values 
    {
        get
        {
            List<TValue> value = new List<TValue>();
            IEnumerable<TreeEntry<TKey, TValue>> elements = InOrder();
            foreach (var element in elements)
            {
                value.Add(element.Value);
            }
            return value;

        }
    }
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode node = CreateNode(key, value);
        if (Root == null)
        {
            Root = node;
            Count++;
            OnNodeAdded(node);
            return;
        }

        TNode? current = Root;
        TNode? parent = null;
        int comparison = 0;

        while (current != null)
        {
            parent = current;
            comparison = Comparer.Compare(key, current.Key);

            if (comparison < 0)             //ключ меньше, идем влево
            {
                current = current.Left;
            }
            else if (comparison > 0)        //ключ больше, идем вправо
            {
                current = current.Right;   
            } 
            else
            {
                current.Value = value;
                return;
            }
        }

        if (parent == null)
        {
            throw new InvalidOperationException();
        }

        node.Parent = parent;

        if (comparison < 0)
        {
            parent.Left = node;
        }
        else
        {
            parent.Right = node;
        }

        Count++;
        OnNodeAdded(node);

    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null && node.Right == null)
        {
            Transplant(node, null);
            this.Count--;
            OnNodeRemoved(node.Parent, null, node);
            return;
        }

        if (node.Left == null)
        {
            Transplant(node, node.Right);
            this.Count--;
            OnNodeRemoved(node.Parent, node.Right, node);
            return;
        }

        if (node.Right == null)
        {
            Transplant(node, node.Left);
            this.Count--;
            OnNodeRemoved(node.Parent, node.Left, node);
            return;
        }

        // если 2 ребенка
        TNode rightmost = node.Left;
        while (rightmost.Right != null)
        {
            rightmost = rightmost.Right;
        } 

        node.Key = rightmost.Key;
        node.Value = rightmost.Value;
        
        TNode? parentRightmost = rightmost.Parent;
        TNode? childRightmost = rightmost.Left;

        Transplant(rightmost, childRightmost);

        Count--;
        OnNodeRemoved(parentRightmost, childRightmost, rightmost);
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
    /// <param name="deletedNode">Узел, который удалили</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child, TNode deletedNode) { }
    
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
        if (x == null || x == Root || x.Parent == null) return;
        
        TNode? tmp = x.Left;
        x.Left = x.Parent;
        x.Parent.Right = tmp;
        tmp?.Parent = x.Parent;

        Transplant(x.Parent, x);
        x.Left.Parent = x;
    }

    protected void RotateRight(TNode y)
    {
        if (y == null || y == Root || y.Parent == null) return;

        TNode? tmp = y.Right;
        y.Right = y.Parent;
        y.Parent.Left = tmp;
        tmp?.Parent = y.Parent;

        Transplant(y.Parent, y);
        y.Right.Parent = y;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        RotateRight(x);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateLeft(y);
        RotateRight(y);
    }
    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y);
        RotateRight(y);
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
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => InOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node)
        => new TreeIterator(node, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    private struct TreeIterator : 
            IEnumerable<TreeEntry<TKey, TValue>>,
            IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? root;
        private readonly TraversalStrategy strategy;

        private TNode? current;
        private TNode? lastVisited;
        private bool started;
        private bool finished;


        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current 
        {
            get
            {
                if (current == null) 
                    throw new InvalidOperationException("Iterator not positioned");
                return new TreeEntry<TKey, TValue>(current.Key, current.Value, ComputeDepth(current));
            }
        }
        object IEnumerator.Current => Current;
        
        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            this.root = root;
            this.strategy = strategy;
            this.current = null;
            this.lastVisited = null;
            this.started = false;
            this.finished = false;
        }
        
        private int ComputeDepth(TNode node)
        {
            int depth = 0;
            TNode? curr = node;
            while (curr?.Parent != null)
            {
                depth++;
                curr = curr.Parent;
            }
            return depth;
        }
        
        public bool MoveNext()
        {
            if (finished) 
                return false;
            
            if (!started)
            {
                started = true;
                current = GetFirst(strategy, root);
                if (current == null)
                {
                    finished = true;
                    return false;
                }
                lastVisited = null;
                return true;
            }
            
            lastVisited = current;
            current = GetNext(strategy, current, lastVisited);
            
            if (current == null)
            {
                finished = true;
                return false;
            }
            return true;
        }

        private TNode? GetFirst(TraversalStrategy strategy, TNode? node)
        {
            return strategy switch
            {
                TraversalStrategy.InOrder => FirstInOrder(node),
                TraversalStrategy.PreOrder => node,
                TraversalStrategy.PostOrder => FirstPostOrder(node),
                TraversalStrategy.InOrderReverse => FirstInOrderReverse(node),
                TraversalStrategy.PreOrderReverse => FirstPreOrderReverse(node),
                TraversalStrategy.PostOrderReverse => node,
                _ => null
            };
        }

        private static TNode? FirstInOrder(TNode? node)
        {
            while (node?.Left != null)
                node = node.Left;
            return node;
        }

        private static TNode? FirstInOrderReverse(TNode? node)
        {
            while (node?.Right != null)
                node = node.Right;
            return node;
        }

        private static TNode? FirstPostOrder(TNode? node)
        {
            if (node == null) return null;
            while (true)
            {
                if (node.Left != null)
                    node = node.Left;
                else if (node.Right != null)
                    node = node.Right;
                else
                    break;
            }
            return node;
        }

        private static TNode? FirstPreOrderReverse(TNode? node)
        {
            if (node == null) return null;
            while (node.Right != null)
                node = node.Right;
            return node;
        }


        private TNode? GetNext(TraversalStrategy strategy, TNode? current, TNode? last)
        {
            if (current == null) return null;
            
            return strategy switch
            {
                TraversalStrategy.InOrder => NextInOrder(current),
                TraversalStrategy.PreOrder => NextPreOrder(current, last),
                TraversalStrategy.PostOrder => NextPostOrder(current, last),
                TraversalStrategy.InOrderReverse => NextInOrderReverse(current),
                TraversalStrategy.PreOrderReverse => NextPreOrderReverse(current, last),
                TraversalStrategy.PostOrderReverse => NextPostOrderReverse(current, last),
                _ => null
            };
        }

        private static TNode? NextInOrder(TNode? node)
        {
            if (node?.Right != null)
                return FirstInOrder(node.Right);
            
            TNode? curr = node;
            while (curr?.Parent != null && curr.IsRightChild)
                curr = curr.Parent;
            
            return curr?.Parent;
        }

        private static TNode? NextInOrderReverse(TNode? node)
        {
            if (node?.Left != null)
                return FirstInOrderReverse(node.Left);
            
            TNode? curr = node;
            while (curr?.Parent != null && curr.IsLeftChild)
                curr = curr.Parent;
            
            return curr?.Parent;
        }

        private static TNode? NextPreOrder(TNode? node, TNode? last)
        {
            if (node == null) return null;
            
            if (node.Left != null && node.Left != last)
                return node.Left;
            
            if (node.Right != null && node.Right != last)
                return node.Right;
            
            TNode? curr = node;
            while (curr?.Parent != null)
            {
                if (curr.IsLeftChild && curr.Parent.Right != null && curr.Parent.Right != last)
                    return curr.Parent.Right;
                curr = curr.Parent;
            }
            
            return null;
        }

        private static TNode? NextPreOrderReverse(TNode? node, TNode? last)
        {
            if (node == null) return null;
            
            if (node.IsRightChild && node.Parent?.Left != null && node.Parent.Left != last)
            {
                TNode? left = node.Parent.Left;
                while (left?.Right != null)
                    left = left.Right;
                return left;
            }
            
            return node.Parent;
        }

        private static TNode? NextPostOrder(TNode? node, TNode? last)
        {
            TNode? parent = node?.Parent;
            if (parent == null) 
                return null;
            
            if (node?.IsLeftChild == true && parent.Right != null && parent.Right != last)
                return FirstPostOrder(parent.Right);
            
            return parent;
        }

        private static TNode? NextPostOrderReverse(TNode? node, TNode? last)
        {
            // Root, Right-subtree, Left-subtree
            if (node == null) return null;
            
            if (node.Right != null && node.Right != last)
                return node.Right;
            
            if (node.Left != null && node.Left != last)
                return node.Left;
            
            // Both children processed, go up to find unvisited left sibling
            TNode? curr = node;
            while (curr?.Parent != null)
            {
                // If we came from right child, check for left sibling of parent
                if (curr.IsRightChild && curr.Parent.Left != null && curr.Parent.Left != last)
                    return curr.Parent.Left;
                curr = curr.Parent;
            }
            
            return null;
        }

        public void Reset()
        {
            current = null;
            lastVisited = null;
            started = false;
            finished = false;
        }

        public void Dispose()
        {
            finished = true;
            current = null;
            lastVisited = null;
        }
    }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator();
    }
    
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index can't be negative");
        
        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Not enough space in array");
        
        int currentIndex = arrayIndex;
        
        foreach (var entry in InOrder())
        {
            array[currentIndex] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            currentIndex++;
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}