using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using System.Linq;

namespace Transform
{
    public abstract class RewriterBase : CSharpSyntaxRewriter
    {
        protected ILogger Logger { get; }

        protected RewriterBase(ILogger logger)
        {
            Logger = logger;
        }

        protected static IdentifierNameSyntax IdentifierName(string name) => SyntaxFactory.IdentifierName(name);

        protected static CompilationUnitSyntax CompilationUnit(SyntaxNode node)
        {
            return node.AncestorsAndSelf().FirstOrDefault(a => a.IsKind(SyntaxKind.CompilationUnit)) as CompilationUnitSyntax;
        }
    }
}