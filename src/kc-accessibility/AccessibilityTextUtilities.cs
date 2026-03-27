using System.Text;

public static class AccessibilityTextUtilities
{
	public static string JoinParts(params string[] parts)
	{
		StringBuilder builder = new StringBuilder();
		for (int i = 0; i < parts.Length; i++)
		{
			string part = CleanText(parts[i]);
			if (string.IsNullOrEmpty(part))
			{
				continue;
			}
			if (builder.Length > 0)
			{
				builder.Append(". ");
			}
			builder.Append(part.TrimEnd('.'));
		}
		if (builder.Length == 0)
		{
			return string.Empty;
		}
		builder.Append('.');
		return builder.ToString();
	}

	public static string CleanText(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		string cleaned = value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
		while (cleaned.Contains("  "))
		{
			cleaned = cleaned.Replace("  ", " ");
		}
		return cleaned.Trim();
	}

	public static string SplitIdentifier(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		StringBuilder builder = new StringBuilder(value.Length + 8);
		char previous = '\0';
		for (int i = 0; i < value.Length; i++)
		{
			char current = value[i];
			if (i > 0 && char.IsUpper(current) && previous != '\0' && !char.IsUpper(previous))
			{
				builder.Append(' ');
			}
			if (current == '_' || current == '-')
			{
				builder.Append(' ');
			}
			else
			{
				builder.Append(current);
			}
			previous = current;
		}
		return builder.ToString().Trim();
	}
}
