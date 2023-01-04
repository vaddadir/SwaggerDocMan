using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Transform
{
    public class ExampleGenerator
    {
        public SourceText GetExampleProviderCodeBlock(string modelName, string className, string immediateParentFolder, string ns)
        {
            return sf.CompilationUnit()
            .WithUsings(sf.List(new UsingDirectiveSyntax[]
                            {
                                sf.UsingDirective(sf.IdentifierName(ns))
                                .WithUsingKeyword(sf.Token(sf.TriviaList(), SyntaxKind.UsingKeyword, sf.TriviaList(sf.Space)))
                                .WithSemicolonToken(sf.Token(sf.TriviaList(), SyntaxKind.SemicolonToken, sf.TriviaList(sf.LineFeed))),
                                sf.UsingDirective(sf.QualifiedName(sf.QualifiedName(sf.IdentifierName("Swashbuckle"), sf.IdentifierName("AspNetCore"))
                                .WithDotToken(sf.Token(SyntaxKind.DotToken)),sf.IdentifierName("Filters"))
                                .WithDotToken(sf.Token(SyntaxKind.DotToken)))
                                .WithUsingKeyword(sf.Token(sf.TriviaList(), SyntaxKind.UsingKeyword, sf.TriviaList(sf.Space)))
                                .WithSemicolonToken ( sf.Token ( sf.TriviaList(), SyntaxKind.SemicolonToken, sf.TriviaList ( sf.LineFeed)  ) ),
                                sf.UsingDirective(sf.IdentifierName("AvocadoNexusApi.Core.Constants"))
                                .WithUsingKeyword(sf.Token(sf.TriviaList(), SyntaxKind.UsingKeyword, sf.TriviaList(sf.Space)))
                                .WithSemicolonToken(sf.Token(sf.TriviaList(), SyntaxKind.SemicolonToken, sf.TriviaList(sf.LineFeed)))
                            }))
            .WithMembers(sf.SingletonList<MemberDeclarationSyntax>(sf.NamespaceDeclaration(sf.QualifiedName(sf.QualifiedName(sf.IdentifierName("AvocadoNexusApi"), sf.IdentifierName("Swagger"))
            .WithDotToken(sf.Token(SyntaxKind.DotToken)), sf.IdentifierName(sf.Identifier(sf.TriviaList(), immediateParentFolder, sf.TriviaList(sf.LineFeed))))
            .WithDotToken(sf.Token(SyntaxKind.DotToken))).WithNamespaceKeyword(sf.Token(sf.TriviaList(), SyntaxKind.NamespaceKeyword, sf.TriviaList(sf.Space)))
            .WithOpenBraceToken(sf.Token(sf.TriviaList(), SyntaxKind.OpenBraceToken, sf.TriviaList(sf.LineFeed)))
            .WithMembers(sf.SingletonList<MemberDeclarationSyntax>(sf.ClassDeclaration(sf.Identifier(sf.TriviaList(), className, sf.TriviaList(sf.Space)))
            .WithModifiers(sf.TokenList(sf.Token(sf.TriviaList(), SyntaxKind.PublicKeyword, sf.TriviaList(sf.Space))))
            .WithKeyword(sf.Token(sf.TriviaList(), SyntaxKind.ClassKeyword, sf.TriviaList(sf.Space)))
            .WithBaseList(sf.BaseList(sf.SingletonSeparatedList<BaseTypeSyntax>(sf.SimpleBaseType(sf.GenericName(sf.Identifier("IExamplesProvider"))
            .WithTypeArgumentList(sf.TypeArgumentList(sf.SingletonSeparatedList<TypeSyntax>(sf.IdentifierName(modelName)))
            .WithLessThanToken(sf.Token(SyntaxKind.LessThanToken))
            .WithGreaterThanToken(sf.Token(sf.TriviaList(), SyntaxKind.GreaterThanToken, sf.TriviaList(sf.LineFeed)))))))
            .WithColonToken(sf.Token(sf.TriviaList(), SyntaxKind.ColonToken, sf.TriviaList(sf.Space))))
            .WithOpenBraceToken(sf.Token(sf.TriviaList(), SyntaxKind.OpenBraceToken, sf.TriviaList(sf.LineFeed)))
            .WithMembers(sf.SingletonList<MemberDeclarationSyntax>(sf.MethodDeclaration(sf.IdentifierName(sf.Identifier(sf.TriviaList(), modelName, sf.TriviaList(sf.Space))), sf.Identifier("GetExamples")).WithModifiers(sf.TokenList(sf.Token(sf.TriviaList(sf.Whitespace("    ")), SyntaxKind.PublicKeyword, sf.TriviaList(sf.Space))))
            .WithParameterList(sf.ParameterList().WithOpenParenToken(sf.Token(SyntaxKind.OpenParenToken))
            .WithCloseParenToken(sf.Token(sf.TriviaList(), SyntaxKind.CloseParenToken, sf.TriviaList(sf.LineFeed))))
            .WithBody(sf.Block(sf.SingletonList<StatementSyntax>(sf.ReturnStatement(sf.ObjectCreationExpression(sf.IdentifierName(modelName))
            .WithNewKeyword(sf.Token(sf.TriviaList(), SyntaxKind.NewKeyword, sf.TriviaList(sf.Space)))
            .WithArgumentList(sf.ArgumentList().WithOpenParenToken(sf.Token(SyntaxKind.OpenParenToken))
            .WithCloseParenToken(sf.Token(sf.TriviaList(), SyntaxKind.CloseParenToken, sf.TriviaList(sf.LineFeed))))
            .WithInitializer(sf.InitializerExpression(SyntaxKind.ObjectInitializerExpression)
            .WithOpenBraceToken(sf.Token(sf.TriviaList(sf.Whitespace("        ")), SyntaxKind.OpenBraceToken, sf.TriviaList(sf.LineFeed)))
            .WithCloseBraceToken(sf.Token(sf.TriviaList(new[] { sf.Whitespace(""), sf.LineFeed, sf.Whitespace("        ") }), SyntaxKind.CloseBraceToken, sf.TriviaList()))))
            .WithReturnKeyword(sf.Token(sf.TriviaList(sf.Whitespace("        ")), SyntaxKind.ReturnKeyword, sf.TriviaList(sf.Space)))
            .WithSemicolonToken(sf.Token(sf.TriviaList(), SyntaxKind.SemicolonToken, sf.TriviaList(sf.LineFeed)))))
            .WithOpenBraceToken(sf.Token(sf.TriviaList(sf.Whitespace("    ")), SyntaxKind.OpenBraceToken, sf.TriviaList(sf.LineFeed)))
            .WithCloseBraceToken(sf.Token(sf.TriviaList(sf.Whitespace("    ")), SyntaxKind.CloseBraceToken, sf.TriviaList(sf.LineFeed))))))
            .WithCloseBraceToken(sf.Token(sf.TriviaList(), SyntaxKind.CloseBraceToken, sf.TriviaList(sf.LineFeed)))))
            .WithCloseBraceToken(sf.Token(sf.TriviaList(new[] { sf.Whitespace("    "), sf.LineFeed }),
            SyntaxKind.CloseBraceToken,
            sf.TriviaList(sf.LineFeed)))))
            .WithEndOfFileToken(sf.Token(SyntaxKind.EndOfFileToken))
            .GetText();
        }
    }
}