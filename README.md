# Pdoxcl2Sharp

Pdoxcl2Sharp is a general parser for files related to Paradox Interactive.
While the parser is aimed towards Paradox Interactive, it is not exclusive,
meaning that any file or configuration written in a similar style can be parsed
without problems.

## Example

Say you want to parse this file:

	# Hey, I'm a comment, put me anywhere and everything 
	# until the end of line won't matter and will be chucked! 

	theoretical = {
		infantry_theory
		militia_theory
		mobile_theory
	}
	

Here's how:

	public class TheoreticalFile : IParadoxRead
	{
		IList<string> theories;
		public void TokenCallback(ParadoxParser parser, string token)
		{
			if (token == "theoretical")
				theories = parser.ReadStringList();
		}
	}

	public static int Main()
	{
		TheoreticalFile theoryFile;

		using (FileStream fs = new FileStream("theories.txt"))
		using (ParadoxParser parse = new ParadoxParser(fs))
		{
			while (parse.EndOfStream)
			{
				if (parse.ReadString() == "theoretical")
				{
					theoryFile = parse.Parse(new TheoreticalFile())
				}
			}
		}

		//Save the information into RAM
		using (MemoryStream ms = new MemoryStream())
		{
			using (ParadoxSaver saver = new ParadoxSaver(ms))
			{
				saver.WriteComment("Hey, I'm a new comment");
				saver.WriteLine("theoretical = {")
				foreach (var theory in theoryFile.theories)
				{
					saver.WriteLine(theory, ValueWrite.LeadingTab)
				}
				saver.WriteLine("}");
			}
		}
	}
	
## Motivation

Many of those who play Paradox Interactive's games are also programmers;
however, one of the biggest hurdles to developing a tool is the actual parsing
of the files.  The goal of this project is take care of all the boiler plate
code and provide a "plug and parse" mechanism.

## License

Pdoxcl2Sharp is licensed under MIT, so feel free to do whatever you want, as
long as this license follows the code.
