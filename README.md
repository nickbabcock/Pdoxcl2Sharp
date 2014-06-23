# Pdoxcl2Sharp

Pdoxcl2Sharp is a general parser for files related to Paradox Interactive.
While the parser is aimed towards Paradox Interactive, it is not exclusive,
meaning that any file or configuration written in a similar
[style](#style-of-file) can be parsed without problems.

## Motivation

Many of those who play Paradox Interactive's games are also programmers;
however, one of the biggest hurdles to developing a tool is the actual parsing
of the files.  Parsing is a nontrivial problem that is hard to get right.  This
project aims to be 100% compatible with the parser Paradox uses.  The end
result is to eliminate of all the boiler plate code and provide a "plug and
parse" mechanism.

## Install

Since the demographics users are convceivably small, I've decided against a
nuget package, so installation is a manual process. If a nuget package is
wanted, please raise an issue.

If you will be doing all the parsing manually, then all you need to do is grab
the .dll from the from the [latest] release, and add the appropriate reference
in your favorite IDE.

[latest]: https://github.com/nickbabcock/Pdoxcl2Sharp/releases/latest

If you want to use the parser generator ParseTemplate.tt, which I highly
recommend, then you'll have to grab that file along with the .dll. Then modify
the text in between the commented "add" section. Add a reference to the
downloaded .dll in the generator.

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

```csharp
public class TheoreticalFile : IParadoxRead, IParadoxWrite
{
    IList<string> theories = new List<string>();
    public void TokenCallback(ParadoxParser parser, string token)
    {
        if (token == "theoretical") 
        {
            theories = parser.ReadStringList();
        }
    }

    public void Write(ParadoxStreamWriter writer)
    {
        writer.WriteComment("Hey, I'm a new comment");
        writer.WriteLine("theoretical = {");
        foreach (var theory in theoryFile.theories)
        {
            writer.WriteLine(theory, ValueWrite.LeadingTabs);
        }
        writer.WriteLine("}");
    }
}

public static int Main()
{
    TheoreticalFile theoryFile;

    using (FileStream fs = new FileStream("theories.txt", FileMode.Open))
    {
        theoryFile = ParadoxParser.Parse(fs, new TheoreticalFile());
    }

    //Save the information into RAM
    using (FileStream fs = new FileStream("theories.new.txt", FileMode.Create))
    using (ParadoxSaver saver = new ParadoxSaver(fs))
    {
        theoryFile.Write(saver);
    }
}
```

## Advanced Examples

These are more advanced examples in the test project, but I'll copy a sample
here for convienence.

Let's say you want to parse the following file (it is slightly unrealistic, but
it serves as a good example)

```
name = "My Prov"
tax = 1.000
add_core=MEE
add_core=YOU
add_core=THM
top_provinces={ "BNG" "ORI" "PEG" }
army={
  name = "My first army"
  leader = { id = 5 }
  unit = {
    name = "First infantry of Awesomeness"
    type = ninjas
    morale = 5.445
    strength = 0.998
  }
  unit = {
    name = "Second infantry of awesomeness"
    type = ninjas
    morale = 6.000
    strength = 1.000
  }
}
```

You could manually code this up in an hour, or you could modify
ParseTemplate.tt and in a few minutes and 35 lines of code you could have the
same thing! Here are the relevant lines added to ParseTemplate.tt:

```csharp
var classes = new[] {
  new {
    Name = "Province",
    Props = new[] {
      new { Type = "string", Name = "Name", Alias = "" },
      new { Type = "double", Name = "Tax", Alias = "" },
      new { Type = "IList<string>", Name = "Cores", Alias = "add_core" },
      new { Type = "[ConsecutiveElements] IList<string>", Name = "TopProvinces", Alias = ""},
      new { Type = "IList<Army>", Name = "Armies", Alias = ""}
    }
  },
  new {
    Name = "Army",
    Props = new[] {
      new { Type = "string", Name = "Name", Alias = "" },
      new { Type = "IList<Unit>", Name = "Units", Alias = "" },
      new { Type = "Leader", Name = "Leader", Alias = "" }
    }
  },
  new {
    Name = "Unit",
    Props = new[] {
      new { Type = "string", Name = "Name", Alias = "" },
      new { Type = "string", Name = "Type", Alias = "" },
      new { Type = "double", Name = "Morale", Alias = "" },
      new { Type = "double", Name = "Strength", Alias = "" }
    }
  },
  new {
    Name = "Leader",
    Props = new[] {
      new { Type = "int", Name = "Id", Alias = "" }
    }
  }
};
```

Here's the generated auto-generated content:

