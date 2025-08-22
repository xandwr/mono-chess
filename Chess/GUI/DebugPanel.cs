using Godot;
using MonoChess.Chess.GUI;
using MonoChess.Chess.Core;

public partial class DebugPanel : VBoxContainer
{
    private BoardView boardView;
    private Button switchSideButton;
    private Label playerColorLabel;

    public override void _Ready()
    {
        base._Ready();

        // Find the BoardView
        var main = GetTree().Root.GetNode<Control>("Main");
        var mainHBox = main.GetNode<HBoxContainer>("HBoxContainer");
        boardView = mainHBox.GetNode<BoardView>("BoardView");

        switchSideButton = GetNode<Button>("SwitchSideButton");
        if (switchSideButton != null)
        {
            switchSideButton.Pressed += OnSwitchSideButtonPressed;
        }

        // Optional: Add a label to show current player color
        playerColorLabel = GetNode<Label>("PlayerColorLabel"); // Add this to your scene if desired
        if (playerColorLabel != null && boardView != null)
        {
            // Connect to the signal to update the label
            boardView.PlayerColorChanged += OnPlayerColorChanged;
            UpdatePlayerColorLabel(boardView.PlayerColor);
        }
    }

    private void OnSwitchSideButtonPressed()
    {
        if (boardView != null)
        {
            boardView.SwitchPlayerColor();
            GD.Print("Player color switched!");
        }
        else
        {
            GD.PrintErr("BoardView not found! Check the node path.");
        }
    }

    private void OnPlayerColorChanged(int newPlayerColor)
    {
        UpdatePlayerColorLabel(newPlayerColor);
    }

    private void UpdatePlayerColorLabel(int playerColor)
    {
        if (playerColorLabel != null)
        {
            string colorName = playerColor == Piece.WHITE ? "White" : "Black";
            playerColorLabel.Text = $"Playing as: {colorName}";
        }
    }
}