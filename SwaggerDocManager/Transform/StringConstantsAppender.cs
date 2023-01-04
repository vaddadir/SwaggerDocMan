using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Transform
{
    public class StringConstantsAppender : RewriterBase
    {
        private readonly Dictionary<string, string> constants;
        private readonly string regionName;

        public StringConstantsAppender(ILogger logger, Dictionary<string, string> constantsToAdd, string regionText = default) : base(logger)
        {
            constants = constantsToAdd ?? new();
            regionName = regionText;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var existingMembers = GetExistingMemberNames(node);
            List<MemberDeclarationSyntax> memberDeclarations = BuildMemberDeclarations(existingMembers);

            if (memberDeclarations.Count == 0)
            {
                return node;
            }

            StringBuilder builder = new();
            int lastIndex = node.ToFullString().LastIndexOf("}");
            if (lastIndex >= 0)
            {
                var openingCodeBlock = node.ToFullString()[..lastIndex];
                var closingCodeBlock = node.ToFullString()[lastIndex..];

                builder.AppendLine(openingCodeBlock);
                builder.AppendLine(RegionTrivia(regionName).ToFullString());
                foreach (var item in memberDeclarations)
                {
                    builder.AppendLine(item.ToFullString());
                }
                builder.AppendLine(EndRegionTrivia().ToFullString());
                builder.AppendLine(closingCodeBlock);
                var nodeString = builder.ToString();
                return sf.ParseMemberDeclaration(nodeString).NormalizeWhitespace();
            }
            else
            {
                memberDeclarations[0] = memberDeclarations[0].WithLeadingTrivia(RegionTrivia(regionName));
                memberDeclarations[memberDeclarations.Count - 1] = memberDeclarations[memberDeclarations.Count - 1].WithTrailingTrivia(EndRegionTrivia());
                return node.WithMembers(sf.List(memberDeclarations))
                           .NormalizeWhitespace();
            }
        }

        private List<MemberDeclarationSyntax> BuildMemberDeclarations(IEnumerable<string> existingMembers)
        {
            int currentIndex = 0;
            List<MemberDeclarationSyntax> memberDeclarations = new();
            foreach (var kvPair in constants)
            {
                if (existingMembers.Any(s => s == kvPair.Key))
                {
                    Logger.Information($"{kvPair.Key} already defined");
                    continue;
                }

                var fieldDeclaration = FieldDeclaration(kvPair.Key, kvPair.Value);

                memberDeclarations.Add(fieldDeclaration);
                currentIndex++;
            }

            return memberDeclarations;
        }

        private VariableDeclaratorSyntax VariableDeclarator(string variableName) => sf.VariableDeclarator(variableName);

        private LiteralExpressionSyntax LiteralExpression(string literalValue) => sf.LiteralExpression(SyntaxKind.StringLiteralExpression, sf.Literal(literalValue));

        private VariableDeclarationSyntax StringVariableDeclaration() => sf.VariableDeclaration(sf.PredefinedType(sf.Token(SyntaxKind.StringKeyword)));

        private FieldDeclarationSyntax FieldDeclaration(string variableName, string variableValue)
        {
            var equalsValueClause = sf.EqualsValueClause(LiteralExpression(variableValue));
            var variableDeclaratorList = sf.SingletonSeparatedList(VariableDeclarator(variableName).WithInitializer(equalsValueClause));
            var variableDeclaration = StringVariableDeclaration().WithVariables(variableDeclaratorList);
            var modifierTokenList = sf.TokenList(sf.Token(SyntaxKind.PublicKeyword), sf.Token(SyntaxKind.ConstKeyword));

            return sf.FieldDeclaration(variableDeclaration)
                     .WithModifiers(modifierTokenList)
                     .NormalizeWhitespace();
        }

        private SyntaxTrivia RegionTrivia(string regionText)
        {
            if (string.IsNullOrWhiteSpace(regionText))
            {
                return sf.Trivia(sf.RegionDirectiveTrivia(true));
            }
            var preprocessingMessage = sf.PreprocessingMessage(regionText);
            var triviaList = sf.TriviaList(preprocessingMessage);
            var token = sf.Token(triviaList, SyntaxKind.EndOfDirectiveToken, new());

            return sf.Trivia(sf.RegionDirectiveTrivia(true).WithEndOfDirectiveToken(token).NormalizeWhitespace());
        }

        private SyntaxTrivia EndRegionTrivia() => sf.Trivia(sf.EndRegionDirectiveTrivia(true).NormalizeWhitespace());

        private IEnumerable<string> GetExistingMemberNames(ClassDeclarationSyntax node)
        {
            var members = node.Members;
            return members.OfType<FieldDeclarationSyntax>()
                                    .SelectMany(c => c.Declaration.ChildNodes())
                                    .OfType<VariableDeclaratorSyntax>()
                                    .SelectMany(c => c.ChildTokens())
                                    .Select(c => c.Text);
        }
    }
}