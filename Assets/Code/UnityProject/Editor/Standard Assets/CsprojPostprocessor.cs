namespace UnityProject.Editor
{
	using System.Text.RegularExpressions;

	public class CsprojPostprocessor : AssetPostprocessor
	{
		// Keys
		private const string LangVersionKey = "LangVersion";
		private const string NullableKey = "Nullable";
		private const string TargetFrameworkVersionKey = "TargetFrameworkVersion";
		private const string TargetFrameworkKey = "TargetFramework";

		// Settings
		private const string LangVersion = "11.0";
		private const string TargetFramework = "netstandard2.1";
		private const bool EnableNullableContext = true;

		// Methods
		private static string OnGeneratedCSProject(string _, string content)
		{
			string pattern, replacement;

			// LangVersion, Nullable
			pattern = CreateKeyValue(LangVersionKey, ".*?");
			replacement = CreateKeyValue(LangVersionKey, LangVersion);
			if (EnableNullableContext)
				replacement += '\n' + CreateKeyValue(NullableKey, "enable");

			content = Regex.Replace(content, pattern, replacement);

			// TargetFramework
			pattern = CreateKeyValue(TargetFrameworkVersionKey, ".*?");
			replacement = CreateKeyValue(TargetFrameworkKey, TargetFramework);
			content = Regex.Replace(content, pattern, replacement);

			return content;
		}
		private static string CreateKeyValue(string key, string value)
			=> $"<{key}>{value}</{key}>";
	}
}