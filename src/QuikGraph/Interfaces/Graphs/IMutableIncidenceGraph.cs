﻿using JetBrains.Annotations;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
using QuikGraph.Contracts;
#endif

namespace QuikGraph
{
    /// <summary>
    /// A mutable incidence graph with vertices of type <typeparamref name="TVertex"/>
    /// and edges of type <typeparamref name="TEdge"/>.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
#if SUPPORTS_CONTRACTS
    [ContractClass(typeof(MutableIncidenceGraphContract<,>))]
#endif
    public interface IMutableIncidenceGraph<TVertex, TEdge> : IMutableGraph<TVertex, TEdge>, IIncidenceGraph<TVertex, TEdge>
        where TEdge : IEdge<TVertex>
    {
        /// <summary>
        /// Removes all out-edges of the <paramref name="vertex"/>
        /// where the <paramref name="predicate"/> is evaluated to true.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="predicate">Predicate to remove edges.</param>
        /// <returns>The number of removed edges.</returns>
        int RemoveOutEdgeIf([NotNull] TVertex vertex, [NotNull, InstantHandle] EdgePredicate<TVertex, TEdge> predicate);

        /// <summary>
        /// Trims the out-edges of the given <paramref name="vertex"/>
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        void ClearOutEdges([NotNull] TVertex vertex);

        /// <summary>
        /// Trims excess storage allocated for edges.
        /// </summary>
        void TrimEdgeExcess();
    }
}