using Godot;
using MonoChess.Chess.Core;
using System.Collections.Generic;
using System.Linq;

namespace MonoChess.Chess.GUI
{
    [Tool]
    public partial class BoardView : Control
    {
        [Signal] public delegate void MoveSoundEventHandler();
        [Signal] public delegate void CaptureSoundEventHandler();
        [Signal] public delegate void PlayerColorChangedEventHandler(int newPlayerColor);

        private Board board;
        private Vector2 squareSize = new Vector2(100, 100);
        private Vector2 boardOffset = new Vector2(0, 0);

        // Player color - which color the human is playing as
        private int playerColor = Piece.WHITE; // Default to white

        public int PlayerColor
        {
            get => playerColor;
            set
            {
                playerColor = value;
                EmitSignal(SignalName.PlayerColorChanged, playerColor);
                QueueRedraw();
            }
        }

        // Colors for the board squares - exported for editor customization
        [Export]
        public Color LightSquareColor
        {
            get => lightSquareColor;
            set
            {
                lightSquareColor = value;
                QueueRedraw(); // Auto-refresh in editor
            }
        }
        private Color lightSquareColor = new Color(0.93f, 0.93f, 0.82f); // Light cream

        [Export]
        public Color DarkSquareColor
        {
            get => darkSquareColor;
            set
            {
                darkSquareColor = value;
                QueueRedraw(); // Auto-refresh in editor
            }
        }
        private Color darkSquareColor = new Color(0.72f, 0.53f, 0.04f);  // Dark brown

