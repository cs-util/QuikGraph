﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
#endif
using JetBrains.Annotations;

namespace QuikGraph.Collections
{
    /// <summary>
    /// Soft heap, which aims to has a constant amortized time for
    /// creation of heap, inserting an element merging two heaps,
    /// deleting an element and finding the element with minimum key.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class SoftHeap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private sealed class Cell
        {
            [NotNull]
            public TKey Key { get; }

            public TValue Value { get; }

            public Cell Next { get; internal set; }

            public Cell(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private sealed class Node
        {
            [NotNull]
            public TKey CKey { get; internal set; }

            public int Rank { get; }

            [CanBeNull]
            public Node Next { get; internal set; }

            [CanBeNull]
            public Node Child { get; internal set; }

            // ReSharper disable once InconsistentNaming
            [CanBeNull]
            public Cell IL { get; internal set; }
            
            // ReSharper disable once InconsistentNaming
            [CanBeNull]
            public Cell ILTail { get; internal set; }

            public Node([NotNull] Cell cell)
            {
                Rank = 0;
                CKey = cell.Key;
                IL = cell;
                ILTail = cell;
            }

            public Node(
                [NotNull] TKey cKey, 
                int rank, 
                [NotNull] Node next,
                [NotNull] Node child,
                [CanBeNull] Cell il,
                [CanBeNull] Cell ilTail)
            {
                CKey = cKey;
                Rank = rank;
                Next = next;
                Child = child;
                IL = il;
                ILTail = ilTail;
            }
        }

        private sealed class Head
        {
            public Node Queue { get; internal set; }
            public Head Next { get; internal set; }
            public Head Prev { get; internal set; }
            public Head SuffixMin { get; internal set; }
            public int Rank { get; internal set; }
        }

        [NotNull]
        private readonly Head _header;

        [NotNull]
        private readonly Head _tail;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftHeap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="maximumErrorRate">Indicates the maximum error rate to respect.</param>
        /// <param name="keyMaxValue">Gives the maximum key value.</param>
        public SoftHeap(double maximumErrorRate, [NotNull] TKey keyMaxValue)
            : this(maximumErrorRate, keyMaxValue, Comparer<TKey>.Default.Compare)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftHeap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="maximumErrorRate">Indicates the maximum error rate to respect.</param>
        /// <param name="keyMaxValue">Gives the maximum key value.</param>
        /// <param name="comparison">Key comparer.</param>
        public SoftHeap(double maximumErrorRate, [NotNull] TKey keyMaxValue, [NotNull] Comparison<TKey> comparison)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(keyMaxValue != null);
            Contract.Requires(comparison != null);
            Contract.Requires(0 < maximumErrorRate && maximumErrorRate <= 0.5);
#endif

            KeyComparison = comparison;
            KeyMaxValue = keyMaxValue;
            _header = new Head();
            _tail = new Head { Rank = int.MaxValue };
            _header.Next = _tail;
            _tail.Prev = _header;
            ErrorRate = maximumErrorRate;
            MinRank = 2 + 2 * (int)Math.Ceiling(Math.Log(1.0 / ErrorRate, 2.0));
            Count = 0;
        }

        /// <summary>
        /// Minimal rank (based on <see cref="ErrorRate"/>).
        /// </summary>
#if SUPPORTS_CONTRACTS
        [System.Diagnostics.Contracts.Pure]
#endif
        public int MinRank { get; }

        /// <summary>
        /// Key comparer.
        /// </summary>
        [NotNull]
        public Comparison<TKey> KeyComparison { get; }

        /// <summary>
        /// Maximal authorized key.
        /// </summary>
#if SUPPORTS_CONTRACTS
        [System.Diagnostics.Contracts.Pure]
#endif
        [NotNull]
        public TKey KeyMaxValue { get; }

        /// <summary>
        /// Error rate.
        /// </summary>
#if SUPPORTS_CONTRACTS
        [System.Diagnostics.Contracts.Pure]
#endif
        public double ErrorRate { get; }

        /// <summary>
        /// Number of element.
        /// </summary>
#if SUPPORTS_CONTRACTS
        [System.Diagnostics.Contracts.Pure]
#endif
        public int Count { get; private set; }

#if SUPPORTS_CONTRACTS
        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Count > -1);
            Contract.Invariant(_header != null);
            Contract.Invariant(_tail != null);
        }
#endif

        /// <summary>
        /// Adds the given <paramref name="value"/> with the given <paramref name="key"/> into the heap.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value to add.</param>
        public void Add([NotNull] TKey key, TValue value)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(key != null);
            Contract.Requires(KeyComparison(key, KeyMaxValue) < 0);
#endif

            var cell = new Cell(key, value);
            var node = new Node(cell);

            Meld(node);
            Count++;
        }

        private void Meld([NotNull] Node node)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(node != null);
