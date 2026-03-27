using ReLogic.Graphics;
using ReLogic.Localization.IME;
using ReLogic.OS;
using StructureHelper.Core.Loaders.UILoading;
using StructureHelper.Helpers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace StructureHelper.Content.GUI
{
	public enum InputType
	{
		text,
		integer,
		number
	}

	internal class TextField : SmartUIElement
	{
		public bool typing;
		public bool updated;
		public bool reset;
		public InputType inputType;

		public string currentValue = "";

		// Composition string is handled at the very beginning of the update
		// In order to check if there is a composition string before backspace is typed, we need to check the previous state
		private bool _oldHasCompositionString;

		public TextField(InputType inputType = InputType.text)
		{
			this.inputType = inputType;
			Width.Set(130, 0);
			Height.Set(24, 0);
		}

		public void SetTyping()
		{
			typing = true;
			Main.blockInput = true;
		}

		public void SetNotTyping()
		{
			typing = false;
			Main.blockInput = false;
		}

		public override void SafeClick(UIMouseEvent evt)
		{
			SetTyping();
		}

		public override void SafeRightClick(UIMouseEvent evt)
		{
			SetTyping();
			currentValue = "";
			updated = true;
		}

		public override void SafeUpdate(GameTime gameTime)
		{
			if (reset)
			{
				updated = false;
				reset = false;
			}

			if (updated)
				reset = true;

			if (Main.mouseLeft && !IsMouseHovering)
				SetNotTyping();
		}

		public void HandleText()
		{
			if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
				SetNotTyping();

			PlayerInput.WritingText = true;
			Main.instance.HandleIME();

			string newText = Main.GetInputText(currentValue);

			// GetInputText() handles typing operation, but there is a issue that it doesn't handle backspace correctly when the composition string is not empty. It will delete a character both in the text and the composition string instead of only the one in composition string. We'll fix the issue here to provide a better user experience
			if (_oldHasCompositionString && Main.inputText.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Back))
				newText = currentValue; // force text not to be changed

			if (inputType == InputType.integer)
			{
				if (newText != currentValue && Regex.IsMatch(newText, "^[0-9]*$"))
				{
					currentValue = newText;
					updated = true;
				}
			}
			else if (inputType == InputType.number) //I found this regex on SO so no idea if it works right lol
			{
				if (newText != currentValue && Regex.IsMatch(newText, "(?<=^| )[0-9]+(.[0-9]+)?(?=$| )|(?<=^| ).[0-9]+(?=$| )"))
				{
					currentValue = newText;
					updated = true;
				}
			}
			else
			{
				if (newText != currentValue)
				{
					currentValue = newText;
					updated = true;
				}
			}

			_oldHasCompositionString = Platform.Get<IImeService>().CompositionString is { Length: > 0 };
		}

		//Copy paste from tmod 1.4.4.9
		public static void DrawWindowsIMEPanel(Vector2 position, float xAnchor = 0f)
		{
			if (!Platform.Get<IImeService>().IsCandidateListVisible)
			{
				return;
			}
			List<string> list = new List<string>();
			for (uint num = 0u; num < Platform.Get<IImeService>().CandidateCount; num++)
			{
				string candidate = Platform.Get<IImeService>().GetCandidate(num);
				list.Add(candidate);
			}
			if (list.Count == 0)
			{
				return;
			}
			uint selectedCandidate = (uint)Platform.Get<IImeService>().SelectedCandidate;
			DynamicSpriteFont value = FontAssets.MouseText.Value;
			float num2 = 0.85f;
			float num3 = 14f;
			float num4 = 0f;
			int num5 = 32;
			num4 += num3;
			string text = "{0,2}: {1}";
			string text2 = "  ";
			for (int i = 0; i < list.Count; i++)
			{
				int num6 = i + 1;
				string text3 = text;
				if (i < list.Count - 1)
				{
					text3 += text2;
				}
				num4 += value.MeasureString(string.Format(text3, num6, list[i])).X * num2;
				num4 += num3;
			}
			Vector2 vector = new(num4 * (0f - xAnchor), 0f);
			Utils.DrawSettings2Panel(Main.spriteBatch, position + vector + new Vector2(0f, (float)(-num5)), num4, new Color(63, 65, 151, 255) * 0.785f);
			Vector2 pos = position + new Vector2(10f, (float)(-num5 / 2)) + vector;
			for (uint num7 = 0u; num7 < list.Count; num7++)
			{
				Color color = Color.Gray;
				if (num7 == selectedCandidate)
				{
					color = Color.White;
				}
				uint num8 = num7 + 1;
				string text4 = text;
				if (num7 < list.Count - 1)
				{
					text4 += text2;
				}
				string text5 = string.Format(text4, num8, list[(int)num7]);
				Vector2 vector2 = value.MeasureString(text5) * num2;
				Utils.DrawBorderString(Main.spriteBatch, text5, pos, color, num2, 0f, 0.4f);
				pos.X += vector2.X + num3;
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			GUIHelper.DrawBox(spriteBatch, GetDimensions().ToRectangle(), Color.Black * 0.5f);

			if (typing)
			{
				GUIHelper.DrawOutline(spriteBatch, GetDimensions().ToRectangle(), Color.White);
				HandleText();

				// draw ime panel, note that if there's no composition string then it won't draw anything
				DrawWindowsIMEPanel(GetDimensions().Position());
			}

			Vector2 pos = GetDimensions().Position() + Vector2.One * 4;

			const float scale = 0.75f;
			string displayed = currentValue ?? "";

			Utils.DrawBorderString(spriteBatch, displayed, pos, Color.White, scale);

			// composition string + cursor drawing below
			if (!typing)
				return;

			pos.X += FontAssets.MouseText.Value.MeasureString(displayed).X * scale;
			string compositionString = Platform.Get<IImeService>().CompositionString;

			if (compositionString is { Length: > 0 })
			{
				Utils.DrawBorderString(spriteBatch, compositionString, pos, new Color(255, 240, 20), scale);
				pos.X += FontAssets.MouseText.Value.MeasureString(compositionString).X * scale;
			}

			if (Main.GameUpdateCount % 20 < 10)
				Utils.DrawBorderString(spriteBatch, "|", pos, Color.White, scale);
		}
	}
}