        [Export]
        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;
                QueueRedraw(); // Auto-refresh in editor
            }
        }
        private Color borderColor = new Color(0.72f, 0.53f, 0.04f);  // Dark brown

        private Color highlightColor = new Color(1.0f, 1.0f, 0.0f, 0.6f); // Yellow highlight
        private Color validMoveColor = new Color(0.0f, 1.0f, 0.0f, 0.4f); // Green for valid moves
        private Color lastMoveColor = new Color(0.0f, 0.5f, 1.0f, 0.4f);  // Blue for last move

        // Piece textures
        private Dictionary<string, Texture2D> pieceTextures = new Dictionary<string, Texture2D>();

        // Dragging state
        private bool isDragging = false;
        private int draggedFromSquare = -1;
        private Vector2 dragOffset;
        private List<int> validMoves = new List<int>();
        private int lastMoveFrom = -1;
        private int lastMoveTo = -1;

        // UI state
        private int selectedSquare = -1;

        // Audio
        private AudioStreamPlayer moveSound = null;
        private AudioStreamPlayer captureSound = null;

        public override void _Ready()
        {
            board = new Board();
            LoadPieceTextures();
            SetCustomMinimumSize(new Vector2(800, 800));

            moveSound = GetNode<AudioStreamPlayer>("MoveSound");
            captureSound = GetNode<AudioStreamPlayer>("CaptureSound");
        }

        private void LoadPieceTextures()
        {
            string[] colors = { "w", "b" };
            string[] pieces = { "P", "N", "B", "R", "Q", "K" };

            foreach (string color in colors)
            {
                foreach (string piece in pieces)
                {
                    string key = $"{color}{piece}";
                    string path = $"res://Textures/Pieces/{key}.png";

                    if (ResourceLoader.Exists(path))
                    {
                        pieceTextures[key] = GD.Load<Texture2D>(path);
                    }
                    else
                    {
                        GD.PrintErr($"Missing piece texture: {path}");
                    }
                }
            }
        }

        public override void _Draw()
        {
            if (board == null || pieceTextures.Count == 0)
                return;

            DrawBoard();

            // Only draw highlights and pieces when not in editor
            if (!Engine.IsEditorHint())
            {
                DrawHighlights();
                DrawPieces();
            }
        }

        private void DrawBoard()
        {
            // Draw the 8x8 board
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    Vector2 pos = GetSquarePosition(rank * 8 + file);
                    Color color = (rank + file) % 2 == 0 ? lightSquareColor : darkSquareColor;
                    DrawRect(new Rect2(pos, squareSize), color);
                }
            }

            // Draw board border
            DrawRect(new Rect2(boardOffset - Vector2.One * 10, squareSize * 8 + Vector2.One * 20), borderColor, false, 20);
        }

        private void DrawHighlights()
        {
            // Draw last move highlight
            if (lastMoveFrom >= 0)
            {
                Vector2 fromPos = GetSquarePosition(lastMoveFrom);
                DrawRect(new Rect2(fromPos, squareSize), lastMoveColor);
            }
            if (lastMoveTo >= 0)
            {
                Vector2 toPos = GetSquarePosition(lastMoveTo);
                DrawRect(new Rect2(toPos, squareSize), lastMoveColor);
            }

            // Draw selected square highlight
            if (selectedSquare >= 0)
            {
                Vector2 pos = GetSquarePosition(selectedSquare);
                DrawRect(new Rect2(pos, squareSize), highlightColor);
            }

            // Draw valid move indicators
            foreach (int move in validMoves)
            {
                int to = Move.To(move);
                Vector2 pos = GetSquarePosition(to);

                // Different indicator based on whether it's a capture
                int captured = Move.Captured(move);
                if (captured != Piece.EMPTY)
                {
                    // Ring for captures
                    DrawArc(pos + squareSize / 2, squareSize.X * 0.35f, 0, Mathf.Tau, 32, validMoveColor, 6);
                }
                else
                {
                    // Dot for empty squares
                    DrawCircle(pos + squareSize / 2, squareSize.X * 0.15f, validMoveColor);
                }
            }
        }

        private void DrawPieces()
        {
            if (board == null)
                return;

            for (int square = 0; square < 64; square++)
            {
                if (isDragging && square == draggedFromSquare)
                    continue;

                int piece = board.GetPieceAt(square);
                if (piece == Piece.EMPTY)
                    continue;

                int color = board.IsOccupied(square, Piece.WHITE) ? Piece.WHITE : Piece.BLACK;
                string textureKey = GetPieceTextureKey(piece, color);

                if (pieceTextures.ContainsKey(textureKey))
                {
                    Vector2 pos = GetSquarePosition(square);
                    Rect2 targetRect = new Rect2(pos, squareSize);
                    DrawTextureRect(pieceTextures[textureKey], targetRect, false); // false = keep aspect
                }
            }

            // Draw dragged piece at mouse position
            if (isDragging && draggedFromSquare >= 0)
            {
                int piece = board.GetPieceAt(draggedFromSquare);
                if (piece != Piece.EMPTY)
                {
                    int color = board.IsOccupied(draggedFromSquare, Piece.WHITE) ? Piece.WHITE : Piece.BLACK;
                    string textureKey = GetPieceTextureKey(piece, color);

                    if (pieceTextures.ContainsKey(textureKey))
                    {
                        // Use local mouse position instead of global
                        Vector2 mousePos = GetLocalMousePosition() - dragOffset;
                        Rect2 targetRect = new Rect2(mousePos, squareSize);
                        DrawTextureRect(pieceTextures[textureKey], targetRect, false);
                    }
                }
            }
        }

        private string GetPieceTextureKey(int piece, int color)
        {
            string colorStr = color == Piece.WHITE ? "w" : "b";
            string pieceStr = piece switch
            {
                Piece.PAWN => "P",
                Piece.KNIGHT => "N",
                Piece.BISHOP => "B",
                Piece.ROOK => "R",
                Piece.QUEEN => "Q",
                Piece.KING => "K",
                _ => ""
            };
            return $"{colorStr}{pieceStr}";
        }


        private Vector2 GetSquarePosition(int square)
        {
            int file = square % 8;
            int rank = square / 8;

            // Flip the board when playing as Black
            if (playerColor == Piece.BLACK)
            {
                file = 7 - file;  // Flip files (a-h becomes h-a)
            }
            else
            {
                rank = 7 - rank;  // Flip ranks (so White's back rank is at bottom)
            }

            return boardOffset + new Vector2(file * squareSize.X, rank * squareSize.Y);
        }

        private int GetSquareFromPosition(Vector2 pos)
        {
            Vector2 relative = pos - boardOffset;
            if (relative.X < 0 || relative.Y < 0 || relative.X >= squareSize.X * 8 || relative.Y >= squareSize.Y * 8)
                return -1;

            int file = (int)(relative.X / squareSize.X);
            int rank = (int)(relative.Y / squareSize.Y);

            // Flip coordinates when playing as Black
            if (playerColor == Piece.BLACK)
            {
                file = 7 - file;  // Flip files back
                                  // rank stays as-is
            }
            else
            {
                rank = 7 - rank;  // Flip ranks back
            }

            if (file < 0 || file > 7 || rank < 0 || rank > 7)
                return -1;

            return rank * 8 + file;
        }

        public override void _GuiInput(InputEvent @event)
        {
            // Only handle input when not in editor
            if (Engine.IsEditorHint())
                return;

            if (@event is InputEventMouseButton mouseButton)
            {
                HandleMouseButton(mouseButton);
            }
            else if (@event is InputEventMouseMotion mouseMotion)
            {
                HandleMouseMotion(mouseMotion);
            }
        }

        private void HandleMouseButton(InputEventMouseButton mouseButton)
        {
            // Only allow input when it's the player's turn
            if (board.SideToMove != playerColor)
                return;

            int square = GetSquareFromPosition(mouseButton.Position);

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    // Mouse down
                    if (square >= 0)
                    {
                        int piece = board.GetPieceAt(square);
                        if (piece != Piece.EMPTY && board.IsOccupied(square, board.SideToMove))
                        {
                            // Start dragging our own piece
                            StartDrag(square, mouseButton.Position);
                        }
                        else if (selectedSquare >= 0)
                        {
                            // Try to move selected piece
                            TryMove(selectedSquare, square);
                        }
                    }
                }
                else
                {
                    // Mouse up
                    if (isDragging)
                    {
                        EndDrag(mouseButton.Position);
                    }
                }
            }
        }

        private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
        {
            if (isDragging)
            {
                QueueRedraw(); // Redraw to update dragged piece position
            }
        }

        private void StartDrag(int square, Vector2 mousePos)
        {
            isDragging = true;
            draggedFromSquare = square;
            selectedSquare = square;

            // Use local coordinates for offset
            Vector2 squarePos = GetSquarePosition(square);
            dragOffset = mousePos - squarePos;

            GenerateValidMovesFromSquare(square);
            QueueRedraw();
        }

        private void EndDrag(Vector2 mousePos)
        {
            int targetSquare = GetSquareFromPosition(mousePos);

            if (targetSquare >= 0 && targetSquare != draggedFromSquare)
            {
                TryMove(draggedFromSquare, targetSquare);
            }

            isDragging = false;
            draggedFromSquare = -1;
            QueueRedraw();
        }

        private void GenerateValidMovesFromSquare(int fromSquare)
        {
            validMoves.Clear();

            var allMoves = MoveGenerator.Generate(board);

            foreach (int move in allMoves)
            {
                if (Move.From(move) == fromSquare)
                {
                    // Test if move is legal by making it and checking if king is in check
                    board.MakeMove(move);

                    int mover = board.SideToMove ^ 1; // Side that just moved
                    int kingSquare = board.KingSquare(mover);
                    bool isLegal = kingSquare >= 0 && !MoveGenerator.IsSquareAttacked(board, kingSquare, board.SideToMove);

                    board.UnmakeMove();

                    if (isLegal)
                    {
                        validMoves.Add(move);
                    }
                }
            }
        }

        private void TryMove(int fromSquare, int toSquare)
        {
            // Find the move in our valid moves list
            int moveToMake = -1;
            var candidateMoves = validMoves.Where(m => Move.From(m) == fromSquare && Move.To(m) == toSquare).ToList();

            if (candidateMoves.Count == 1)
            {
                moveToMake = candidateMoves[0];
            }
            else if (candidateMoves.Count > 1)
            {
                // Multiple moves possible (probably pawn promotion)
                // For now, default to queen promotion
                moveToMake = candidateMoves.FirstOrDefault(m => Move.Promo(m) == Piece.QUEEN);
                if (moveToMake == -1)
                    moveToMake = candidateMoves[0];
            }

            if (moveToMake != -1)
            {
                int captured = Move.Captured(moveToMake);

                // Make the move
                board.MakeMove(moveToMake);

                // Update last move highlighting
                lastMoveFrom = fromSquare;
                lastMoveTo = toSquare;

                // Clear selection and valid moves
                selectedSquare = -1;
                validMoves.Clear();

                QueueRedraw();

                // Emit sounds
                if (captured != Piece.EMPTY)
                {
                    EmitSignal(SignalName.CaptureSound);
                    captureSound.Play();
                }
                else
                {
                    EmitSignal(SignalName.MoveSound);
                    moveSound.Play();
                }

                // Debug print
                GD.Print($"Move: {SquareToString(fromSquare)}{SquareToString(toSquare)}");
            }

            else
            {
                // Invalid move - clear selection
                selectedSquare = -1;
                validMoves.Clear();
                QueueRedraw();
            }
        }

        private string SquareToString(int square)
        {
            int file = square % 8;
            int rank = square / 8;
            return $"{(char)('a' + file)}{rank + 1}";
        }

        // Public methods for external control
        public void ResetBoard()
        {
            board = new Board();
            selectedSquare = -1;
            validMoves.Clear();
            lastMoveFrom = -1;
            lastMoveTo = -1;
            isDragging = false;
            draggedFromSquare = -1;
            QueueRedraw();
        }

        public void SwitchPlayerColor()
        {
            PlayerColor = playerColor == Piece.WHITE ? Piece.BLACK : Piece.WHITE;

            // Clear any current selection since it's no longer the player's turn
            selectedSquare = -1;
            validMoves.Clear();
            isDragging = false;
            draggedFromSquare = -1;

            GD.Print($"Player now playing as: {(playerColor == Piece.WHITE ? "White" : "Black")}");
            QueueRedraw();
        }

        public Board GetBoard()
        {
            return board;
        }

        public void SetBoard(Board newBoard)
        {
            board = newBoard;
            selectedSquare = -1;
            validMoves.Clear();
            lastMoveFrom = -1;
            lastMoveTo = -1;
            isDragging = false;
            draggedFromSquare = -1;
            QueueRedraw();
        }
    }
}