using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumTicTacToe
{
    public class Game
    {
        private Board _board = new Board();

        private readonly List<Player> _players = new()
        {
            new Player("X"),
            new Player("O")
        };
        public Func<int, List<Move>, Dictionary<Move, int>>? CollapseChooser;
        private int _currentPlayerIndex = 0;
        private int _moveNumber = 1;
        private bool _isOver = false;

        private int _movesByX = 0;
        private int _movesByO = 0;

        private readonly Stack<(Board board, int playerIndex, int moveNumber, int movesX, int movesO, bool wasOver)> _history = new();

        public Board Board => _board;
        public bool IsOver => _isOver;
        public bool AutoCollapse { get; set; } = true;
        public string CurrentPlayerSymbol => _players[_currentPlayerIndex].Symbol;

        public event EventHandler<string>? GameOver;
        public event EventHandler? BoardChanged;
        public event EventHandler<List<Move>>? CycleDetected;

        public bool CanUndo => _history.Count > 0;

        public void Reset()
        {
            _board = new Board();
            _currentPlayerIndex = 0;
            _moveNumber = 1;
            _movesByX = 0;
            _movesByO = 0;
            _isOver = false;
            _history.Clear();
            BoardChanged?.Invoke(this, EventArgs.Empty);
        }
        private Dictionary<Move, int> DefaultCollapseChooser(int chooser, List<Move> cycle)
        {
            var last = cycle[^1];

            Console.WriteLine($"Player {_players[chooser].Symbol}, choose collapse for move {last.Label}");
            Console.WriteLine($"Choose {last.CellA + 1} or {last.CellB + 1}");

            while (true)
            {
                var input = Console.ReadLine();
                if (int.TryParse(input, out int choice))
                {
                    choice--;
                    if (choice == last.CellA || choice == last.CellB)
                        return OrientCycle(cycle, choice);
                }
            }
        }
        public bool TryPlaceQuantumMove(int cellA, int cellB)
        {
            if (_isOver)
                return false;

            _history.Push((_board.Clone(), _currentPlayerIndex, _moveNumber, _movesByX, _movesByO, _isOver));

            int triggeringPlayer = _currentPlayerIndex;
            int playerMoveNumber = triggeringPlayer == 0 ? _movesByX + 1 : _movesByO + 1;

            var move = new Move(playerMoveNumber, _players[_currentPlayerIndex], cellA, cellB);

            var cycle = _board.DetectCycle(move);
            bool placed = _board.TryPlaceQuantumMove(move);

            if (!placed)
                return false;

            if (triggeringPlayer == 0) _movesByX++; else _movesByO++;

            _moveNumber++;
            _currentPlayerIndex = 1 - _currentPlayerIndex;

            BoardChanged?.Invoke(this, EventArgs.Empty);

            if (cycle != null)
            {
                CycleDetected?.Invoke(this, cycle);

                if (AutoCollapse)
                {
                    int chooser = 1 - triggeringPlayer;

                    var chooserFunc = CollapseChooser ?? DefaultCollapseChooser;
                    var winners = chooserFunc(chooser, cycle);

                    _board.CollapseCycle(cycle, winners);
                    BoardChanged?.Invoke(this, EventArgs.Empty);
                    CheckForWin();
                }
            }

            return true;
        }

        public void ApplyCollapse(List<Move> cycle, Dictionary<Move, int> winnerByMove)
        {
            _board.CollapseCycle(cycle, winnerByMove);
            BoardChanged?.Invoke(this, EventArgs.Empty);
            CheckForWin();
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

        private void CheckForWin()
        {
            var winners = _board.GetWinners();
            
            // Check for winner
            if (winners.Count > 0)
            {
                _isOver = true;

                string result;
                if (winners.Count == 1)
                    result = winners.First() + " wins!";
                else
                    result = _movesByX < _movesByO ? "X wins by fewer turns!"
                            : _movesByO < _movesByX ? "O wins by fewer turns!"
                            : "Draw!";

                GameOver?.Invoke(this, result);
                return;
            }

            // Check for draw (board full, no winner)
            if (_board.IsFull())
            {
                _isOver = true;
                GameOver?.Invoke(this, "Draw - Board Full!");
            }
        }

        public static Dictionary<Move, int> OrientCycle(List<Move> cycle, int winnerCell)
        {
            // special-case: two moves linking the same pair of cells
            if (cycle.Count == 2)
            {
                var older = cycle[0];
                var newer = cycle[1];
                return new Dictionary<Move, int>
                {
                    [newer] = winnerCell,
                    [older] = older.CellA == winnerCell ? older.CellB : older.CellA
                };
            }

            // rebuild ordered cell loop from the path portion of the cycle
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

            // if the path was reversed, flip it
            if (cells[^1] != end)
            {
                cells.Reverse();
                (start, end) = (end, start);
            }

            // close the loop and orient toward the chosen winner
            cells.Add(start);
            if (cells[^1] != winnerCell)
            {
                cells.Reverse();
            }

            var map = new Dictionary<Move, int>();
            for (int i = 0; i < cells.Count - 1; i++)
            {
                int u = cells[i];
                int v = cells[i + 1];
                var move = cycle.First(m => (m.CellA == u && m.CellB == v) || (m.CellA == v && m.CellB == u));
                map[move] = v;
            }

            return map;
        }
    }
}
