

using System;
using System.CommandLine;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace XSharpInteractive;

public static class XSharpKernelVSExtension
{
    public static void Load(Kernel kernel)
    {
        PocketView view = div(
                    "Loading XSharp Interactive... "
                );

        KernelInvocationContext.Current?.Display(view);
        //
        ((CompositeKernel)Kernel.Root).Add(new XSharpKernel());
        //
        // Finally, display some information to the user so they can see how to use the extension.
        view = div(
            code(nameof(XSharpKernelVSExtension)),
            " has been loaded successfully. It support XSharp Core Dialect language scripting. ",
            "Try it by running: ",
            code("#!xsharp"),
            "Or by selecting ",
            code("xsharp"),
            " as language for the Notebook cell."
        );

        KernelInvocationContext.Current?.Display(view);
    }
}