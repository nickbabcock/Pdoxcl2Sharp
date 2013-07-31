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

    public void Write(ParadoxSaver writer)
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

public static int Main()
{
    TheoreticalFile theoryFile;

    using (FileStream fs = new FileStream("theories.txt"))
    {
        theoryFile = ParadoxParser.Parse(fs, new TheoryFile());
    }

    //Save the information into RAM
    using (FileStream fs = new FileStream("theories.new.txt"))
    using (ParadoxSaver saver = new ParadoxSaver(ms))
    {
        theoryFile.Write(saver);
    }
}
```

## Advanced Examples

For a more advanced saving example see FileSave.cs and FileText.txt. 

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
