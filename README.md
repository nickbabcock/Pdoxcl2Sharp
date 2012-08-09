# Pdoxcl2Sharp

Pdoxcl2Sharp is a general parser for files related to Paradox Interactive.  This project is in its baby stages, any and all contribution would be wonderful.

## Example

	public class SaveFile : IParadoxFile
	{
		DateTime currentDate;
		public SaveFile(string filePath)
		{
			ParadoxParser p = new ParadoxParser(this, filePath);
		}
		public void TokenCallback(ParadoxParser parser, string token)
		{
			if (token == "date")
				currentDate = parser.ReadDateTime();
		}
	}
	
## Motivation

Many of those who play Paradox Interactive's games are also programmers; however, one of the biggest hurdles to developing a tool is the actual parsing of the files.  The goal of this project is take care of all the boiler plate code and provide a "plug and parse" mechanism.

## License

Pdoxcl2Sharp is licensed under MIT, so feel free to do whatever you want, as long as this license follows the code.