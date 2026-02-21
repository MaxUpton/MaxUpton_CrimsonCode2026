using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuantumTicTacToe
{
    public class MainForm : Form
    {
        private readonly Game _game;
        private readonly Button[] _cellButtons = new Button[9];
        private readonly Label _statusLabel;
        private Button? _undoButton;
        private int? _firstSelection;

        public MainForm()
        {
            Text = "Quantum Tic-Tac-Toe";
            ClientSize = new Size(300, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            _game = new Game();
            _game.BoardChanged += OnBoardChanged;
            _game.CycleDetected += OnCycleDetected;
            _game.GameOver += OnGameOver;

            // when a cycle occurs, the non‑triggering player chooses collapse outcomes
            _game.CollapseChooser = (chooserIndex, cycle) =>
            {
                var last = cycle[^1];
                var prompt = $"Chooser: select collapse for triggering move {last.Label}\n" +
                             $"Yes = cell {last.CellA + 1}, No = cell {last.CellB + 1}.";
                var result = MessageBox.Show(prompt, "Collapse", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                int winnerCell = result == DialogResult.Yes ? last.CellA : last.CellB;
                return Game.OrientCycle(cycle, winnerCell);
            };

            // toolbar with controls
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight,
            };
            var newGameBtn = new Button { Text = "New Game", AutoSize = true };
            newGameBtn.Click += (s,e) => { _game.Reset(); _firstSelection = null; UpdateBoardDisplay(); };
            var undoBtn = new Button { Text = "Undo", AutoSize = true, Enabled = false };
            undoBtn.Click += (s,e) =>
            {
                if (_game.Undo())
                {
                    _firstSelection = null;
                    UpdateBoardDisplay();
                }
            };
            toolbar.Controls.Add(newGameBtn);
            toolbar.Controls.Add(undoBtn);
            Controls.Add(toolbar);

            // store reference to undo button for enabling/disabling later
            _undoButton = undoBtn;

            // layout: simple grid of buttons
            var panel = new TableLayoutPanel
            {
                RowCount = 3,
                ColumnCount = 3,
                Dock = DockStyle.Top,
                Height = 300,
            };

            for (int i = 0; i < 3; i++)
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            for (int j = 0; j < 3; j++)
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            for (int i = 0; i < 9; i++)
            {
                var btn = new Button
                {
                    Dock = DockStyle.Fill,
                    Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                    Tag = i,
                };
                btn.Click += CellButton_Click;
                _cellButtons[i] = btn;
                panel.Controls.Add(btn, i % 3, i / 3);
            }

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
            };

            Controls.Add(panel);
            Controls.Add(_statusLabel);

            UpdateBoardDisplay();
        }

        private void OnBoardChanged(object? sender, EventArgs e)
        {
            UpdateBoardDisplay();
        }

        private void OnCycleDetected(object? sender, List<Move> cycle)
        {
            // simple alert for now
            string moves = string.Join(" -> ", cycle.Select(m => m.Label));
            MessageBox.Show($"Cycle detected:\n{moves}", "Collapse pending", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnGameOver(object? sender, string message)
        {
            MessageBox.Show(message, "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateBoardDisplay()
        {
            for (int i = 0; i < 9; i++)
            {
                _cellButtons[i].Text = _game.Board.GetCellDisplay(i);
                _cellButtons[i].Enabled = !_game.IsOver;
            }

            if (_firstSelection.HasValue)
                _statusLabel.Text = $"First cell selected: {_firstSelection.Value + 1}. Choose second cell.";
            else
                _statusLabel.Text = "Click a cell to begin your quantum move.";

            if (_undoButton != null)
                _undoButton.Enabled = _game.CanUndo;
        }

        private void CellButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                if (!_firstSelection.HasValue)
                {
                    _firstSelection = index;
                    UpdateBoardDisplay();
                    return;
                }

                // second selection
                int second = index;
                if (_firstSelection == second)
                {
                    MessageBox.Show("You must select two different cells.", "Invalid move", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _firstSelection = null;
                    UpdateBoardDisplay();
                    return;
                }

                bool ok = _game.TryPlaceQuantumMove(_firstSelection.Value, second);
                if (!ok)
                    MessageBox.Show("Cannot place move here (cell may be collapsed).", "Invalid move", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                _firstSelection = null;
                UpdateBoardDisplay();
            }
        }
    }
}
