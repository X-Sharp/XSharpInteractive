using LanguageService.CodeAnalysis.Scripting;
using LanguageService.CodeAnalysis.Scripting.Hosting;
using LanguageService.CodeAnalysis.XSharp;
using LanguageService.CodeAnalysis.XSharp.Scripting;
using LanguageService.CodeAnalysis.XSharp.Scripting.Hosting;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace XSharpInteractive
{
    public class XSharpKernel : Kernel, IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "xsharp";
        private ScriptOptions _scriptOptions;
        private ScriptState<object> _scriptState = null;
        private XSharpObjectFormatter _objectFormatter = null;
        private InteractiveScriptGlobals _scriptGlobals;

        public XSharpKernel() : base(DefaultKernelName)
        {
            Load();
        }

        public XSharpKernel(string name) : base(name)
        {
            Load();
        }

        private void Load()
        {
            KernelInfo.LanguageName = "X#";
            KernelInfo.LanguageVersion = "2.20";
            KernelInfo.DisplayName = $"{KernelInfo.LocalName} - X# Script";
            KernelInfo.Description = "This Kernel can compile and execute X# code and display the results." +
                                 "The language is X# Core Scripting, a dialect of X# that is used for interactive programming.";
            //
            if (_objectFormatter == null)
            {
                _objectFormatter = XSharpObjectFormatter.Instance;
            }
            StringWriter sw = new StringWriter();
            _scriptGlobals = new InteractiveScriptGlobals(sw, _objectFormatter);

            _scriptOptions = ScriptOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest)
            .AddImports(
                "System",
                "System.Text",
                "System.Collections",
                "System.Collections.Generic",
                "System.Threading.Tasks",
                "System.Linq")
            .AddReferences(
                typeof(Enumerable).Assembly,
                typeof(IEnumerable<>).Assembly,
                typeof(Task<>).Assembly,
                typeof(Microsoft.DotNet.Interactive.Kernel).Assembly,
                typeof(CSharpKernel).Assembly,
                typeof(PocketView).Assembly);
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            var cancellationToken = new CancellationToken();

            Script<object> newScript = null;
            if (_scriptState == null)
            {
                newScript = XSharpScript.Create(command.Code, null, _scriptGlobals.GetType());
            }
            else
            {
                newScript = _scriptState.Script.ContinueWith(command.Code);
            }
            BuildAndRun(newScript, _scriptGlobals, ref _scriptState, ref _scriptOptions, displayResult: true, cancellationToken: cancellationToken);
        }

        private void BuildAndRun(Script<object> newScript, InteractiveScriptGlobals globals, ref ScriptState<object> state, ref ScriptOptions options, bool displayResult, CancellationToken cancellationToken)
        {
            var diagnostics = newScript.Compile(cancellationToken);
            DisplayDiagnostics(diagnostics);
            if (diagnostics.HasAnyErrors())
            {
                return;
            }

            var task = (state == null) ?
                newScript.RunAsync(globals, catchException: e => true, cancellationToken: cancellationToken) :
                newScript.RunFromAsync(state, catchException: e => true, cancellationToken: cancellationToken);

            state = task.GetAwaiter().GetResult();
            if (state.Exception != null)
            {
                DisplayException(state.Exception);
            }
            else if (displayResult)// && newScript.HasReturnValue())
            {
                globals.Print(state.ReturnValue);
            }

            //options = UpdateOptions(options, globals);
        }

        private void DisplayException(Exception e)
        {
            PocketView view;
            try
            {
                if (e is FileLoadException && e.InnerException != null)
                {
                    view = div(
                        span[style:"color: red"](e.InnerException.Message),
                        HTML("<br />")
                        );
                }
                else
                {
                    view = div(
                        span[style: "color: red"](_objectFormatter.FormatException(e)),
                    HTML("<br />")
                    );
                }
                KernelInvocationContext.Current?.Display(view);
            }
            finally
            {
            }
        }

        private void DisplayDiagnostics(ImmutableArray<LanguageService.CodeAnalysis.Diagnostic> diagnostics)
        {
            const int MaxDisplayCount = 5;
            List<string> errors = new List<string>();
            try
            {
                PocketView view;
                String r = "";
                foreach (var diagnostic in diagnostics.Take(MaxDisplayCount))
                {
                    errors.Add(diagnostic.ToString());
                }

                if (diagnostics.Length > MaxDisplayCount)
                {
                    int notShown = diagnostics.Length - MaxDisplayCount;
                    r += string.Format("+ additional {0} errors", notShown);
                }
                view = div(
                    span(
                        errors.Select( x =>
                        li[style:"color: red"](x) ),
                    p[style: "color: red"](r),
                    HTML("<br />")
                    )
                    );
                KernelInvocationContext.Current?.Display(view);
            }
            finally
            {
            }
        }


    }



}
