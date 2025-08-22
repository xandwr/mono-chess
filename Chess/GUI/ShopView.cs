using Godot;
using System.Collections.Generic;

namespace MonoChess.Chess.GUI
{
    public partial class ShopView : Control
    {
        [Signal] public delegate void ItemPurchasedEventHandler(string itemName);

        private Label currencyLabel;
        private ItemList itemList;
        private RichTextLabel descriptionLabel;
        private BoardView boardView;

        private class ShopItem(string name, int cost, string description, System.Action onPurchase = null)
        {
            public string Name = name;
            public int Cost = cost;
            public string Description = description;
            public System.Action OnPurchase = onPurchase;
        }

        private List<ShopItem> shopItems = [];

        public override void _Ready()
        {
            base._Ready();

            currencyLabel = GetNode<Label>("CurrencyLabel");
            itemList = GetNode<ItemList>("ItemList");
            descriptionLabel = GetNode<RichTextLabel>("DescriptionLabel");

            // Find the BoardView
            var main = GetTree().Root.GetNode<Control>("Main");
            var mainHBox = main.GetNode<HBoxContainer>("HBoxContainer");
            boardView = mainHBox.GetNode<BoardView>("BoardView");

            InitializeShopItems();
            PopulateShop();
            UpdateCurrencyLabel();

            itemList.ItemSelected += OnItemSelected;
            itemList.ItemActivated += OnItemActivated; // double-click to buy

            // Connect to board events
            if (boardView != null)
            {
                boardView.BonusChipsEarned += OnBonusChipsChanged;
            }
        }

        private void InitializeShopItems()
        {
            shopItems = [
                new("Reveal Best Move", 3,
                    "Shows you the objectively best move from your current position. Can only be used during your turn.",
                    () => RevealBestMove()),

                new("Show All Legal Moves", 2,
                    "Highlights all legal moves for all your pieces. Lasts for your entire turn.",
                    () => ShowAllLegalMoves()),

                new("Undo Last Move", 4,
                    "Undoes your last move and the opponent's response, letting you try a different approach.",
                    () => UndoLastMove()),
            ];
        }

        private void PopulateShop()
        {
            itemList.Clear();
            for (int i = 0; i < shopItems.Count; i++)
            {
                var item = shopItems[i];
                string itemText = $"{item.Name} - {item.Cost} BC";

                itemList.AddItem(itemText);
                itemList.SetItemTooltip(i, item.Description);

                // Disable items that cost more than current chips
                if (GetCurrentChips() < item.Cost)
                {
                    itemList.SetItemDisabled(i, true);
                }
            }
        }

        private void UpdateCurrencyLabel()
        {
            int chips = GetCurrentChips();
            currencyLabel.Text = $"Bonus Chips: {chips}";

            // Update item availability
            for (int i = 0; i < shopItems.Count; i++)
            {
                var item = shopItems[i];
                itemList.SetItemDisabled(i, chips < item.Cost);
            }
        }

        private int GetCurrentChips()
        {
            return boardView?.GetBonusChips() ?? 0;
        }

        private void OnBonusChipsChanged(int amount)
        {
            UpdateCurrencyLabel();
        }

        private void OnItemSelected(long index)
        {
            if (index >= 0 && index < shopItems.Count)
            {
                var item = shopItems[(int)index];
                descriptionLabel.Text = $"[b]{item.Name}[/b]\nCost: {item.Cost} Bonus Chips\n\n{item.Description}";
            }
            else
            {
                descriptionLabel.Text = "";
            }
        }

        private void OnItemActivated(long index)
        {
            if (index < 0 || index >= shopItems.Count) return;

            var item = shopItems[(int)index];
            int currentChips = GetCurrentChips();

            if (currentChips >= item.Cost)
            {
                // Spend the chips
                boardView?.SpendBonusChips(item.Cost);

                // Execute the item effect
                item.OnPurchase?.Invoke();

                // Update UI
                UpdateCurrencyLabel();
                PopulateShop();

                GD.Print($"Purchased {item.Name} for {item.Cost} BC");
                EmitSignal(SignalName.ItemPurchased, item.Name);
            }
            else
            {
                GD.Print("Not enough bonus chips!");
            }
        }

        // Shop item effects
        private static void RevealBestMove()
        {
            // This would integrate with a chess engine to show the best move
            // For now, just show a placeholder
            GD.Print("Best move analysis activated! (Feature needs chess engine integration)");

            // could implement this by:
            // 1. Running a deeper search with your AI
            // 2. Highlighting the recommended move
            // 3. Showing evaluation numbers
        }

        private static void ShowAllLegalMoves()
        {
            GD.Print("All legal moves highlighted! (Feature needs BoardView integration)");

            // could be implemented by:
            // 1. Adding a "show all moves" flag to BoardView
            // 2. Generating moves for all pieces of current player
            // 3. Drawing highlights for all valid destinations
        }

        private static void UndoLastMove()
        {
            // This would need integration with BoardView's move history
            GD.Print("Undo move activated! (Feature needs move history integration)");

            // Implementation would involve:
            // 1. Storing more move history in Board class
            // 2. Adding public undo methods to BoardView
            // 3. Handling prediction state when undoing
        }

        private void ExtendThinkingTime()
        {
            GD.Print("Extended thinking time granted!");

            // This could be implemented by:
            // 1. Adding a delay before the next AI move
            // 2. Showing a countdown timer
            // 3. Giving player time to think during AI turn

            boardView?.SetAIThinkingTime(5.0f);
        }

        private static void ShowPredictionHint()
        {
            GD.Print("Prediction hint activated! (Feature needs AI evaluation integration)");

            // This could show:
            // 1. Heat map of likely move destinations
            // 2. Highlighting pieces that are more likely to move
            // 3. Statistical analysis of position type
        }

        // Public method to refresh shop when chips change
        public void RefreshShop()
        {
            UpdateCurrencyLabel();
            PopulateShop();
        }
    }
}