```csharp
  
using System;
using Pdoxcl2Sharp;
using System.Collections.Generic;
public partial class Province : IParadoxRead
{
  public string Name { get; set; }
  public double Tax { get; set; }
  public IList<string> Cores { get; set; }
  public IList<string> TopProvinces { get; set; }
  public IList<Army> Armies { get; set; }

  public Province()
  {
        Cores = new List<string>();
        Armies = new List<Army>();
  }

  public void TokenCallback(ParadoxParser parser, string token)
  {
    switch (token)
    {
    case "name": Name = parser.ReadString(); break;
    case "tax": Tax = parser.ReadDouble(); break;
    case "add_core": Cores.Add(parser.ReadString()); break;
    case "top_provinces": TopProvinces = parser.ReadStringList(); break;
    case "army": Armies.Add(parser.Parse(new Army())); break;
    }
  }
}

public partial class Army : IParadoxRead
{
  public string Name { get; set; }
  public IList<Unit> Units { get; set; }
  public Leader Leader { get; set; }

  public Army()
  {
        Units = new List<Unit>();
  }

  public void TokenCallback(ParadoxParser parser, string token)
  {
    switch (token)
    {
    case "name": Name = parser.ReadString(); break;
    case "unit": Units.Add(parser.Parse(new Unit())); break;
    case "leader": Leader = parser.Parse(new Leader()); break;
    }
  }
}

public partial class Unit : IParadoxRead
{
  public string Name { get; set; }
  public string Type { get; set; }
  public double Morale { get; set; }
  public double Strength { get; set; }

  public Unit()
  {
  }

  public void TokenCallback(ParadoxParser parser, string token)
  {
    switch (token)
    {
    case "name": Name = parser.ReadString(); break;
    case "type": Type = parser.ReadString(); break;
    case "morale": Morale = parser.ReadDouble(); break;
    case "strength": Strength = parser.ReadDouble(); break;
    }
  }
}

public partial class Leader : IParadoxRead
{
  public int Id { get; set; }

  public Leader()
  {
  }

  public void TokenCallback(ParadoxParser parser, string token)
  {
    switch (token)
    {
    case "id": Id = parser.ReadInt32(); break;
    }
  }
}

```

## FAQ: What does ConsecutiveElements mean?

If you do all the parsing by hand then there is no need to worry about this;
however, if you are using the generator then there are a couple of ambiugous
situations. Let's say you have a list of strings representing factories:

    factories={Here There Everywhere}

This is also valid

    factory = Here
    factory = There
    factory = Everywhere

It would prohibitively expensive to support both variations with a single list,
hence the attribute must be appended lists that are in the format of the first
factory example.

## TokenCallback and ReadString

These methods are the heart of the parsing.  Whenever the parser comes across
something interesting, it invokes the callback with what it found.  The token
found will never be a comment, an equal sign, brackets, empty, null or a string
that has quotes (the quotes are stripped).  ReadString has the exact same
behavior

If there are child structures that are being parsed (delimited by squirrely
brackets `{}`) and the parent and the children share the same tokens such as
"name" and "id", these can be differentiated by querying `CurrentIndent`.  The
children will be at a higher indent than the parents.  Perhaps a better
solution to this approach is to define a separate class for the children or an
`Action<ParadoxParser, string>` and invoke `Parse` on the parser.

## Contributing

So you want to help?  Great!  Here are a series of steps to get you on your way!
Let me first say that if you have any troubles, file an issue.

- Get a github account
- Fork the repo
- Add a failing test.  The purpose of this is to show that what you are adding couldn't have been done before, or was wrong.
- Add your changes
- Commit your changes in such a way there is only a single commit difference between the main branch here and yours.  If you need help, check out [git workflow][]
- Push changes to your repo
- Submit a pull request and I'll review it!
    
## Style of File 

This section describes in more detail the style of files that can be parsed.

The most important characters to the parser are `=`, `"`, `}`, `{`, `#`, `,`, and
whitespace.  Let's call this set lexemes.  The complement of lexemes is the
untyped.  Lexemes delimit tokens, which are composed of untyped characters.
The parsing process goes as such:

- Read a character and test to see if it is a lexeme.
    - If a command peeked at the next lexeme, return that
    - else advance through whitespace until a lexeme
        - If that lexeme is a comment, advance until newline and go back to step 1
        - else if `{` increase indent by one
        - else if `}` decrease indent by one
        - return encountered lexeme
- If lexeme is a quote, advance until closing quote
    - Everything enclosed in the quotes excluding the quotes is a token
    - Lexemes are considered untyped wrapped in quotes
- else if any other lexeme go back to step 1
- else if the character read was untyped
    - All characters until whitespace or a lexeme is considered a token
- Return token

This is probably, at best, hard to follow.  Hopefully, a couple of examples
will clear some of the confusion.

    player = "AAA"
      player= =  "AAA"

Both are parsed (ie, the tokens returned to the client) the same way `player`
and then `AAA`.  An explanation of this is that all whitespace is skipped until
something interesting is encountered.  Then `player` is grabbed.  From there,
the next token starts at the quotes.  Remember that tokens are composed
strictly of untyped characters, hence the second equals is completely ignored.

This leads to an interesting example showcasing the parser's flexibility.

    ids = { 1 2 3 4 }
    ids={1 2 3 4 }
    ids = { 
        1, 2, 
        3, 4,
    }

All three snippets will return `ids`, `1`, `2`, `3`, and `4` as tokens.  The
extra comma at the end of the '4' is even on purpose.

## License

Pdoxcl2Sharp is licensed under MIT, so feel free to do whatever you want, as
long as this license follows the code.

[git workflow]: https://sandofsky.com/blog/git-workflow.html
