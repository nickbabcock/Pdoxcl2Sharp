# Pdoxcl2Sharp

Pdoxcl2Sharp is a general parser for files related to Paradox Interactive.
While the parser is aimed towards Paradox Interactive, it is not exclusive,
meaning that any file or configuration written in a similar style can be parsed
without problems.

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

    public class TheoreticalFile : IParadoxRead, IParadoxWrite
    {
        IList<string> theories = new List<string>();
        public void TokenCallback(ParadoxParser parser, string token)
        {
            theories.Add(token);
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
        using (ParadoxParser parse = new ParadoxParser(fs))
        {
            while (!parse.EndOfStream)
            {
                if (parse.ReadString() == "theoretical")
                {
                    theoryFile = parse.Parse(new TheoreticalFile())
                }
            }
        }

        //Save the information into RAM
        using (FileStream fs = new FileStream("theories.new.txt"))
        using (ParadoxSaver saver = new ParadoxSaver(ms))
        {
            theoryFile.Write(saver);
        }
    }

## Advanced Examples

For a more advanced saving example see FileSave.cs and FileText.txt. 

## TokenCallback and ReadString

These methods are the heart of the parsing.  Whenever the parser comes across
something interesting, it invokes the callback with what it found.  The token
found will never be a comment, an equal sign, brackets, empty, null or a string
that has quotes (the quotes are stripped).  ReadString has the exact same
behavior

If there are child structures that are being parsed (deliminted by squirrely
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
    
## License

Pdoxcl2Sharp is licensed under MIT, so feel free to do whatever you want, as
long as this license follows the code.

[git workflow]: https://sandofsky.com/blog/git-workflow.html