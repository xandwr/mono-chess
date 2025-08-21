using Godot;
using MonoChess.Chess.GUI;
using MonoChess.Chess.Core;
using System.Linq;

namespace MonoChess.Chess.Scripts
{

	public partial class Main : Control
	{
		private BoardView boardView;
		private VBoxContainer sidePanel;
		private Button resetButton;
		private Button perftButton;
		private Label statusLabel;
		private Label turnLabel;

		public override void _Ready()
		{
			SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

			// Create horizontal layout
			var hbox = new HBoxContainer();
			AddChild(hbox);

			// Create and add board view
			boardView = new BoardView();
			hbox.AddChild(boardView);

			// Create side panel
			CreateSidePanel();
			hbox.AddChild(sidePanel);

			UpdateUI();
		}

		private void CreateSidePanel()
		{
			sidePanel = new VBoxContainer();
			sidePanel.SetCustomMinimumSize(new Vector2(200, 400));

			// Title
			var title = new Label();
			title.Text = "MonoChess";
			title.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
			sidePanel.AddChild(title);

			sidePanel.AddChild(new HSeparator());

			// Turn indicator
			turnLabel = new Label();
			turnLabel.Text = "White to move";
			sidePanel.AddChild(turnLabel);

			sidePanel.AddChild(new HSeparator());

			// Reset button
			resetButton = new Button();
			resetButton.Text = "Reset Game";
			resetButton.Pressed += OnResetPressed;
			sidePanel.AddChild(resetButton);

			// Perft test button
			perftButton = new Button();
			perftButton.Text = "Run Perft Test";
			perftButton.Pressed += OnPerftPressed;
			sidePanel.AddChild(perftButton);

			sidePanel.AddChild(new HSeparator());

			// Status label
			statusLabel = new Label();
			statusLabel.Text = "Ready to play!";
			statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			sidePanel.AddChild(statusLabel);
		}

		private void OnResetPressed()
		{
			boardView.ResetBoard();
			UpdateUI();
			statusLabel.Text = "Game reset!";
		}

		private void OnPerftPressed()
		{
			statusLabel.Text = "Running Perft test...";

			// Run a quick perft test to verify the engine
			var testBoard = new Board();
			long result = Perft.Run(testBoard, 4);

			statusLabel.Text = $"Perft(4): {result} nodes\n(Expected: 197,281)";

			if (result == 197281)
			{
				statusLabel.Text += "\n✓ Engine working correctly!";
			}
			else
			{
				statusLabel.Text += "\n✗ Engine error detected!";
			}
		}

		public override void _Process(double delta)
		{
			UpdateUI();
		}

		private void UpdateUI()
		{
			if (boardView?.GetBoard() != null)
			{
				var board = boardView.GetBoard();
				string currentPlayer = board.SideToMove == Piece.WHITE ? "White" : "Black";
				turnLabel.Text = $"{currentPlayer} to move";

				// Check for game end conditions
				var moves = MoveGenerator.Generate(board);
				var legalMoves = moves.Where(move =>
				{
					board.MakeMove(move);
					int mover = board.SideToMove ^ 1;
					int kingSquare = board.KingSquare(mover);
					bool isLegal = kingSquare >= 0 && !MoveGenerator.IsSquareAttacked(board, kingSquare, board.SideToMove);
					board.UnmakeMove();
					return isLegal;
				}).ToList();

				if (legalMoves.Count == 0)
				{
					int kingSquare = board.KingSquare(board.SideToMove);
					bool inCheck = kingSquare >= 0 && MoveGenerator.IsSquareAttacked(board, kingSquare, 1 - board.SideToMove);

					if (inCheck)
					{
						statusLabel.Text = $"Checkmate! {(board.SideToMove == Piece.WHITE ? "Black" : "White")} wins!";
					}
					else
					{
						statusLabel.Text = "Stalemate! Draw.";
					}
				}
				else
				{
					int kingSquare = board.KingSquare(board.SideToMove);
					bool inCheck = kingSquare >= 0 && MoveGenerator.IsSquareAttacked(board, kingSquare, 1 - board.SideToMove);

					if (inCheck)
					{
						statusLabel.Text = $"{currentPlayer} is in check!";
					}
					else
					{
						statusLabel.Text = $"{legalMoves.Count} legal moves available";
					}
				}
			}
		}
	}
}
