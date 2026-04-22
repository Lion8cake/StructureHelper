using System;
using System.Reflection;
using Terraria.Localization;

namespace StructureHelper
{
	internal class LocalizationRewriter : ModSystem
	{
		public override void PostSetupContent()
		{
#if DEBUG
			MethodInfo refreshInfo = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.NonPublic | BindingFlags.Static, new Type[] { typeof(Mod), typeof(string), typeof(GameCulture) });
			refreshInfo.Invoke(null, new object[] { StructureHelper.Instance, null, Language.ActiveCulture });
#endif
		}
	}

	internal static class LocalizationRoundabout
	{
		public static void SetDefault(this LocalizedText text, string value)
		{
#if DEBUG
			FieldInfo valueProp = typeof(LocalizedText).GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);

			LanguageManager.Instance.GetOrRegister(text.Key, () => value);
			valueProp.SetValue(text, value);
#endif
		}
	}
}