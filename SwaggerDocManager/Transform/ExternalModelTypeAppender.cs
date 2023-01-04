using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Transform
{
    public class ExternalModelTypeAppender : RewriterBase
    {
        private readonly List<string> valuesToAppend;
        private string variableName = string.Empty;

        public ExternalModelTypeAppender(ILogger logger, string variableName, List<string> valuesToAppend) : base(logger)
        {
            this.valuesToAppend = valuesToAppend;
            this.variableName = variableName.ToLower();
        }

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var token = node.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.IdentifierName));
            if (node.Variables.SelectMany(v => v.ChildTokens()).Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText.ToLower() == variableName))
            {
                var existingExternalTypes = node.DescendantNodes().OfType<QualifiedNameSyntax>().Select(c => c.ToFullString()).ToList();
                foreach (var item in valuesToAppend)
                {
                    if (existingExternalTypes.Contains(item))
                    {
                        continue;
                    }
                    var typeofExpressionString = Formatted(item);
                    var expression = sf.ParseExpression(typeofExpressionString).NormalizeWhitespace();
                    var lastNode = node.DescendantNodes().OfType<TypeOfExpressionSyntax>().LastOrDefault();
                    node = node.InsertNodesAfter(lastNode, sf.SingletonList(expression));//.NormalizeWhitespace();
                }
            }
            return node;
        }

        private string Formatted(string raw)
        {
            var formatted = raw.Trim();
            if (formatted.StartsWith("typeof"))
            {
                return formatted;
            }
            formatted = formatted.Trim("()".ToCharArray());
            formatted = $"typeof({formatted})";
            return formatted;
        }
    }
}