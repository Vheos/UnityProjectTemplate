#pragma warning disable

using System.IO;
using System.Text.RegularExpressions;

public class CsprojPostprocessor : AssetPostprocessor
{
	// Settings
	private const bool ForceCSharp11 = true;
	private const bool ForceNullable = true;
	private const bool ForceNetStandard21 = true;
	private static readonly HashSet<string> Whitelist = new()
	{
		nameof(Vheos),
	};

	// Const
	private const string LangVersion = "LangVersion";
	private const string Nullable = "Nullable";
	private const string TargetFrameworkVersion = "TargetFrameworkVersion";
	private const string TargetFramework = "TargetFramework";
	private const string PropertyGroup = "PropertyGroup";
	private const string CSharp11 = "11.0";
	private const string NetStandard21 = "netstandard2.1";

	// Methods
	private static string ElementWithValue(string element, string value)
		=> ElementOpen(element) + value + ElementClose(element);
	private static string ElementOpen(string element)
		=> $"<{element}>";
	private static string ElementClose(string element)
		=> $"</{element}>";

	// Unity
	private static string OnGeneratedCSProject(string path, string content)
	{
		var fileName = Path.GetFileNameWithoutExtension(path);
		if (!Whitelist.Any(key => fileName.Contains(key)))
			return content;

		string pattern, replacement;
		void Replace(int count = -1)
			=> content = new Regex(pattern).Replace(content, replacement, count);

		if (ForceCSharp11)
		{
			// Recommended additional compiler argument: /langversion:preview
			pattern = ElementWithValue(LangVersion, "(.*?)");
			replacement = ElementWithValue(LangVersion, CSharp11);
			Replace();
		}

		if (ForceNullable)
		{
			// Recommended additional compiler argument: /nullable:enable
			pattern = $"(.*{ElementOpen(PropertyGroup)}.*)";
			replacement = "$1\n" + ElementWithValue(Nullable, "enable");
			Replace(1);
		}

		if (ForceNetStandard21)
		{
			pattern = ElementWithValue(TargetFrameworkVersion, ".*?");
			replacement = ElementWithValue(TargetFramework, NetStandard21);
			Replace();
		}

		return content;
	}
}