#endif

            Head toHead = _header.Next;
            while (node.Rank > toHead.Rank)
            {
#if SUPPORTS_CONTRACTS
                Contract.Assert(toHead.Next != null);
#endif
                toHead = toHead.Next;
            }

            Head prevHead = toHead.Prev;
            while (node.Rank == toHead.Rank)
            {
                Node top, bottom;
                if (KeyComparison(toHead.Queue.CKey, node.CKey) > 0)
                {
                    top = node;
                    bottom = toHead.Queue;
                }
                else
                {
                    top = toHead.Queue;
                    bottom = node;
                }

                node = new Node(top.CKey, top.Rank + 1, bottom, top, top.IL, top.ILTail);
                toHead = toHead.Next;
            }

            Head head = prevHead == toHead.Prev 
                ? new Head() 
                : prevHead.Next;

            head.Queue = node;
            head.Rank = node.Rank;
            head.Prev = prevHead;
            head.Next = toHead;
            prevHead.Next = head;
            toHead.Prev = head;

            FixMinList(head);
        }

        private void FixMinList([NotNull] Head head)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(head != null);
#endif

            Head tmpMin = head.Next == _tail 
                ? head 
                : head.Next.SuffixMin;

            while (head != _header)
            {
                if (KeyComparison(tmpMin.Queue.CKey, head.Queue.CKey) > 0)
                    tmpMin = head;

                head.SuffixMin = tmpMin;
                head = head.Prev;
            }
        }

        [NotNull]
        private Node Shift([NotNull] Node v)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(v != null);
#endif

            v.IL = null;
            v.ILTail = null;
            if (v.Next is null && v.Child is null)
            {
                v.CKey = KeyMaxValue;
                return v;
            }

            v.Next = Shift(v.Next);
            // Restore heap ordering that might be broken by shifting
            if (KeyComparison(v.Next.CKey, v.Child.CKey) > 0)
            {
                Node tmp = v.Child;
                v.Child = v.Next;
                v.Next = tmp;
            }

            v.IL = v.Next.IL;
            v.ILTail = v.Next.ILTail;
            v.CKey = v.Next.CKey;

            // Softening the heap
            SoftenHeap(v);

            UpdateChildAndNext(v);

            return v;
        }

        private void SoftenHeap([NotNull] Node node)
        {
            if (node.Rank > MinRank
                && (node.Rank % 2 == 1 || node.Child.Rank < node.Rank - 1))
            {
#if SUPPORTS_CONTRACTS
                Contract.Assert(node.Next != null);
#endif

                node.Next = Shift(node.Next);
                // Restore heap ordering that might be broken by shifting
                if (KeyComparison(node.Next.CKey, node.Child.CKey) > 0)
                {
                    Node tmp = node.Child;
                    node.Child = node.Next;
                    node.Next = tmp;
                }

                if (KeyComparison(node.Next.CKey, KeyMaxValue) != 0 && node.Next.IL != null)
                {
                    node.Next.ILTail.Next = node.IL;
                    node.IL = node.Next.IL;
                    if (node.ILTail == null)
                        node.ILTail = node.Next.ILTail;
                    node.CKey = node.Next.CKey;
                }
            } // End second shift
        }

        private void UpdateChildAndNext([NotNull] Node node)
        {
#if SUPPORTS_CONTRACTS
            Contract.Assert(node.Child != null);
#endif

            if (KeyComparison(node.Child.CKey, KeyMaxValue) == 0)
            {
#if SUPPORTS_CONTRACTS
                Contract.Assert(node.Next != null);
#endif

                if (KeyComparison(node.Next.CKey, KeyMaxValue) == 0)
                {
                    node.Child = null;
                    node.Next = null;
                }
                else
                {
                    node.Child = node.Next.Child;
                    node.Next = node.Next.Next;
                }
            }
        }
// Shift

        /// <summary>
        /// Deletes the element with minimal key.
        /// </summary>
        /// <returns>Deleted element.</returns>
        public KeyValuePair<TKey, TValue> DeleteMin()
        {
            if (Count == 0)
                throw new InvalidOperationException("Heap is empty.");

            Head head = _header.Next.SuffixMin;
            while (head.Queue.IL is null)
            {
                Node tmp = head.Queue;
                int childCount = 0;
                while (tmp.Next != null)
                {
                    tmp = tmp.Next;
                    childCount++;
                }

                if (childCount < head.Rank / 2)
                {
                    head.Prev.Next = head.Next;
                    head.Next.Prev = head.Prev;
                    FixMinList(head.Prev);
                    tmp = head.Queue;
                    while (tmp.Next != null)
                    {
                        Meld(tmp.Child);
                        tmp = tmp.Next;
                    }
                }
                else
                {
                    head.Queue = Shift(head.Queue);
                    if (KeyComparison(head.Queue.CKey, KeyMaxValue) == 0)
                    {
                        head.Prev.Next = head.Next;
                        head.Next.Prev = head.Prev;
                        head = head.Prev;
                    }

                    FixMinList(head);
                }

                head = _header.Next.SuffixMin;
            } // End of outer while loop

            TKey min = head.Queue.IL.Key;
            TValue value = head.Queue.IL.Value;
            head.Queue.IL = head.Queue.IL.Next;
            if (head.Queue.IL is null)
                head.Queue.ILTail = null;

            Count--;
            return new KeyValuePair<TKey, TValue>(min, value);
        }

        #region IEnumerable

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>>

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator();
        }

        private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            public Enumerator()
            {
                Current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext()
            {
                // TODO
                return false;
            }

            public KeyValuePair<TKey, TValue> Current { get; }

            public void Dispose()
            {
                // Currently the enumerator does nothing
            }

            object IEnumerator.Current => Current;

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }
}