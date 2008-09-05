using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace jgrep
{
	internal class Program
	{
		private static readonly List<string> excludePattern = new List<string>();
		private static readonly List<string> includePattern = new List<string>();
		private static bool caseSensitive = true;
		private static bool chatty;
		private static bool excludeLine;
		private static bool filenameonly;
		private static string initialRootDirectory = ".";
		private static int maxdepth = -1;
		private static bool quiet;
		private static bool regex;
		private static string searchPattern = "";

		private static void Main(string[] args)
		{
			try
			{
				readConfig();
				parseOpts(args);
				if (string.IsNullOrEmpty(searchPattern))
				{
					Console.Error.WriteLine("Missing search pattern (/p)");
					Console.Error.WriteLine(UsageError());
					return;
				}
				Setup();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
		}

		private static void Setup()
		{
			StringComparison comparison = caseSensitive
			                              	? StringComparison.InvariantCulture
			                              	: StringComparison.InvariantCultureIgnoreCase;
			RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.Singleline |
			                       (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
			Matcher matcher = textline =>
			                  regex
			                  	? Regex.Match(textline, searchPattern, options).Success
			                  	: textline.IndexOf(searchPattern, comparison) > -1;

			if (!Directory.Exists(initialRootDirectory))
			{
				throw new DirectoryNotFoundException(string.Format("'{0}' is not a valid root path.", initialRootDirectory));
			}
			InclusionTester shouldInclude =
				working =>
					{
						bool include = true;
						string curr = working.ToLower();
						//process exclusions first
						excludePattern.ForEach(
							excl => include = include
							                  	? string.IsNullOrEmpty(excl)
							                  	  	? true
							                  	  	: curr.IndexOf(excl) < 0
							                  	: include);
						//inclusions override exclusions
						includePattern.ForEach(
							incl =>
							include =
							string.IsNullOrEmpty(incl) ? true : curr.IndexOf(incl) > -1);
						return include;
					};
			Search(initialRootDirectory, shouldInclude, matcher, 0);
		}

		private static void readConfig()
		{
			string config = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? ".", ".jgreprc");
			if (File.Exists(config))
			{
				parseOpts(
					new List<string>(File.ReadAllLines(config))
						.ConvertAll(
						l =>
							{
								int beginComment = l.IndexOf('#');
								if (beginComment > -1)
								{
									l = l.Remove(beginComment);
								}
								return l.Trim();
							})
						.FindAll(l => !string.IsNullOrEmpty(l))
						.ToArray()
					);
			}
		}

		private static void parseOpts(IEnumerable<string> args)
		{
			try
			{
				foreach (string arg in args)
				{
					if (string.IsNullOrEmpty(arg))
					{
						continue;
					}
					string[] pair = arg.Split(new[] {':'}, 2);
					switch (pair[0].Substring(0, 2).ToLower())
					{
						case "/c":
							chatty = true;
							break;
						case "/d":
							maxdepth = Convert.ToInt32(pair[1]);
							break;
						case "/e":
							regex = true;
							break;
						case "/i":
							caseSensitive = false;
							break;
						case "/l":
							filenameonly = true;
							break;
						case "/n":
							includePattern.Add(pair[1].ToLower());
							break;
						case "/p":
							searchPattern = pair[1];
							break;
						case "/q":
							quiet = true;
							break;
						case "/r":
							initialRootDirectory = pair[1];
							break;
						case "/v":
							excludeLine = true;
							break;
						case "/x":
							excludePattern.Add(pair[1].ToLower());
							break;
						default:
							Console.Error.WriteLine(UsageError());
							Environment.Exit(0);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				throw new ApplicationException(UsageError(), ex);
			}
		}

		private static void Search(string root, InclusionTester shouldInclude, Matcher match, int depth)
		{
			if (-1 != maxdepth && depth > maxdepth)
			{
				return;
			}

			foreach (string file in Directory.GetFiles(root))
			{
				if (shouldInclude(file))
				{
					if (chatty)
					{
						Console.Out.WriteLine("::Searching file {0}.", file);
					}
					try
					{
						using (var reader = new StreamReader(file))
						{
							int lineNumber = 0;
							while (!reader.EndOfStream)
							{
								lineNumber++;
								string line = reader.ReadLine();
								bool matches = match(line);

								if (excludeLine ? !matches : matches)
								{
									string fmtstr = (filenameonly)
									                	? "{0}"
									                	: (quiet)
									                	  	? "{0}:{1}"
									                	  	: (chatty)
									                	  	  	? "{0}:{1}{2}{3}"
									                	  	  	: "{0} at line {1} matches.";
									Console.Out.WriteLine(fmtstr, file, lineNumber, Environment.NewLine, line);
									if (filenameonly)
									{
										break;
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine(ex.Message);
					}
				}
				else
				{
					if (chatty)
					{
						Console.Out.WriteLine(
							"::Skipping {0}.  Does not match inclusion criteria, or matches exclusion criteria.", file);
					}
				}
			}
			foreach (string dir in Directory.GetDirectories(root))
			{
				if (chatty)
				{
					Console.Out.WriteLine("::Entering directory {0}.", dir);
				}
				Search(dir, shouldInclude, match, depth++);
			}
		}

		private static string UsageError()
		{
			return
				@"
Usage:
  jgrep /p:<search pattern> [/i][/e][/r:<root>][/x:<exclude pattern>,...][/n:<include pattern>,...][/d:<max depth>][/q][/v][/l][/c]
  jgrep /h

 Options:

  /p:<search pattern>   Text to search for (see /e).
  /i                    Make pattern case insensitive.
  /e                    Use regular expression to search.
  /r:<root>             Root path to begin search. Defaults to './'
  /x:<exclude pattern>  (can use multiple times)
                         String pattern to exclude files/directories 
                         from the search.
  /n:<include pattern>  (can use multiple times)
                         String pattern to include files/directories
                         from the search. overrides exclude
  /d:<max depth>        Number of directory levels deep to go.
                         -1 for infinite (default),
                         0 for current dir, etc.
  /q                    Quiet.  Minimum output.
  /c                    Chatty output.
  /l                    Like similar option in grep. Only display 
                         filenames that match.
  /v                    Like grep option.  exclude match instead
                         of including.
  /h
  /?                    this help.

";
		}

		private delegate bool InclusionTester(string working);

		private delegate bool Matcher(string textline);
	}
}