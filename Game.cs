using System;
using System.Collections.Generic;

namespace QuantumTicTacToe
{
    public class Game
    {
        private Board _board = new Board();
        private readonly List<Player> _players = new List<Player>
        {
            new Player("X"),
            new Player("O")
        };

        private int _currentPlayerIndex = 0;
        private int _moveNumber = 1;

        private bool _isOver = false;

        // track how many moves each player has taken (quantum moves played)
        private int _movesByX = 0;
        private int _movesByO = 0;

        // history stores board, player index, move number, move counts, and whether game was over
        private readonly Stack<(Board board, int playerIndex, int moveNumber, int movesX, int movesO, bool wasOver)> _history = new();

        public Board Board => _board;

        public bool IsOver => _isOver;

        public event EventHandler<string>? GameOver;

        public event EventHandler? BoardChanged;

        /// <summary>
        /// Try to place a quantum move between two cells (0‑based indices).
        /// </summary>
        public event EventHandler<List<Move>>? CycleDetected;

        public Func<int, List<Move>, Dictionary<Move,int>>? CollapseChooser;

        public bool CanUndo => _history.Count > 0;

        public void Reset()
        {
            _board = new Board();
            _currentPlayerIndex = 0;
            _moveNumber = 1;
            _movesByX = _movesByO = 0;
            _isOver = false;
            _history.Clear();
            BoardChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;
            var prev = _history.Pop();
            _board = prev.board;
            _currentPlayerIndex = prev.playerIndex;
            _moveNumber = prev.moveNumber;
            _movesByX = prev.movesX;
            _movesByO = prev.movesO;
            _isOver = prev.wasOver;
            BoardChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private Dictionary<Move,int> DefaultCollapseChooser(int chooser, List<Move> cycle)
        {
            // chooser picks which endpoint of the final (triggering) move collapses.
            var last = cycle[^1];
            Console.WriteLine($"Player {_players[chooser].Symbol}, choose collapse for the triggering move {last.Label}:");
            int chosenCell;
            while (true)
            {
                Console.WriteLine($"Cells {last.CellA+1} (A) or {last.CellB+1} (B)?");
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                char c = char.ToUpperInvariant(line[0]);
                if (c == 'A') { chosenCell = last.CellA; break; }
                if (c == 'B') { chosenCell = last.CellB; break; }
            }

            return OrientCycle(cycle, chosenCell);
        }

        /// <summary>
        /// Given an ordered list of moves forming a cycle (last element is the most
        /// recently added move) and the winning cell of that last move, compute a map
        /// associating every move with the cell it collapses to.  The orientation of the
        /// cycle is determined by walking from the loser endpoint of the last move toward
        /// the chosen winner, then continuing around the loop.
        /// </summary>
        internal static Dictionary<Move,int> OrientCycle(List<Move> cycle, int winnerCell)
        {
            // quick special-case: two moves that link the same pair of cells form a
            // trivial 2-cycle.  The chooser picks collapse for the new move; the
            // older move collapses to the opposite endpoint.
            if (cycle.Count == 2)
            {
                var older = cycle[0];
                var newer = cycle[1];
                var result = new Dictionary<Move,int>();
                result[newer] = winnerCell;
                result[older] = (older.CellA == winnerCell) ? older.CellB : older.CellA;
                return result;
            }

            // reconstruct the sequence of cells along the path returned by DetectCycle
            // (excluding the new move), starting from newMove.CellA.
            var cells = new List<int>();
            var newMove = cycle[^1];
            int start = newMove.CellA;
            int end = newMove.CellB;

            cells.Add(start);
            for (int i = 0; i < cycle.Count - 1; i++)
            {
                var move = cycle[i];
                int last = cells[^1];
                int next = move.CellA == last ? move.CellB : move.CellA;
                cells.Add(next);
            }

            // at this point cells.Last() should equal end; if not, the path was reversed
            if (cells[^1] != end)
            {
                cells.Reverse();
                (start, end) = (end, start);
            }

            cells.Add(start); // close loop

            int closingTo = cells[cells.Count - 1];
            if (closingTo != winnerCell)
            {
                cells.Reverse();
            }

            var map = new Dictionary<Move,int>();
            for (int i = 0; i < cells.Count - 1; i++)
            {
                int u = cells[i];
                int v = cells[i + 1];
                var move = cycle.First(m => (m.CellA == u && m.CellB == v) || (m.CellA == v && m.CellB == u));
                map[move] = v;
            }

            return map;
        }

        private void CheckForWin()
        {
            var winners = _board.GetWinners();
            if (winners.Count == 0)
                return;

            _isOver = true;
            string result;
            if (winners.Count == 1)
            {
                result = winners.First() + " wins!";
            }
            else
            {
                // both have a line
                if (_movesByX < _movesByO)
                    result = "X wins by fewer turns!";
                else if (_movesByO < _movesByX)
                    result = "O wins by fewer turns!";
                else
                    result = "Draw!";
            }

            GameOver?.Invoke(this, result);
        }

        public bool TryPlaceQuantumMove(int cellA, int cellB)
        {
            if (_isOver)
                return false;
            // save history before mutating board
            _history.Push((_board.Clone(), _currentPlayerIndex, _moveNumber, _movesByX, _movesByO, _isOver));

            // determine the player's own move number (X1, X2, O1, etc)
            int triggeringPlayer = _currentPlayerIndex;
            int playerMoveNumber = triggeringPlayer == 0 ? _movesByX + 1 : _movesByO + 1;
            var move = new Move(playerMoveNumber, _players[_currentPlayerIndex], cellA, cellB);

            // ask board whether this move causes a cycle before committing
            var cycle = _board.DetectCycle(move);
            bool placed = _board.TryPlaceQuantumMove(move);

            if (!placed)
                return false;

            // increment move count for current player
            if (triggeringPlayer == 0) _movesByX++; else _movesByO++;
            _moveNumber++;// continue tracking overall turn count but not used for labels
            _currentPlayerIndex = 1 - _currentPlayerIndex; // switch turn
            BoardChanged?.Invoke(this, EventArgs.Empty);

            if (cycle != null)
            {
                CycleDetected?.Invoke(this, cycle);
                int chooser = 1 - triggeringPlayer; // other player chooses
                var chooserFunc = CollapseChooser ?? DefaultCollapseChooser;
                var winners = chooserFunc(chooser, cycle);
                _board.CollapseCycle(cycle, winners);
                BoardChanged?.Invoke(this, EventArgs.Empty); // update after collapse
                CheckForWin();
            }

            return true;
        }

       
    }
}
