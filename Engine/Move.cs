namespace QuantumTicTacToe
{
    public class Move
    {
        public int Number { get; }
        public Player Player { get; }
        public int CellA { get; }
        public int CellB { get; }
        public string Label => $"{Player.Symbol}{Number}";

        public Move(int number, Player player, int a, int b)
        {
            Number = number;
            Player = player;
            CellA = a;
            CellB = b;
        }
    }
}
