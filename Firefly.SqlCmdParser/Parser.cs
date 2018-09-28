namespace Firefly.SqlCmdParser
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Firefly.SqlCmdParser.SimpleParser;
    using Firefly.SqlCmdParser.SimpleParser.Commands;

    /// <summary>
    /// The parse engine.
    /// Parses input SQL, executing directives as they are found and batches on each GO.
    ///  </summary>
    /// <remarks>
    /// The parser is extremely basic. It does NOT understand SQL itself.
    /// It only distinguishes the following
    /// - Comments. Token is or is not a comment
    /// - String constants. Tracks single/double quoted strings so that comment characters within a string constant are not interpreted as comments.
    /// </remarks>
    public class Parser
    {
        /// <summary>
        /// The variable regex
        /// </summary>
        internal static readonly Regex VariableRegex = new Regex(@"\$\((?<varname>[^\s\(\)]+)\)");

        /// <summary>
        /// List of matchers for SQLCMD commands
        /// </summary>
        private readonly List<ICommandMatcher> commandMatchers = new List<ICommandMatcher>
                                                                     {
                                                                         new GoCommand(),
                                                                         new SetvarCommand(),
                                                                         new IncludeCommand(),
                                                                         new OnErrorCommand(),
                                                                         new ExitCommand(),
                                                                         new EdCommand(),
                                                                         new ErrorCommand(),
                                                                         new OutCommand(),
                                                                         new PerftraceCommand(),
                                                                         new ShellCommand(),
                                                                         new HelpCommand(),
                                                                         new ListCommand(),
                                                                         new ListVarCommand(),
                                                                         new QuitCommand(),
                                                                         new ResetCommand(),
                                                                         new ServerListCommand(),
                                                                         new ConnectCommand(),
                                                                         new InvalidCommand()
                                                                     };

        /// <summary>
        /// The current directory resolver
        /// </summary>
        private readonly ICurrentDirectoryResolver currentDirectoryResolver;

        /// <summary>
        /// The disable interactive commands
        /// </summary>
        private readonly bool disableInteractiveCommands;

        /// <summary>
        /// The disable variable substitution
        /// </summary>
        private readonly bool disableVariableSubstitution;

        /// <summary>
        /// The invocation number for multi-threaded operation.
        /// </summary>
        private readonly int nodeNumber;

        /// <summary>
        /// The run configuration
        /// </summary>
        private readonly RunConfiguration runConfiguration;

        /// <summary>
        /// The input source stack.
        /// As <c>:R</c> directives are processed, new input source for the included file is pushed onto this stack.
        /// </summary>
        private readonly Stack<IBatchSource> sourceStack = new Stack<IBatchSource>();

        /// <summary>
        /// The current batch
        /// </summary>
        private SqlBatch currentBatch;

        /// <summary>
        /// The previous batch
        /// </summary>
        private SqlBatch previousBatch;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser" /> class.
        /// </summary>
        /// <param name="nodeNumber">The execution number for multi-threaded operation.</param>
        /// <param name="runConfiguration">Run configuration for this parser.</param>
        /// <param name="disableInteractiveCommands">if set to <c>true</c> [disable interactive commands].</param>
        /// <param name="disableVariableSubstitution">If set to <c>true</c> disable variable substitution.</param>
        /// <param name="currentDirectoryResolver">The current directory resolver.</param>
        public Parser(
            int nodeNumber,
            RunConfiguration runConfiguration,
            bool disableInteractiveCommands,
            bool disableVariableSubstitution,
            ICurrentDirectoryResolver currentDirectoryResolver = null)
        {
            this.nodeNumber = nodeNumber;
            this.runConfiguration = runConfiguration;
            this.disableInteractiveCommands = disableInteractiveCommands;
            this.disableVariableSubstitution = disableVariableSubstitution;
            this.currentDirectoryResolver =
                currentDirectoryResolver == null ? new NetRuntimeCurrentDirectoryResolver() : null;
        }

        /// <summary>
        /// Occurs when [input source changed].
        /// </summary>
        public event EventHandler<InputSourceChangedEventArgs> InputSourceChanged;

        /// <summary>
        /// Gets the batch count.
        /// </summary>
        /// <value>
        /// The batch count.
        /// </value>
        public int BatchCount { get; private set; }

        /// <summary>
        /// Gets the command executer.
        /// </summary>
        /// <value>
        /// The command executer.
        /// </value>
        public ICommandExecuter CommandExecuter => this.runConfiguration.CommandExecuter;

        /// <summary>
        /// Gets current batch source. When handling exceptions use this to determine where in the input the exception occurred..
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IBatchSource Source { get; private set; }

        /// <summary>
        /// Gets the variable resolver.
        /// </summary>
        /// <value>
        /// The variable resolver.
        /// </value>
        private IVariableResolver VariableResolver => this.runConfiguration.VariableResolver;

        /// <summary>
        /// Parses this instance.
        /// </summary>
        /// <exception cref="ParserException">Syntax error or unrecognized command directive
        /// or
        /// or</exception>
        public void Parse()
        {
            if (this.runConfiguration.OutputFile != null)
            {
                this.CommandExecuter.Out(OutputDestination.File, this.runConfiguration.OutputFile);
            }

            this.InputSourceChanged += this.CommandExecuter.OnInputSourceChanged;

            this.CommandExecuter.ConnectWithConnectionString(this.runConfiguration.ConnectionString);

            this.SetInputSource(this.runConfiguration.InitialBatchSource);
            var tokenizer = new Tokenizer();

            try
            {
                while (this.sourceStack.Count > 0)
                {
                    this.Source = this.sourceStack.Peek();

                    if (this.currentBatch == null)
                    {
                        this.currentBatch = new SqlBatch();
                    }

                    var line = this.Source.GetNextLine();

                    if (line == null)
                    {
                        // End of input source
                        if (tokenizer.State != TokenizerState.None)
                        {
                            // Incomplete parse
                            throw new ParserException(tokenizer.State, this.Source);
                        }

                        this.sourceStack.Pop();
                        continue;
                    }

                    tokenizer.AddLine(line);
                    Token token;

                    var isStartOfLine = true;

                    while ((token = tokenizer.GetNextToken()) != null)
                    {
                        if (tokenizer.State == TokenizerState.None)
                        {
                            // Not in a comment or a multi-line string literal
                            if (isStartOfLine)
                            {
                                // Command matching only when we are at the beginning of a line
                                var commandMatched = false;

                                foreach (var commandMatcher in this.commandMatchers)
                                {
                                    if (!commandMatcher.IsMatch(token.TokenValue))
                                    {
                                        continue;
                                    }

                                    switch (commandMatcher)
                                    {
                                        case GoCommand go:

                                            if (string.IsNullOrWhiteSpace(this.currentBatch.Sql))
                                            {
                                                break;
                                            }

                                            try
                                            {
                                                this.CommandExecuter.ProcessBatch(this.currentBatch, go.ExecutionCount);
                                            }
                                            finally
                                            {
                                                this.previousBatch = this.currentBatch;
                                                this.currentBatch = new SqlBatch();
                                                ++this.BatchCount;
                                            }

                                            break;

                                        case SetvarCommand setvar:

                                            if (setvar.VarValue == null)
                                            {
                                                this.VariableResolver.DeleteVariable(setvar.VarValue);
                                            }
                                            else
                                            {
                                                this.VariableResolver.SetVariable(setvar.VarName, setvar.VarValue);
                                            }

                                            break;

                                        case IncludeCommand include:

                                            string resolvedPath;

                                            if (Path.IsPathRooted(include.Filename))
                                            {
                                                resolvedPath = include.Filename;
                                            }
                                            else if (!include.Filename.Contains(Path.DirectorySeparatorChar))
                                            {
                                                // Assume the included file is in the same location as the current batch source, or current directory if no source.
                                                resolvedPath = Path.Combine(
                                                    File.Exists(this.Source.Filename)
                                                        ? Path.GetDirectoryName(this.Source.Filename)
                                                        : this.currentDirectoryResolver.GetCurrentDirectory(),
                                                    include.Filename);
                                            }
                                            else
                                            {
                                                resolvedPath = include.Filename;
                                            }

                                            this.SetInputSource(this.CommandExecuter.IncludeFileName(resolvedPath));

                                            break;

                                        case ConnectCommand connect:

                                            connect.ResolveConnectionParameters(this.VariableResolver);

                                            if (connect.Server != null)
                                            {
                                                // Execute whatever we have so far on the current connection before reconnecting
                                                try
                                                {
                                                    this.CommandExecuter.ProcessBatch(this.currentBatch, 1);
                                                }
                                                finally
                                                {
                                                    this.previousBatch = this.currentBatch;
                                                    this.currentBatch = new SqlBatch();
                                                    ++this.BatchCount;
                                                }

                                                this.CommandExecuter.Connect(
                                                    connect.Timeout,
                                                    connect.Server,
                                                    connect.Username,
                                                    connect.Password);
                                            }

                                            break;

                                        case EdCommand _:

                                            if (!this.disableInteractiveCommands && Environment.UserInteractive)
                                            {
                                                var currentBatchStr = this.currentBatch.Sql;
                                                var batchToEdit =
                                                    string.IsNullOrEmpty(currentBatchStr)
                                                        ? this.previousBatch.Sql
                                                        : currentBatchStr;

                                                if (!string.IsNullOrWhiteSpace(batchToEdit))
                                                {
                                                    var editedBatch = this.CommandExecuter.Ed(batchToEdit);

                                                    if (editedBatch != null)
                                                    {
                                                        this.SetInputSource(editedBatch);
                                                        this.currentBatch = new SqlBatch();
                                                    }
                                                }
                                            }

                                            break;

                                        case ExitCommand exit:

                                            if (exit.ExitImmediately)
                                            {
                                                return;
                                            }

                                            this.CommandExecuter.Exit(this.currentBatch, exit.ExitBatch);
                                            return;

                                        case ErrorCommand error:

                                            this.CommandExecuter.Error(error.OutputDestination, FileParameterCommand.GetNodeFilepath(this.nodeNumber, error.Filename));
                                            break;

                                        case OutCommand @out:

                                            this.CommandExecuter.Out(@out.OutputDestination, FileParameterCommand.GetNodeFilepath(this.nodeNumber, @out.Filename));
                                            break;

                                        // ReSharper disable once UnusedVariable
                                        case ServerListCommand serverList:

                                            if (!this.disableInteractiveCommands)
                                            {
                                                this.CommandExecuter.ServerList();
                                            }

                                            break;

                                        // ReSharper disable once UnusedVariable
                                        case HelpCommand help:

                                            if (!this.disableInteractiveCommands)
                                            {
                                                this.CommandExecuter.Help();
                                            }

                                            break;

                                        case ResetCommand _:

                                            if (!this.disableInteractiveCommands)
                                            {
                                                this.CommandExecuter.Reset();
                                                this.currentBatch.Clear();
                                            }

                                            break;

                                        case ListCommand _:

                                            if (!this.disableInteractiveCommands)
                                            {
                                                this.CommandExecuter.List(this.currentBatch.Sql);
                                            }

                                            break;

                                        case ListVarCommand _:

                                            if (!this.disableInteractiveCommands)
                                            {
                                                this.CommandExecuter.ListVar(this.VariableResolver.Variables);
                                            }

                                            break;

                                        case ShellCommand shell:

                                            this.CommandExecuter.ExecuteShellCommand(shell.Command);
                                            break;

                                        case QuitCommand _:

                                            this.CommandExecuter.Quit();

                                            // Stop parsing
                                            return;

                                        case OnErrorCommand onError:

                                            this.CommandExecuter.OnError(onError.ErrorAction);
                                            break;

                                        case InvalidCommand _:

                                            throw new ParserException(
                                                "Syntax error or unrecognized command directive",
                                                this.Source);
                                    }

                                    commandMatched = true;
                                    break;
                                }

                                if (commandMatched)
                                {
                                    // Jump to next line, appending a blank line where we removed the command
                                    break;
                                }
                            }

                            this.AppendToken(token);
                        }
                        else
                        {
                            // In a comment, just append to batch buffer
                            this.AppendToken(token);
                        }

                        isStartOfLine = false;
                    }
                }

                if (tokenizer.State != TokenizerState.None)
                {
                    // Incomplete parse
                    throw ParserException.CreateInvalidTokenizerStateException(tokenizer.State, this.Source);
                }

                // If we have anything left, it's the last batch
                if (!string.IsNullOrWhiteSpace(this.currentBatch.Sql))
                {
                    this.CommandExecuter.ProcessBatch(this.currentBatch, 1);
                    ++this.BatchCount;
                }
            }
            catch (ParserException)
            {
                throw;
            }
            catch (SqlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ParserException(ex.Message, ex, this.Source);
            }
        }

        /// <summary>
        /// Appends the token to the current batch doing variable substitution as appropriate.
        /// </summary>
        /// <param name="token">The token.</param>
        private void AppendToken(Token token)
        {
            this.currentBatch.Append(
                token.TokenType == TokenType.Text ? this.SubstituteVariables(token.TokenValue) : token.TokenValue,
                this.Source);
        }

        /// <summary>
        /// Sets the input source.
        /// </summary>
        /// <param name="newSource">The new source.</param>
        private void SetInputSource(IBatchSource newSource)
        {
            this.sourceStack.Push(newSource);
            this.InputSourceChanged?.Invoke(this, new InputSourceChangedEventArgs(this.nodeNumber, newSource, this.CommandExecuter.StdoutDestination));
        }

        /// <summary>
        /// Substitutes any SQLCMD variables.
        /// </summary>
        /// <param name="str">The current SQL fragment.</param>
        /// <returns>Input string with any SQLCMD variables replaced by their values.</returns>
        private string SubstituteVariables(string str)
        {
            if (this.disableVariableSubstitution)
            {
                return str;
            }

            var mc = VariableRegex.Matches(str);

            return mc.Count > 0
                       ? mc.Cast<Match>().Aggregate(
                           str,
                           (current, m) => current.Replace(
                               m.Value,
                               this.VariableResolver.ResolveVariable(m.Groups["varname"].Value)))
                       : str;
        }
    }
}