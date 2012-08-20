# Pdoxcl2Sharp

Pdoxcl2Sharp is a general parser for files related to Paradox Interactive.  While the parser is aimed towards Paradox Interactive, it is not exclusive, meaning that any file or configuration written in a similar style can be parsed without problems.

## Example

Say you want to parse this file:
	# Hey, I'm a comment, put me anywhere and everything 
	# until the end of line won't matter and will be chucked! 

	theoretical= {
		infantry_theory
		militia_theory
		mobile_theory
	}
	

Here's how:

	public class TheoreticalFile : IParadoxFile
	{
		IList<string> theories;
		public TheoreticalFile(string filePath)
		{
			ParadoxParser.Parse(this, filePath);
		}
		public void TokenCallback(ParadoxParser parser, string token)
		{
			if (token == "theoretical")
				theories = parser.ReadStringList();
		}
	}
	
## Motivation

Many of those who play Paradox Interactive's games are also programmers; however, one of the biggest hurdles to developing a tool is the actual parsing of the files.  The goal of this project is take care of all the boiler plate code and provide a "plug and parse" mechanism.

## License

Pdoxcl2Sharp is licensed under MIT, so feel free to do whatever you want, as long as this license follows the code.