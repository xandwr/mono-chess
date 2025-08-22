using Godot;
using MonoChess.Chess.Core;
using MonoChess.Chess.AI;
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
        [Signal] public delegate void GameOverEventHandler(string result);
        [Signal] public delegate void BonusChipsEarnedEventHandler(int amount);
        [Signal] public delegate void PredictionPhaseChangedEventHandler(bool inPredictionPhase);

        private Board board;
        private Vector2 squareSize = new(100, 100);
        private Vector2 boardOffset = new(0, 0);

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
        private Color lightSquareColor = new(0.93f, 0.93f, 0.82f); // Light cream

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
        private Color darkSquareColor = new(0.72f, 0.53f, 0.04f);  // Dark brown

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
        private Color borderColor = new(0.72f, 0.53f, 0.04f);  // Dark brown

        private Color highlightColor = new(1.0f, 1.0f, 0.0f, 0.6f); // Yellow highlight
        private Color validMoveColor = new(0.0f, 1.0f, 0.0f, 0.4f); // Green for valid moves
        private Color lastMoveColor = new(0.0f, 0.5f, 1.0f, 0.4f);  // Blue for last move
        private Color predictionColor = new(1.0f, 0.0f, 1.0f, 0.6f); // Magenta for predictions

        // Piece textures
        private readonly Dictionary<string, Texture2D> pieceTextures = [];

        // Dragging state
        private bool isDragging = false;
        private int draggedFromSquare = -1;
        private Vector2 dragOffset;
        private readonly List<int> validMoves = [];
        private int lastMoveFrom = -1;
        private int lastMoveTo = -1;

        // UI state
        private int selectedSquare = -1;

        // Prediction system
        private bool isInPredictionPhase = false;
        private bool isPredictingDrag = false;
        private int predictedFromSquare = -1;
        private int predictedToSquare = -1;
        private int predictedPiece = Piece.EMPTY;
        private readonly List<int> validPredictionMoves = [];
        private bool predictionConfirmed = false;
        private int bonusChips = 0;

        // Game state tracking
        private enum GamePhase
        {
            PlayerPrediction,    // Player is making prediction for AI move
            PlayerMove,          // Player is making their own move
            AIMove,              // AI is making its move
            PredictionReveal     // Revealing prediction results
        }
        private GamePhase currentPhase = GamePhase.PlayerMove;

        // Audio
        private AudioStreamPlayer moveSound = null;
        private AudioStreamPlayer captureSound = null;

        // AI
        private bool aiEnabled = true; // Set to true to enable AI opponent
        private Timer aiMoveTimer;
        private bool waitingForAI = false;

        public override void _Ready()
        {
            board = new Board();
            LoadPieceTextures();
            SetCustomMinimumSize(new Vector2(800, 800));

            moveSound = GetNode<AudioStreamPlayer>("MoveSound");
            captureSound = GetNode<AudioStreamPlayer>("CaptureSound");

            // Setup AI timer
            aiMoveTimer = new()
            {
                WaitTime = 1.0f, // 1 second delay for AI moves
                OneShot = true
            };

            aiMoveTimer.Timeout += OnAITimerTimeout;
            AddChild(aiMoveTimer);

            // Start with appropriate phase
            DetermineInitialPhase();
        }

        private void DetermineInitialPhase()
        {
            if (aiEnabled && board.SideToMove != playerColor)
            {
                // AI's turn - start prediction phase
                currentPhase = GamePhase.PlayerPrediction;
                isInPredictionPhase = true;
                EmitSignal(SignalName.PredictionPhaseChanged, true);
            }
            else
            {
                // Player's turn
                currentPhase = GamePhase.PlayerMove;
                isInPredictionPhase = false;
                EmitSignal(SignalName.PredictionPhaseChanged, false);
            }
        }

        private void OnAITimerTimeout()
        {
            waitingForAI = false;

            if (currentPhase == GamePhase.AIMove)
            {
                int aiMove = SimpleAI.GetWeightedMove(board);

                if (aiMove != -1)
                {
                    // Make the AI move
                    int captured = Move.Captured(aiMove);
                    board.MakeMove(aiMove);

                    // Update last move highlighting
                    lastMoveFrom = Move.From(aiMove);
                    lastMoveTo = Move.To(aiMove);

                    QueueRedraw();

                    // Play appropriate sound
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
                    GD.Print($"AI Move: {SquareToString(Move.From(aiMove))}{SquareToString(Move.To(aiMove))}");

                    // Check prediction
                    CheckPredictionAndAwardChips(aiMove);

                    // Check for game over
                    if (SimpleAI.IsGameOver(board))
                    {
                        CheckForGameOver();
                    }
                    else
                    {
                        // Move to next phase (player prediction for next AI move or player move)
                        if (board.SideToMove == playerColor)
                        {
                            currentPhase = GamePhase.PlayerMove;
                            isInPredictionPhase = false;
                            EmitSignal(SignalName.PredictionPhaseChanged, false);
                        }
                        else
                        {
                            // Another AI turn coming up
                            currentPhase = GamePhase.PlayerPrediction;
                            isInPredictionPhase = true;
                            EmitSignal(SignalName.PredictionPhaseChanged, true);
                            ClearPrediction();
                        }
                    }
                }
                else
                {
                    // AI has no legal moves - game over
                    CheckForGameOver();
                }
            }
        }

        private void CheckPredictionAndAwardChips(int aiMove)
        {
            if (predictionConfirmed && predictedFromSquare >= 0 && predictedToSquare >= 0)
            {
                int actualFrom = Move.From(aiMove);
                int actualTo = Move.To(aiMove);

                if (actualFrom == predictedFromSquare && actualTo == predictedToSquare)
                {
                    // Correct prediction!
                    bonusChips++;
                    EmitSignal(SignalName.BonusChipsEarned, 1); // Fixed: uncommented and corrected signal name
                    GD.Print($"Correct prediction! Earned 1 bonus chip. Total: {bonusChips}");
                }
                else
                {
                    GD.Print($"Prediction incorrect. Predicted: {SquareToString(predictedFromSquare)}->{SquareToString(predictedToSquare)}, Actual: {SquareToString(actualFrom)}->{SquareToString(actualTo)}");
                }
            }

            ClearPrediction();
        }

        private void ClearPrediction()
        {
            predictedFromSquare = -1;
            predictedToSquare = -1;
            predictedPiece = Piece.EMPTY;
            predictionConfirmed = false;
            isPredictingDrag = false;
            validPredictionMoves.Clear();
            QueueRedraw();
        }

        public void ConfirmPrediction()
        {
            if (currentPhase == GamePhase.PlayerPrediction && predictedFromSquare >= 0 && predictedToSquare >= 0)
            {
                predictionConfirmed = true;
                currentPhase = GamePhase.AIMove;
                isInPredictionPhase = false;
                EmitSignal(SignalName.PredictionPhaseChanged, false);

                // Start AI move timer
                waitingForAI = true;
                aiMoveTimer.Start();

                GD.Print($"Prediction confirmed: {SquareToString(predictedFromSquare)}->{SquareToString(predictedToSquare)}");
            }
        }

        public int GetBonusChips() => bonusChips;

        public void SpendBonusChips(int amount)
        {
            bonusChips = Mathf.Max(0, bonusChips - amount);
        }

        private void CheckForGameOver()
        {
            if (SimpleAI.IsGameOver(board))
            {
                string result;
                if (SimpleAI.IsCheckmate(board))
                {
                    int winner = 1 - board.SideToMove; // The side that just moved won
                    result = winner == Piece.WHITE ? "White wins by checkmate!" : "Black wins by checkmate!";
                }
                else if (SimpleAI.IsStalemate(board))
                {
                    result = "Draw by stalemate!";
                }
                else
                {
                    result = "Game over!";
                }

                GD.Print(result);
                EmitSignal(SignalName.GameOver, result);
            }
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
                DrawPrediction();
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

        private void DrawPrediction()
        {
            if (!isInPredictionPhase)
                return;

            // Draw prediction move highlight
            if (predictedFromSquare >= 0)
            {
                Vector2 fromPos = GetSquarePosition(predictedFromSquare);
                DrawRect(new Rect2(fromPos, squareSize), predictionColor);
            }
            if (predictedToSquare >= 0)
            {
                Vector2 toPos = GetSquarePosition(predictedToSquare);
                DrawRect(new Rect2(toPos, squareSize), predictionColor);
            }

            // Draw valid prediction move indicators
            foreach (int move in validPredictionMoves)
            {
                int to = Move.To(move);
                Vector2 pos = GetSquarePosition(to);

                // Different indicator based on whether it's a capture
                int captured = Move.Captured(move);
                if (captured != Piece.EMPTY)
                {
                    // Ring for captures
                    DrawArc(pos + squareSize / 2, squareSize.X * 0.35f, 0, Mathf.Tau, 32, predictionColor, 6);
                }
                else
                {
                    // Dot for empty squares
                    DrawCircle(pos + squareSize / 2, squareSize.X * 0.15f, predictionColor);
                }
            }

            // Draw predicted piece at destination (transparent)
            if (predictedFromSquare >= 0 && predictedToSquare >= 0 && predictedPiece != Piece.EMPTY)
            {
                int opponentColor = 1 - playerColor;
                string textureKey = GetPieceTextureKey(predictedPiece, opponentColor);

                if (pieceTextures.ContainsKey(textureKey))
                {
                    Vector2 pos = GetSquarePosition(predictedToSquare);
                    Rect2 targetRect = new Rect2(pos, squareSize);

                    // Draw with transparency
                    Color modulate = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                    DrawTextureRect(pieceTextures[textureKey], targetRect, false, modulate);
                }
            }
        }

        private void DrawHighlights()
        {
            // Don't draw normal highlights during prediction phase
            if (isInPredictionPhase)
                return;

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

                // Don't draw piece at prediction from square during prediction phase
                if (isInPredictionPhase && isPredictingDrag && square == predictedFromSquare)
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
                    DrawTextureRect(pieceTextures[textureKey], targetRect, false);
                }
            }

            // Draw dragged piece at mouse position (for both normal moves and predictions)
            if ((isDragging && draggedFromSquare >= 0) || (isPredictingDrag && predictedFromSquare >= 0))
            {
                int piece, color;

                if (isDragging && !isInPredictionPhase)
                {
                    piece = board.GetPieceAt(draggedFromSquare);
                    color = board.IsOccupied(draggedFromSquare, Piece.WHITE) ? Piece.WHITE : Piece.BLACK;
                }
                else if (isPredictingDrag)
                {
                    piece = predictedPiece;
                    color = 1 - playerColor; // Opponent color
                }
                else
                {
                    return;
                }

                if (piece != Piece.EMPTY)
                {
                    string textureKey = GetPieceTextureKey(piece, color);

                    if (pieceTextures.ContainsKey(textureKey))
                    {
                        Vector2 mousePos = GetLocalMousePosition() - dragOffset;
                        Rect2 targetRect = new Rect2(mousePos, squareSize);

                        Color modulate = isPredictingDrag ? new Color(1.0f, 1.0f, 1.0f, 0.7f) : Color.Color8(255, 255, 255);
                        DrawTextureRect(pieceTextures[textureKey], targetRect, false, modulate);
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
            int square = GetSquareFromPosition(mouseButton.Position);

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    // Mouse down
                    if (isInPredictionPhase)
                    {
                        HandlePredictionMouseDown(square, mouseButton.Position);
                    }
                    else if (currentPhase == GamePhase.PlayerMove && board.SideToMove == playerColor)
                    {
                        HandlePlayerMouseDown(square, mouseButton.Position);
                    }
                }
                else
                {
                    // Mouse up
                    if (isInPredictionPhase && isPredictingDrag)
                    {
                        EndPredictionDrag(mouseButton.Position);
                    }
                    else if (isDragging)
                    {
                        EndDrag(mouseButton.Position);
                    }
                }
            }
        }

        private void HandlePredictionMouseDown(int square, Vector2 mousePos)
        {
            if (square >= 0)
            {
                int piece = board.GetPieceAt(square);
                int opponentColor = 1 - playerColor;

                if (piece != Piece.EMPTY && board.IsOccupied(square, opponentColor))
                {
                    // Start predicting move for opponent's piece
                    StartPredictionDrag(square, piece, mousePos);
                }
            }
        }

        private void HandlePlayerMouseDown(int square, Vector2 mousePos)
        {
            if (square >= 0)
            {
                int piece = board.GetPieceAt(square);
                if (piece != Piece.EMPTY && board.IsOccupied(square, board.SideToMove))
                {
                    // Start dragging our own piece
                    StartDrag(square, mousePos);
                }
                else if (selectedSquare >= 0)
                {
                    // Try to move selected piece
                    TryMove(selectedSquare, square);
                }
            }
        }

        private void StartPredictionDrag(int square, int piece, Vector2 mousePos)
        {
            isPredictingDrag = true;
            predictedFromSquare = square;
            predictedPiece = piece;

            Vector2 squarePos = GetSquarePosition(square);
            dragOffset = mousePos - squarePos;

            GenerateValidPredictionMovesFromSquare(square);
            QueueRedraw();
        }

        private void EndPredictionDrag(Vector2 mousePos)
        {
            int targetSquare = GetSquareFromPosition(mousePos);

            if (targetSquare >= 0 && targetSquare != predictedFromSquare)
            {
                // Check if this is a valid prediction move
                bool validPrediction = validPredictionMoves.Any(m =>
                    Move.From(m) == predictedFromSquare && Move.To(m) == targetSquare);

                if (validPrediction)
                {
                    predictedToSquare = targetSquare;
                    GD.Print($"Prediction set: {SquareToString(predictedFromSquare)}->{SquareToString(predictedToSquare)}");
                }
                else
                {
                    // Invalid prediction - clear it
                    predictedFromSquare = -1;
                    predictedPiece = Piece.EMPTY;
                    validPredictionMoves.Clear();
                }
            }
            else
            {
                // Didn't drop on valid square - clear prediction
                predictedFromSquare = -1;
                predictedPiece = Piece.EMPTY;
                validPredictionMoves.Clear();
            }

            isPredictingDrag = false;
            QueueRedraw();
        }

        private void GenerateValidPredictionMovesFromSquare(int fromSquare)
        {
            validPredictionMoves.Clear();

            // Generate moves for the opponent
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
                        validPredictionMoves.Add(move);
                    }
                }
            }
        }

        private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
        {
            if (isDragging || isPredictingDrag)
            {
                QueueRedraw(); // Redraw to update dragged piece position
            }
        }

        private void StartDrag(int square, Vector2 mousePos)
        {
            isDragging = true;
            draggedFromSquare = square;
            selectedSquare = square;

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
                GD.Print($"Player Move: {SquareToString(fromSquare)}{SquareToString(toSquare)}");

                // Check for game over
                if (SimpleAI.IsGameOver(board))
                {
                    CheckForGameOver();
                }
                else
                {
                    // Move to next phase
                    if (aiEnabled && board.SideToMove != playerColor)
                    {
                        // AI's turn - start prediction phase
                        currentPhase = GamePhase.PlayerPrediction;
                        isInPredictionPhase = true;
                        EmitSignal(SignalName.PredictionPhaseChanged, true);
                        ClearPrediction();
                    }
                }
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
            waitingForAI = false;
            aiMoveTimer.Stop();
            ClearPrediction();
            DetermineInitialPhase();
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
            waitingForAI = false;
            aiMoveTimer.Stop();
            ClearPrediction();

            GD.Print($"Player now playing as: {(playerColor == Piece.WHITE ? "White" : "Black")}");

            DetermineInitialPhase();
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
            ClearPrediction();
            DetermineInitialPhase();
            QueueRedraw();
        }

        public void EnableAI(bool enabled)
        {
            aiEnabled = enabled;
            DetermineInitialPhase();
            GD.Print($"AI {(enabled ? "enabled" : "disabled")}");
        }

        public void SetAIThinkingTime(float seconds)
        {
            aiMoveTimer.WaitTime = seconds;
        }

        public bool IsInPredictionPhase()
        {
            return isInPredictionPhase;
        }
    }
}
