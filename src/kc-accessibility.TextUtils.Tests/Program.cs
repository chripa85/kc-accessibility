using System;

internal static class Program
{
	private static int Main()
	{
		try
		{
			TestCleanText();
			TestSplitIdentifier();
			TestJoinParts();
			Console.WriteLine("All text utility tests passed.");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 1;
		}
	}

	private static void TestCleanText()
	{
		Equal(string.Empty, AccessibilityTextUtilities.CleanText(null), "CleanText handles null.");
		Equal("Alpha Beta Gamma", AccessibilityTextUtilities.CleanText(" Alpha\tBeta\r\nGamma "), "CleanText normalizes whitespace.");
		Equal("A B", AccessibilityTextUtilities.CleanText("A    B"), "CleanText collapses repeated spaces.");
	}

	private static void TestSplitIdentifier()
	{
		Equal("Kingdom Share From Game", AccessibilityTextUtilities.SplitIdentifier("KingdomShareFromGame"), "SplitIdentifier handles PascalCase.");
		Equal("main menu item", AccessibilityTextUtilities.SplitIdentifier("main_menu-item"), "SplitIdentifier handles separators.");
		Equal(string.Empty, AccessibilityTextUtilities.SplitIdentifier(string.Empty), "SplitIdentifier handles empty.");
	}

	private static void TestJoinParts()
	{
		Equal("First. Second. Third.", AccessibilityTextUtilities.JoinParts("First", " Second ", "Third."), "JoinParts joins with punctuation.");
		Equal("Only one.", AccessibilityTextUtilities.JoinParts("", null, "Only one"), "JoinParts skips blank parts.");
		Equal(string.Empty, AccessibilityTextUtilities.JoinParts("", " ", null), "JoinParts handles all blanks.");
	}

	private static void Equal(string expected, string actual, string message)
	{
		if (!string.Equals(expected, actual, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(message + " Expected: '" + expected + "', actual: '" + actual + "'.");
		}
	}
}
