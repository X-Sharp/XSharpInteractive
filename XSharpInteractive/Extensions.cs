using LanguageService.CodeAnalysis;
using LanguageService.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSharpInteractive
{
    internal static class Extensions
    {
        internal static bool HasAnyErrors<T>(this ImmutableArray<T> diagnostics) where T : Diagnostic
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        internal static ScriptOptions RemoveImportsAndReferences(this ScriptOptions options)
        {
            return options.WithReferences(Array.Empty<MetadataReference>()).WithImports(Array.Empty<string>());
        }
    }
}
