using Godot;

namespace MonoChess.Chess.GUI
{
    public partial class PredictionView : VBoxContainer
    {
        private BoardView boardView;
        private Button confirmPredictionButton;
        private Label phaseLabel;
        private Label instructionLabel;

        public override void _Ready()
        {
            base._Ready();

            // Find the BoardView
            var main = GetTree().Root.GetNode<Control>("Main");
            var mainHBox = main.GetNode<HBoxContainer>("HBoxContainer");
            boardView = mainHBox.GetNode<BoardView>("BoardView");

            // Find UI elements
            confirmPredictionButton = GetNode<Button>("ConfirmPredictionButton");
            phaseLabel = GetNode<Label>("PhaseLabel");
            instructionLabel = GetNode<Label>("InstructionLabel");

            if (confirmPredictionButton != null)
            {
                confirmPredictionButton.Pressed += OnConfirmPredictionPressed;
            }

            if (boardView != null)
            {
                // Connect to board signals
                boardView.PredictionPhaseChanged += OnPredictionPhaseChanged;

                // Initialize UI state
                UpdatePhaseDisplay(boardView.IsInPredictionPhase());
            }
        }

        private void OnConfirmPredictionPressed()
        {
            if (boardView != null)
            {
                boardView.ConfirmPrediction();
            }
        }

        private void OnPredictionPhaseChanged(bool inPredictionPhase)
        {
            UpdatePhaseDisplay(inPredictionPhase);
        }

        private void UpdatePhaseDisplay(bool inPredictionPhase)
        {
            if (confirmPredictionButton != null)
            {
                confirmPredictionButton.Visible = inPredictionPhase;
                confirmPredictionButton.Disabled = false;
            }

            if (phaseLabel != null)
            {
                phaseLabel.Text = inPredictionPhase ? "PREDICTION PHASE" : "GAME PHASE";
                phaseLabel.Modulate = inPredictionPhase ? Colors.Magenta : Colors.White;
            }

            if (instructionLabel != null)
            {
                if (inPredictionPhase)
                {
                    instructionLabel.Text = "Drag an opponent piece to predict their next move, then click Confirm Prediction.";
                }
                else
                {
                    instructionLabel.Text = "Make your move by dragging your pieces.";
                }
            }
        }
    }
}