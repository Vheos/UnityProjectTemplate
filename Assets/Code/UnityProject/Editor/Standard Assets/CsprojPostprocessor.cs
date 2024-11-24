#pragma warning disable

using System.Text.RegularExpressions;
using UnityEditor;

namespace UnityProject.Editor
{
	public class CsprojPostprocessor : AssetPostprocessor
	{
		// Const
		private const string LangVersion = "LangVersion";
		private const string Nullable = "Nullable";
		private const string TargetFrameworkVersion = "TargetFrameworkVersion";
		private const string TargetFramework = "TargetFramework";
		private const string PropertyGroup = "PropertyGroup";
		private const string CSharp11 = "11.0";
		private const string NetStandard21 = "netstandard2.1";

		// Methods
		private static string OnGeneratedCSProject(string _, string content)
		{
			string pattern, replacement;
			void Replace(int count = -1)
				=> content = new Regex(pattern).Replace(content, replacement, count);

#if FORCE_CSHARP11
			// Recommended additional compiler argument: /langversion:preview
			pattern = ElementWithValue(LangVersion, "(.*?)");
			replacement = ElementWithValue(LangVersion, CSharp11);
			Replace();
#endif

#if FORCE_NULLABLE
			// Recommended additional compiler argument: /nullable:enable
			pattern = $"(.*{ElementOpen(PropertyGroup)}.*)";
			replacement = "$1\n" + ElementWithValue(Nullable, "enable");
			Replace(1);
#endif

#if FORCE_NETSTANDARD21
			pattern = ElementWithValue(TargetFrameworkVersion, ".*?");
			replacement = ElementWithValue(TargetFramework, NetStandard21);
			Replace();
#endif

			return content;
		}

		private static string ElementWithValue(string element, string value)
			=> ElementOpen(element) + value + ElementClose(element);
		private static string ElementOpen(string element)
			=> $"<{element}>";
		private static string ElementClose(string element)
			=> $"</{element}>";
	}
}