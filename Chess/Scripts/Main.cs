using Godot;
using MonoChess.Chess.Core;

namespace MonoChess.Chess.Scripts
{
	public partial class Main : Node
	{
		public override void _Ready()
		{
			base._Ready();

			Board board = new();
			board.Print();

			for (int d = 1; d <= 5; d++)
			{
				long nodes = Perft.Run(board, d);
				GD.Print($"Perft({d}) = {nodes}");
			}
		}
	}
}
