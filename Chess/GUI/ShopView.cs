using Godot;
using System.Collections.Generic;

namespace MonoChess.Chess.GUI
{
	public partial class ShopView : Control
	{
		private Label currencyLabel;
		private ItemList itemList;
		private RichTextLabel descriptionLabel;

		private int chips = 0;

		private class ShopItem
		{
			public string Name;
			public int Cost;
			public string Description;

			public ShopItem(string name, int cost, string description)
			{
				Name = name;
				Cost = cost;
				Description = description;
			}
		}

		private List<ShopItem> shopItems = new List<ShopItem>
		{
			new ShopItem("Find Best Move", 5, "Shows you the best move from your current position."),
		};

		public override void _Ready()
		{
			base._Ready();
			currencyLabel = GetNode<Label>("CurrencyLabel");
			itemList = GetNode<ItemList>("ItemList");
			descriptionLabel = GetNode<RichTextLabel>("DescriptionLabel");

			PopulateShop();
			UpdateCurrencyLabel();

			itemList.ItemSelected += OnItemSelected;
			itemList.ItemActivated += OnItemActivated; // double-click to buy
		}

		private void PopulateShop()
		{
			itemList.Clear();
			for (int i = 0; i < shopItems.Count; i++)
			{
				var item = shopItems[i];
				itemList.AddItem($"{item.Name} - {item.Cost} chips");
				itemList.SetItemTooltip(i, item.Description);

				//if (chips < item.Cost)
					//itemList.SetItemDisabled(i, true);
			}
		}

		private void UpdateCurrencyLabel()
		{
			currencyLabel.Text = $"Chips: {chips}";

			for (int i = 0; i < shopItems.Count; i++)
			{
				var item = shopItems[i];
				//itemList.SetItemDisabled(i, chips < item.Cost);
			}
		}

		public void AddChips(int amount)
		{
			chips += amount;
			UpdateCurrencyLabel();
		}

		private void OnItemSelected(long index)
		{
			if (index >= 0 && index < shopItems.Count)
			{
				var item = shopItems[(int)index];
				descriptionLabel.Text = $"[b]{item.Name}[/b]\nCost: {item.Cost} chips\n\n{item.Description}";
			}
			else
			{
				descriptionLabel.Text = "";
			}
		}

		private void OnItemActivated(long index)
		{
			var item = shopItems[(int)index];
			if (chips >= item.Cost)
			{
				chips -= item.Cost;
				UpdateCurrencyLabel();
				GD.Print($"Purchased {item.Name}");
				// TODO: Emit signal to apply effect in-game
			}
			else
			{
				GD.Print("Not enough chips!");
			}
		}
	}
}
