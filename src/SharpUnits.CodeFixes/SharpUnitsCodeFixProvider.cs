using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpUnits.Analyzers
{
    /// <summary>Roslyn IDE code-fix provider for SharpUnits diagnostics (<c>SU0001</c> typos and <c>SU0005</c> affine additions).</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SharpUnitsCodeFixProvider)), Shared]
    public sealed class SharpUnitsCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                SharpUnitsLiteralAnalyzer.UnknownOrInvalidUnit.Id,
                SharpUnitsLiteralAnalyzer.AbsoluteAffineAddition.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == SharpUnitsLiteralAnalyzer.UnknownOrInvalidUnit.Id &&
                    diagnostic.Properties.TryGetValue("SuggestedUnit", out string? suggestion) &&
                    !string.IsNullOrWhiteSpace(suggestion))
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                    var literal = node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>()
                        .FirstOrDefault(l => l.IsKind(SyntaxKind.StringLiteralExpression));
                    if (literal is not null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: $"Replace unit literal with '{suggestion}'",
                                createChangedDocument: ct => ReplaceUnitLiteralAsync(context.Document, literal, suggestion!, ct),
                                equivalenceKey: $"SU0001:{suggestion}"),
                            diagnostic);
                    }
                }
                else if (diagnostic.Id == SharpUnitsLiteralAnalyzer.AbsoluteAffineAddition.Id)
                {
                    var binary = root.FindNode(diagnostic.Location.SourceSpan)
                        .AncestorsAndSelf()
                        .OfType<BinaryExpressionSyntax>()
                        .FirstOrDefault(b => b.IsKind(SyntaxKind.AddExpression));

                    if (binary?.Right is InvocationExpressionSyntax rightInv &&
                        rightInv.Expression is MemberAccessExpressionSyntax member &&
                        member.Name.Identifier.Text == "Of")
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Convert right-hand operand to Quantity.Delta(...)",
                                createChangedDocument: ct => RewriteToDeltaAsync(context.Document, member, ct),
                                equivalenceKey: "SU0005:UseDelta"),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> ReplaceUnitLiteralAsync(
            Document document,
            LiteralExpressionSyntax literal,
            string replacement,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) return document;
            var newLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(replacement)).WithTriviaFrom(literal);
            return document.WithSyntaxRoot(root.ReplaceNode(literal, newLiteral));
        }

        private static async Task<Document> RewriteToDeltaAsync(
            Document document,
            MemberAccessExpressionSyntax member,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) return document;
            var newMember = member.WithName(SyntaxFactory.IdentifierName("Delta").WithTriviaFrom(member.Name));
            return document.WithSyntaxRoot(root.ReplaceNode(member, newMember));
        }
    }
}
