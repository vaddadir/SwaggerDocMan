using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Transform
{
    public class ExampleBodyGenerator : RewriterBase
    {
        private readonly List<PropertyInfo> _properties;
        private readonly string _modelName;
        private readonly Random _random;

        public ExampleBodyGenerator(ILogger logger, string modelName, List<PropertyInfo> properties) : base(logger)
        {
            _properties = properties;
            _modelName = modelName.Split(".", StringSplitOptions.RemoveEmptyEntries)[^1];
            _random = new Random(1);
        }

        public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            var existingProps = node.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.ValueText).ToList();
            existingProps.Add("ExtensionData");
            foreach (var item in _properties)
            {
                var propName = Clean(item.Name);
                if (existingProps.Contains(propName))
                {
                    continue;
                }
                node = GetNode(node, item.PropertyType, propName);
            }

            return node;
        }

        private InitializerExpressionSyntax GetNode(InitializerExpressionSyntax node, Type itemPropType, string propName)
        {
            if (itemPropType.FullName == typeof(string).FullName)
            {
                MemberAccessExpressionSyntax memberAccessExpression;
                if (propName.EndsWith("ID"))
                {
                    memberAccessExpression = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("System.Guid"), IdentifierName("NewGuid().ToString()"));
                }
                else
                {
                    memberAccessExpression = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Descriptions"), IdentifierName($"{_modelName}{propName}"));
                }
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), memberAccessExpression));
            }
            else if (itemPropType.FullName == typeof(bool).FullName)
            {
                var literalExpression = sf.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), literalExpression));
            }
            else if ((itemPropType.FullName == typeof(int).FullName) || (itemPropType.FullName == typeof(int?).FullName))
            {
                var literalExpression = sf.LiteralExpression(SyntaxKind.FalseLiteralExpression, sf.Literal(_random.Next(int.MaxValue)));
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), literalExpression));
            }
            else if ((itemPropType.FullName == typeof(DateTime).FullName) || (itemPropType.FullName == typeof(DateTime?).FullName))
            {
                var memberAccessExpression = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("System.DateTime"), IdentifierName("Now"));
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), memberAccessExpression));
            }
            else if ((itemPropType.FullName == typeof(Guid).FullName) || (itemPropType.FullName == typeof(Guid?).FullName))
            {
                var memberAccessExpression = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("System.Guid"), IdentifierName("NewGuid()"));
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), memberAccessExpression));
            }
            else if ((itemPropType.FullName == typeof(decimal).FullName) || (itemPropType.FullName == typeof(decimal?).FullName))
            {
                var memberAccessExpression = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("decimal"), IdentifierName("Zero"));
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), memberAccessExpression));
            }
            else if (itemPropType.IsEnum)
            {
                var enumNames = itemPropType.GetEnumNames();
                var randomIndex = Math.Max(new Random().Next(enumNames.Length)-1, 0);
                var randomEnumName = enumNames.GetValue(randomIndex)?.ToString();

                var memberAccessExpression = sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(itemPropType.FullName), IdentifierName(randomEnumName));
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), memberAccessExpression));
            }
            else
            {
                var literalExpression = sf.LiteralExpression(SyntaxKind.NullLiteralExpression);
                node = node.AddExpressions(sf.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propName), literalExpression));
            }
            return node;
        }

        private string Clean(string parameterName)
        {
            var tokens = parameterName.Split("_", StringSplitOptions.RemoveEmptyEntries).Select(t => $"{t.ToUpper()[0]}{t.Substring(1)}");
            parameterName = string.Join(string.Empty, tokens);
            return parameterName;
        }
    }
}