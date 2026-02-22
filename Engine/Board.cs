using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumTicTacToe
{
    public class Board
    {
        private readonly Cell[] _cells = Enumerable.Range(0, 9).Select(_ => new Cell()).ToArray();

        /// <summary>
        /// Attempt to place a quantum move consisting of two related cells.
        /// Returns false if either cell is already collapsed or move is invalid.
        /// </summary>
        public bool TryPlaceQuantumMove(Move move)
        {
            if (!_cells[move.CellA].CanAddQuantum(move) || !_cells[move.CellB].CanAddQuantum(move))
                return false;

            // before we commit the move, see if it would close a cycle
            var cycle = DetectCycle(move);
            _cells[move.CellA].AddQuantum(move);
            _cells[move.CellB].AddQuantum(move);

            if (cycle != null)
            {
                // TODO: collapse logic will go here later
                // for now we just notify via event or leave it to caller
            }

            return true;
        }

        /// <summary>
        /// Determine whether adding <paramref name="potentialMove"/> produces a cycle.
        /// If so, returns the list of moves that form the cycle (existing path plus the
        /// new move as the last element).  Otherwise returns null.
        /// </summary>
        public List<Move>? DetectCycle(Move potentialMove)
        {
            // find a path from A to B using existing moves (do not include potentialMove)
            var path = FindPath(potentialMove.CellA, potentialMove.CellB, exclude: null);
            if (path == null)
                return null; // no connection present, no cycle

            // cycle consists of the existing path plus the new move
            var cycle = new List<Move>(path) { potentialMove };
            return cycle;
        }

        /// <summary>
        /// Collapse a detected cycle by choosing one endpoint of each move to become a
        /// classical symbol.  <paramref name="winnerByMove"/> maps each move in the
        /// cycle to the index of the winning cell (either move.CellA or CellB).
        /// Collapsing a cell removes all quantum marks from it and clears the losing
        /// branch from its partner cell.
        /// </summary>
        public void CollapseCycle(List<Move> cycle, Dictionary<Move, int> winnerByMove)
        {
            foreach (var move in cycle)
            {
                if (!winnerByMove.TryGetValue(move, out int winnerCell))
                    throw new ArgumentException("Winner mapping missing for a move", nameof(winnerByMove));

                int loserCell = move.CellA == winnerCell ? move.CellB : move.CellA;
                // collapse winner
                _cells[winnerCell].Collapse(move.Player.Symbol);
                // remove quantum reference from loser cell
                _cells[loserCell].RemoveQuantum(move);
            }

            // After collapsing cycle moves, also sweep any remaining quantum marks in
            // collapsed cells (should be cleared already) and remove references to
            // collapsed cells in other moves.
            for (int i = 0; i < 9; i++)
            {
                if (_cells[i].IsCollapsed)
                {
                    // remove this cell from any other moves residing in other cells
                    foreach (var move in _cells[i].GetQuantumMoves().ToList())
                    {
                        int other = move.CellA == i ? move.CellB : move.CellA;
                        _cells[other].RemoveQuantum(move);
                    }
                    _cells[i].Collapse(_cells[i].Display); // ensure cleared
                }
            }
        }

        /// <summary>
        /// Searches for a path between two cells using current quantum moves.
        /// <paramref name="exclude"/> may contain moves to ignore.
        /// Returns a list of moves along the path (in order) or null if no route.
        /// </summary>
        private List<Move>? FindPath(int start, int end, HashSet<Move>? exclude)
        {
            var visited = new bool[9];
            var result = new List<Move>();
            if (Dfs(start, end, visited, result, exclude))
                return result;
            return null;
        }

        private bool Dfs(int current, int target, bool[] visited, List<Move> path, HashSet<Move>? exclude)
        {
            if (current == target)
                return true;
            visited[current] = true;

            foreach (var (neighbor, move) in GetNeighbors(current))
            {
                if (exclude != null && exclude.Contains(move))
                    continue;
                if (visited[neighbor])
                    continue;

                path.Add(move);
                if (Dfs(neighbor, target, visited, path, exclude))
                    return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Returns all adjacent cell indices and the move that links to them.
        /// </summary>
        private IEnumerable<(int neighbor, Move move)> GetNeighbors(int cellIndex)
        {
            foreach (var move in _cells[cellIndex].GetQuantumMoves())
            {
                int other = move.CellA == cellIndex ? move.CellB : move.CellA;
                yield return (other, move);
            }
        }

        public void Print()
        {
            for (int i = 0; i < 9; i++)
            {
                Console.Write($"[{_cells[i]}]");
                if (i % 3 == 2) Console.WriteLine();
            }
        }

        /// <summary>
        /// Retrieves the textual representation of the cell at the given index
        /// (used by UI code).
        /// </summary>
        public string GetCellDisplay(int index)
        {
            if (index < 0 || index >= 9) throw new ArgumentOutOfRangeException(nameof(index));
            return _cells[index].ToString();
        }

        /// <summary>
        /// Returns the set of symbols ("X" and/or "O") that currently have a
        /// collapsed three-in-a-row.  If no one has a line, returns an empty set.
        /// </summary>
        public HashSet<string> GetWinners()
        {
            var results = new HashSet<string>();
            int[][] lines = new[]
            {
                new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8}, // rows
                new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8}, // cols
                new[]{0,4,8}, new[]{2,4,6}               // diags
            };
            foreach (var line in lines)
            {
                var a = _cells[line[0]];
                if (!a.IsCollapsed) continue;
                var symbol = a.Display;
                if (_cells[line[1]].IsCollapsed && _cells[line[1]].Display == symbol &&
                    _cells[line[2]].IsCollapsed && _cells[line[2]].Display == symbol)
                {
                    results.Add(symbol);
                }
            }
            return results;
        }

        /// <summary>
        /// Create a deep copy of the board, including cell states.
        /// </summary>
        public Board Clone()
        {
            var clone = new Board();
            for (int i = 0; i < 9; i++)
            {
                if (_cells[i].IsCollapsed)
                {
                    // collapsed state is stored internally, so replicate via collapse
                    clone._cells[i].Collapse(_cells[i].Display);
                }
                else
                {
                    // copy quantum moves
                    foreach (var move in _cells[i].GetQuantumMoves())
                    {
                        clone._cells[i].AddQuantum(move);
                    }
                }
            }
            return clone;
        }

        // TODO: implement cycle detection and collapse logic
        public bool IsCellCollapsed(int index)
        {
            if (index < 0 || index >= 9)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _cells[index].IsCollapsed;
             
        }

        /// <summary>
        /// Returns true if all cells are collapsed (board is full).
        /// </summary>
        public bool IsFull()
        {
            return _cells.All(cell => cell.IsCollapsed);
        }
    }
}
