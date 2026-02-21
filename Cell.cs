using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumTicTacToe
{
    public class Cell
    {
        private readonly List<Move> _quantumMoves = new List<Move>();
        private string? _collapsed;

        public bool IsCollapsed => _collapsed != null;
        public string Display => _collapsed ?? string.Join(",", _quantumMoves.Select(m => m.Label));

        public bool CanAddQuantum(Move move)
        {
            // cannot add to a collapsed cell
            return !IsCollapsed;
        }

        public void AddQuantum(Move move)
        {
            if (IsCollapsed) throw new InvalidOperationException("Cell already collapsed");
            _quantumMoves.Add(move);
        }

        public void Collapse(string symbol)
        {
            _collapsed = symbol;
            _quantumMoves.Clear();
        }

        /// <summary>
        /// Removes a particular move from the quantum superpositions in this cell.
        /// </summary>
        public void RemoveQuantum(Move move)
        {
            _quantumMoves.Remove(move);
        }

        /// <summary>
        /// Enumerates all pending quantum moves that currently occupy this cell.
        /// </summary>
        public IEnumerable<Move> GetQuantumMoves() => _quantumMoves;

        public override string ToString() => Display;
    }
}
