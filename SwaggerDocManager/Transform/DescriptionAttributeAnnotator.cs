using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Transform
{
    public class DescriptionAttributeAnnotator : RewriterBase
    {
        private readonly bool decorateMethodParameters = false;

        public DescriptionAttributeAnnotator(ILogger logger, bool decorateMethodParameters = false) : base(logger)
        {
            this.decorateMethodParameters = decorateMethodParameters;
        }

        public List<string> DescriptionConstants { get; } = new();

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            if (!decorateMethodParameters)
            {
                return node;
            }

            var anscestors = node.Ancestors();

            bool isMethodParameter = node.Parent?.Parent?.IsKind(SyntaxKind.MethodDeclaration) ?? false;
            MethodDeclarationSyntax methodDeclaration = null;
            if (isMethodParameter)
            {
                methodDeclaration = node?.Parent?.Parent as MethodDeclarationSyntax;
            }

            if (methodDeclaration == null || !IsActionMethod(methodDeclaration))
            {
                return node;
            }

            var attributeNodeTexts = node.AttributeLists.SelectMany(a => a.ChildNodes()).Select(a => a.ToFullString());
            if (attributeNodeTexts.Any(nodeText => nodeText.StartsWith("SwaggerParameter") || nodeText.StartsWith("SwaggerRequestBody")))
            {
                Logger.Information($"SwaggerParameter already defined on {node.Identifier.ValueText}");
                return node;
            }

            bool required = attributeNodeTexts.Any(nodeText => nodeText == "Required");
            ClassDeclarationSyntax classDeclarationSyntax = anscestors.FirstOrDefault(a => a.IsKind(SyntaxKind.ClassDeclaration)) as ClassDeclarationSyntax;

            var className = classDeclarationSyntax.Identifier.ValueText;
            var parameterName = node.Identifier.ValueText;

            var descriptionConstant = $"{className}{Clean(parameterName)}";
            var descAttribValueConstName = IdentifierName(descriptionConstant);

            var descMemberAccessSyntax = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                DescAttribValueConstClassName(),
                descAttribValueConstName);

            var attribDescArgSyntax = sf.AttributeArgument(descMemberAccessSyntax);
            var attribArgList = sf.SeparatedList<AttributeArgumentSyntax>();
            attribArgList = attribArgList.Add(attribDescArgSyntax);

            if (required)
            {
                var reqdAssignmentSyntax = sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("Required"), sf.LiteralExpression(SyntaxKind.TrueLiteralExpression));
                var attribReqdArgSyntax = sf.AttributeArgument(descMemberAccessSyntax);
                attribArgList = attribArgList.Add(attribReqdArgSyntax);
            }
            var attribArgListSyntax = sf.AttributeArgumentList(attribArgList);

            var attribSyntax = sf.Attribute(IdentifierName("SwaggerParameter"), attribArgListSyntax);
            var attribListSyntax = sf.AttributeList(sf.SingletonSeparatedList(attribSyntax));

            if (node.ToFullString().Contains(attribSyntax.ToFullString()))
            {
                return node;
            }

            node = node.AddAttributeLists(attribListSyntax)
                       .NormalizeWhitespace();

            AddDescriptionConstant(descriptionConstant);

            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var propertyNameIdentifierToken = node.ChildTokens().FirstOrDefault(c => c.IsKind(SyntaxKind.IdentifierToken)).Text;
            var classNameIdentifierToken = string.Empty;
            if (node.Parent is ClassDeclarationSyntax classDeclarationSyntax)
            {
                classNameIdentifierToken = classDeclarationSyntax.Identifier.ValueText;
            }

            var descriptionConstant = $"{classNameIdentifierToken}{Clean(propertyNameIdentifierToken)}";
            var descAttribValueConstName = IdentifierName(descriptionConstant);

            var memberAccessSyntax = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                DescAttribValueConstClassName(),
                descAttribValueConstName);

            var attribArgSyntax = sf.AttributeArgument(memberAccessSyntax);
            var attribArgListSyntax = sf.AttributeArgumentList(sf.SingletonSeparatedList(attribArgSyntax));

            var attribSyntax = sf.Attribute(DescAttribName(), attribArgListSyntax);
            var attribListSyntax = sf.AttributeList(sf.SingletonSeparatedList(attribSyntax));

            if (node.ToFullString().Contains(attribSyntax.ToFullString()))
            {
                return node;
            }

            AddDescriptionConstant(descriptionConstant);

            return node.WithLeadingTrivia(sf.ElasticCarriageReturnLineFeed)
                       .AddAttributeLists(attribListSyntax)
                       .NormalizeWhitespace();
        }

        [return: NotNullIfNotNull("node")]
        public SyntaxNode AppendUsings(SyntaxNode node, IEnumerable<string> usingsToAdd)
        {
            var compilationUnit = CompilationUnit(node);

            List<UsingDirectiveSyntax> uniqueList = new();

            IEnumerable<string> existingUsings = compilationUnit.Usings.SelectMany(u => u.ChildNodes()).Select(cn => cn.ToFullString());

            foreach (var item in usingsToAdd)
            {
                if (existingUsings.Any(u => u.ToLower() == item.ToLower()))
                {
                    Logger.Information($"'{item}' Already exists");
                    continue;
                }
                uniqueList.Add(UsingDirective(item));
            }

            return compilationUnit.AddUsings(uniqueList.ToArray()).NormalizeWhitespace();
        }

        private IdentifierNameSyntax DescAttribName() => IdentifierName("Description");

        private IdentifierNameSyntax DescAttribValueConstClassName() => IdentifierName("Descriptions");

        private UsingDirectiveSyntax UsingDirective(string input)
        {
            QualifiedNameSyntax qualifiedName;
            string[] nsparts = input.Split('.');
            if (nsparts.Length > 1)
            {
                qualifiedName = sf.QualifiedName(IdentifierName(nsparts[0]), IdentifierName(nsparts[1]));
                for (int i = 2; i < nsparts.Length; i++)
                {
                    qualifiedName = sf.QualifiedName(qualifiedName, IdentifierName(nsparts[i]));
                }
                var usingDirectiveSyntax = sf.UsingDirective(qualifiedName);
                return usingDirectiveSyntax;
            }
            else
            {
                return sf.UsingDirective(IdentifierName(nsparts[0]));
            }
        }

        private bool IsActionMethod(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.AttributeLists
                                          .SelectMany(a => a.ChildNodes())
                                          .Any(a => a.ToFullString().Contains("SwaggerOperation"));
        }

        private void AddDescriptionConstant(string descConst)
        {
            if (!DescriptionConstants.Contains(descConst))
            {
                DescriptionConstants.Add(descConst);
            }
        }

        private string Clean(string parameterName)
        {
            var tokens = parameterName.Split("_", StringSplitOptions.RemoveEmptyEntries).Select(t => $"{t.ToUpper()[0]}{t.Substring(1)}");
            parameterName = string.Join(string.Empty, tokens);
            return parameterName;
        }
    }
}