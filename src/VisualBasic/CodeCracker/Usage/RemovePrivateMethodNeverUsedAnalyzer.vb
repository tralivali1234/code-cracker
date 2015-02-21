﻿Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class RemovePrivateMethodNeverUsedAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Unused Method"
        Friend Const Message = "Method is not used."
        Private Const Description = "When a private method is declared but not used, remove it to avoid confusion."

        Friend Shared Rule As New DiagnosticDescriptor(
            DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId(),
            Title,
            Message,
            SupportedCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:=Description,
            helpLink:=HelpLink.ForDiagnostic(DiagnosticId.RemovePrivateMethodNeverUsed))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement)
        End Sub

        Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim methodStatement = DirectCast(context.Node, MethodStatementSyntax)
            If Not methodStatement.Modifiers.Any(Function(a) a.ValueText = SyntaxFactory.Token(SyntaxKind.PrivateKeyword).ValueText) Then Exit Sub
            If IsMethodUsed(methodStatement) Then Exit Sub
            Dim diag = Diagnostic.Create(Rule, methodStatement.GetLocation())
            context.ReportDiagnostic(diag)
        End Sub

        Private Function IsMethodUsed(methodTarget As MethodStatementSyntax) As Boolean
            Dim typeDeclaration = TryCast(methodTarget.Parent.Parent, ClassBlockSyntax)
            If typeDeclaration Is Nothing Then Return True

            Dim hasIdentifier = (From invocation In typeDeclaration.DescendantNodes()?.OfType(Of InvocationExpressionSyntax)
                                 Where invocation IsNot Nothing
                                 Select TryCast(invocation?.Expression, IdentifierNameSyntax)).
                                 ToList()
            If hasIdentifier Is Nothing OrElse Not hasIdentifier.Any Then Return False
            Return hasIdentifier.Any(Function(a) a IsNot Nothing AndAlso a.Identifier.ValueText.Equals(methodTarget?.Identifier.ValueText))
        End Function
    End Class
End Namespace