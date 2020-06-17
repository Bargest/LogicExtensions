using System;
using Esprima.Ast;

namespace Esprima.Utils
{
    /// <summary>
    /// An AST visitor that raises events before and after visiting each node
    /// and its descendants.
    /// </summary>

    public class AstVisitorEventSource : AstVisitor
    {
        public delegate void MEventHandler<TEventArgs>(object sender, TEventArgs e);

        public event MEventHandler<Node> VisitingNode;
        public event MEventHandler<Node> VisitedNode;
        public event MEventHandler<Program> VisitingProgram;
        public event MEventHandler<Program> VisitedProgram;
        public event MEventHandler<Statement> VisitingStatement;
        public event MEventHandler<Statement> VisitedStatement;
        public event MEventHandler<Node> VisitingUnknownNode;
        public event MEventHandler<Node> VisitedUnknownNode;
        public event MEventHandler<CatchClause> VisitingCatchClause;
        public event MEventHandler<CatchClause> VisitedCatchClause;
        public event MEventHandler<FunctionDeclaration> VisitingFunctionDeclaration;
        public event MEventHandler<FunctionDeclaration> VisitedFunctionDeclaration;
        public event MEventHandler<WithStatement> VisitingWithStatement;
        public event MEventHandler<WithStatement> VisitedWithStatement;
        public event MEventHandler<WhileStatement> VisitingWhileStatement;
        public event MEventHandler<WhileStatement> VisitedWhileStatement;
        public event MEventHandler<VariableDeclaration> VisitingVariableDeclaration;
        public event MEventHandler<VariableDeclaration> VisitedVariableDeclaration;
        public event MEventHandler<TryStatement> VisitingTryStatement;
        public event MEventHandler<TryStatement> VisitedTryStatement;
        public event MEventHandler<ThrowStatement> VisitingThrowStatement;
        public event MEventHandler<ThrowStatement> VisitedThrowStatement;
        public event MEventHandler<SwitchStatement> VisitingSwitchStatement;
        public event MEventHandler<SwitchStatement> VisitedSwitchStatement;
        public event MEventHandler<SwitchCase> VisitingSwitchCase;
        public event MEventHandler<SwitchCase> VisitedSwitchCase;
        public event MEventHandler<ReturnStatement> VisitingReturnStatement;
        public event MEventHandler<ReturnStatement> VisitedReturnStatement;
        public event MEventHandler<LabeledStatement> VisitingLabeledStatement;
        public event MEventHandler<LabeledStatement> VisitedLabeledStatement;
        public event MEventHandler<IfStatement> VisitingIfStatement;
        public event MEventHandler<IfStatement> VisitedIfStatement;
        public event MEventHandler<EmptyStatement> VisitingEmptyStatement;
        public event MEventHandler<EmptyStatement> VisitedEmptyStatement;
        public event MEventHandler<DebuggerStatement> VisitingDebuggerStatement;
        public event MEventHandler<DebuggerStatement> VisitedDebuggerStatement;
        public event MEventHandler<ExpressionStatement> VisitingExpressionStatement;
        public event MEventHandler<ExpressionStatement> VisitedExpressionStatement;
        public event MEventHandler<ForStatement> VisitingForStatement;
        public event MEventHandler<ForStatement> VisitedForStatement;
        public event MEventHandler<ForInStatement> VisitingForInStatement;
        public event MEventHandler<ForInStatement> VisitedForInStatement;
        public event MEventHandler<DoWhileStatement> VisitingDoWhileStatement;
        public event MEventHandler<DoWhileStatement> VisitedDoWhileStatement;
        public event MEventHandler<Expression> VisitingExpression;
        public event MEventHandler<Expression> VisitedExpression;
        public event MEventHandler<ArrowFunctionExpression> VisitingArrowFunctionExpression;
        public event MEventHandler<ArrowFunctionExpression> VisitedArrowFunctionExpression;
        public event MEventHandler<UnaryExpression> VisitingUnaryExpression;
        public event MEventHandler<UnaryExpression> VisitedUnaryExpression;
        public event MEventHandler<UpdateExpression> VisitingUpdateExpression;
        public event MEventHandler<UpdateExpression> VisitedUpdateExpression;
        public event MEventHandler<ThisExpression> VisitingThisExpression;
        public event MEventHandler<ThisExpression> VisitedThisExpression;
        public event MEventHandler<SequenceExpression> VisitingSequenceExpression;
        public event MEventHandler<SequenceExpression> VisitedSequenceExpression;
        public event MEventHandler<ObjectExpression> VisitingObjectExpression;
        public event MEventHandler<ObjectExpression> VisitedObjectExpression;
        public event MEventHandler<NewExpression> VisitingNewExpression;
        public event MEventHandler<NewExpression> VisitedNewExpression;
        public event MEventHandler<MemberExpression> VisitingMemberExpression;
        public event MEventHandler<MemberExpression> VisitedMemberExpression;
        public event MEventHandler<BinaryExpression> VisitingLogicalExpression;
        public event MEventHandler<BinaryExpression> VisitedLogicalExpression;
        public event MEventHandler<Literal> VisitingLiteral;
        public event MEventHandler<Literal> VisitedLiteral;
        public event MEventHandler<Identifier> VisitingIdentifier;
        public event MEventHandler<Identifier> VisitedIdentifier;
        public event MEventHandler<IFunction> VisitingFunctionExpression;
        public event MEventHandler<IFunction> VisitedFunctionExpression;
        public event MEventHandler<ClassExpression> VisitingClassExpression;
        public event MEventHandler<ClassExpression> VisitedClassExpression;
        public event MEventHandler<ExportDefaultDeclaration> VisitingExportDefaultDeclaration;
        public event MEventHandler<ExportDefaultDeclaration> VisitedExportDefaultDeclaration;
        public event MEventHandler<ExportAllDeclaration> VisitingExportAllDeclaration;
        public event MEventHandler<ExportAllDeclaration> VisitedExportAllDeclaration;
        public event MEventHandler<ExportNamedDeclaration> VisitingExportNamedDeclaration;
        public event MEventHandler<ExportNamedDeclaration> VisitedExportNamedDeclaration;
        public event MEventHandler<ExportSpecifier> VisitingExportSpecifier;
        public event MEventHandler<ExportSpecifier> VisitedExportSpecifier;
        public event MEventHandler<Import> VisitingImport;
        public event MEventHandler<Import> VisitedImport;
        public event MEventHandler<ImportDeclaration> VisitingImportDeclaration;
        public event MEventHandler<ImportDeclaration> VisitedImportDeclaration;
        public event MEventHandler<ImportNamespaceSpecifier> VisitingImportNamespaceSpecifier;
        public event MEventHandler<ImportNamespaceSpecifier> VisitedImportNamespaceSpecifier;
        public event MEventHandler<ImportDefaultSpecifier> VisitingImportDefaultSpecifier;
        public event MEventHandler<ImportDefaultSpecifier> VisitedImportDefaultSpecifier;
        public event MEventHandler<ImportSpecifier> VisitingImportSpecifier;
        public event MEventHandler<ImportSpecifier> VisitedImportSpecifier;
        public event MEventHandler<MethodDefinition> VisitingMethodDefinition;
        public event MEventHandler<MethodDefinition> VisitedMethodDefinition;
        public event MEventHandler<ForOfStatement> VisitingForOfStatement;
        public event MEventHandler<ForOfStatement> VisitedForOfStatement;
        public event MEventHandler<ClassDeclaration> VisitingClassDeclaration;
        public event MEventHandler<ClassDeclaration> VisitedClassDeclaration;
        public event MEventHandler<ClassBody> VisitingClassBody;
        public event MEventHandler<ClassBody> VisitedClassBody;
        public event MEventHandler<YieldExpression> VisitingYieldExpression;
        public event MEventHandler<YieldExpression> VisitedYieldExpression;
        public event MEventHandler<TaggedTemplateExpression> VisitingTaggedTemplateExpression;
        public event MEventHandler<TaggedTemplateExpression> VisitedTaggedTemplateExpression;
        public event MEventHandler<Super> VisitingSuper;
        public event MEventHandler<Super> VisitedSuper;
        public event MEventHandler<MetaProperty> VisitingMetaProperty;
        public event MEventHandler<MetaProperty> VisitedMetaProperty;
        public event MEventHandler<ObjectPattern> VisitingObjectPattern;
        public event MEventHandler<ObjectPattern> VisitedObjectPattern;
        public event MEventHandler<SpreadElement> VisitingSpreadElement;
        public event MEventHandler<SpreadElement> VisitedSpreadElement;
        public event MEventHandler<AssignmentPattern> VisitingAssignmentPattern;
        public event MEventHandler<AssignmentPattern> VisitedAssignmentPattern;
        public event MEventHandler<ArrayPattern> VisitingArrayPattern;
        public event MEventHandler<ArrayPattern> VisitedArrayPattern;
        public event MEventHandler<VariableDeclarator> VisitingVariableDeclarator;
        public event MEventHandler<VariableDeclarator> VisitedVariableDeclarator;
        public event MEventHandler<TemplateLiteral> VisitingTemplateLiteral;
        public event MEventHandler<TemplateLiteral> VisitedTemplateLiteral;
        public event MEventHandler<TemplateElement> VisitingTemplateElement;
        public event MEventHandler<TemplateElement> VisitedTemplateElement;
        public event MEventHandler<RestElement> VisitingRestElement;
        public event MEventHandler<RestElement> VisitedRestElement;
        public event MEventHandler<Property> VisitingProperty;
        public event MEventHandler<Property> VisitedProperty;
        public event MEventHandler<ConditionalExpression> VisitingConditionalExpression;
        public event MEventHandler<ConditionalExpression> VisitedConditionalExpression;
        public event MEventHandler<CallExpression> VisitingCallExpression;
        public event MEventHandler<CallExpression> VisitedCallExpression;
        public event MEventHandler<BinaryExpression> VisitingBinaryExpression;
        public event MEventHandler<BinaryExpression> VisitedBinaryExpression;
        public event MEventHandler<ArrayExpression> VisitingArrayExpression;
        public event MEventHandler<ArrayExpression> VisitedArrayExpression;
        public event MEventHandler<AssignmentExpression> VisitingAssignmentExpression;
        public event MEventHandler<AssignmentExpression> VisitedAssignmentExpression;
        public event MEventHandler<ContinueStatement> VisitingContinueStatement;
        public event MEventHandler<ContinueStatement> VisitedContinueStatement;
        public event MEventHandler<BreakStatement> VisitingBreakStatement;
        public event MEventHandler<BreakStatement> VisitedBreakStatement;
        public event MEventHandler<BlockStatement> VisitingBlockStatement;
        public event MEventHandler<BlockStatement> VisitedBlockStatement;

        public override void Visit(Node node)
        {
            VisitingNode?.Invoke(this, node);
            base.Visit(node);
            VisitedNode?.Invoke(this, node);
        }

        protected override void VisitProgram(Program program)
        {
            VisitingProgram?.Invoke(this, program);
            base.VisitProgram(program);
            VisitedProgram?.Invoke(this, program);
        }

        protected override void VisitStatement(Statement statement)
        {
            VisitingStatement?.Invoke(this, statement);
            base.VisitStatement(statement);
            VisitedStatement?.Invoke(this, statement);
        }

        protected override void VisitUnknownNode(Node node)
        {
            VisitingUnknownNode?.Invoke(this, node);
            base.VisitUnknownNode(node);
            VisitedUnknownNode?.Invoke(this, node);
        }

        protected override void VisitCatchClause(CatchClause catchClause)
        {
            VisitingCatchClause?.Invoke(this, catchClause);
            base.VisitCatchClause(catchClause);
            VisitedCatchClause?.Invoke(this, catchClause);
        }

        protected override void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            VisitingFunctionDeclaration?.Invoke(this, functionDeclaration);
            base.VisitFunctionDeclaration(functionDeclaration);
            VisitedFunctionDeclaration?.Invoke(this, functionDeclaration);
        }

        protected override void VisitWithStatement(WithStatement withStatement)
        {
            VisitingWithStatement?.Invoke(this, withStatement);
            base.VisitWithStatement(withStatement);
            VisitedWithStatement?.Invoke(this, withStatement);
        }

        protected override void VisitWhileStatement(WhileStatement whileStatement)
        {
            VisitingWhileStatement?.Invoke(this, whileStatement);
            base.VisitWhileStatement(whileStatement);
            VisitedWhileStatement?.Invoke(this, whileStatement);
        }

        protected override void VisitVariableDeclaration(VariableDeclaration variableDeclaration)
        {
            VisitingVariableDeclaration?.Invoke(this, variableDeclaration);
            base.VisitVariableDeclaration(variableDeclaration);
            VisitedVariableDeclaration?.Invoke(this, variableDeclaration);
        }

        protected override void VisitTryStatement(TryStatement tryStatement)
        {
            VisitingTryStatement?.Invoke(this, tryStatement);
            base.VisitTryStatement(tryStatement);
            VisitedTryStatement?.Invoke(this, tryStatement);
        }

        protected override void VisitThrowStatement(ThrowStatement throwStatement)
        {
            VisitingThrowStatement?.Invoke(this, throwStatement);
            base.VisitThrowStatement(throwStatement);
            VisitedThrowStatement?.Invoke(this, throwStatement);
        }

        protected override void VisitSwitchStatement(SwitchStatement switchStatement)
        {
            VisitingSwitchStatement?.Invoke(this, switchStatement);
            base.VisitSwitchStatement(switchStatement);
            VisitedSwitchStatement?.Invoke(this, switchStatement);
        }

        protected override void VisitSwitchCase(SwitchCase switchCase)
        {
            VisitingSwitchCase?.Invoke(this, switchCase);
            base.VisitSwitchCase(switchCase);
            VisitedSwitchCase?.Invoke(this, switchCase);
        }

        protected override void VisitReturnStatement(ReturnStatement returnStatement)
        {
            VisitingReturnStatement?.Invoke(this, returnStatement);
            base.VisitReturnStatement(returnStatement);
            VisitedReturnStatement?.Invoke(this, returnStatement);
        }

        protected override void VisitLabeledStatement(LabeledStatement labeledStatement)
        {
            VisitingLabeledStatement?.Invoke(this, labeledStatement);
            base.VisitLabeledStatement(labeledStatement);
            VisitedLabeledStatement?.Invoke(this, labeledStatement);
        }

        protected override void VisitIfStatement(IfStatement ifStatement)
        {
            VisitingIfStatement?.Invoke(this, ifStatement);
            base.VisitIfStatement(ifStatement);
            VisitedIfStatement?.Invoke(this, ifStatement);
        }

        protected override void VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            VisitingEmptyStatement?.Invoke(this, emptyStatement);
            base.VisitEmptyStatement(emptyStatement);
            VisitedEmptyStatement?.Invoke(this, emptyStatement);
        }

        protected override void VisitDebuggerStatement(DebuggerStatement debuggerStatement)
        {
            VisitingDebuggerStatement?.Invoke(this, debuggerStatement);
            base.VisitDebuggerStatement(debuggerStatement);
            VisitedDebuggerStatement?.Invoke(this, debuggerStatement);
        }

        protected override void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            VisitingExpressionStatement?.Invoke(this, expressionStatement);
            base.VisitExpressionStatement(expressionStatement);
            VisitedExpressionStatement?.Invoke(this, expressionStatement);
        }

        protected override void VisitForStatement(ForStatement forStatement)
        {
            VisitingForStatement?.Invoke(this, forStatement);
            base.VisitForStatement(forStatement);
            VisitedForStatement?.Invoke(this, forStatement);
        }

        protected override void VisitForInStatement(ForInStatement forInStatement)
        {
            VisitingForInStatement?.Invoke(this, forInStatement);
            base.VisitForInStatement(forInStatement);
            VisitedForInStatement?.Invoke(this, forInStatement);
        }

        protected override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            VisitingDoWhileStatement?.Invoke(this, doWhileStatement);
            base.VisitDoWhileStatement(doWhileStatement);
            VisitedDoWhileStatement?.Invoke(this, doWhileStatement);
        }

        protected override void VisitExpression(Expression expression)
        {
            VisitingExpression?.Invoke(this, expression);
            base.VisitExpression(expression);
            VisitedExpression?.Invoke(this, expression);
        }

        protected override void VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
        {
            VisitingArrowFunctionExpression?.Invoke(this, arrowFunctionExpression);
            base.VisitArrowFunctionExpression(arrowFunctionExpression);
            VisitedArrowFunctionExpression?.Invoke(this, arrowFunctionExpression);
        }

        protected override void VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            VisitingUnaryExpression?.Invoke(this, unaryExpression);
            base.VisitUnaryExpression(unaryExpression);
            VisitedUnaryExpression?.Invoke(this, unaryExpression);
        }

        protected override void VisitUpdateExpression(UpdateExpression updateExpression)
        {
            VisitingUpdateExpression?.Invoke(this, updateExpression);
            base.VisitUpdateExpression(updateExpression);
            VisitedUpdateExpression?.Invoke(this, updateExpression);
        }

        protected override void VisitThisExpression(ThisExpression thisExpression)
        {
            VisitingThisExpression?.Invoke(this, thisExpression);
            base.VisitThisExpression(thisExpression);
            VisitedThisExpression?.Invoke(this, thisExpression);
        }

        protected override void VisitSequenceExpression(SequenceExpression sequenceExpression)
        {
            VisitingSequenceExpression?.Invoke(this, sequenceExpression);
            base.VisitSequenceExpression(sequenceExpression);
            VisitedSequenceExpression?.Invoke(this, sequenceExpression);
        }

        protected override void VisitObjectExpression(ObjectExpression objectExpression)
        {
            VisitingObjectExpression?.Invoke(this, objectExpression);
            base.VisitObjectExpression(objectExpression);
            VisitedObjectExpression?.Invoke(this, objectExpression);
        }

        protected override void VisitNewExpression(NewExpression newExpression)
        {
            VisitingNewExpression?.Invoke(this, newExpression);
            base.VisitNewExpression(newExpression);
            VisitedNewExpression?.Invoke(this, newExpression);
        }

        protected override void VisitMemberExpression(MemberExpression memberExpression)
        {
            VisitingMemberExpression?.Invoke(this, memberExpression);
            base.VisitMemberExpression(memberExpression);
            VisitedMemberExpression?.Invoke(this, memberExpression);
        }

        protected override void VisitLogicalExpression(BinaryExpression binaryExpression)
        {
            VisitingLogicalExpression?.Invoke(this, binaryExpression);
            base.VisitLogicalExpression(binaryExpression);
            VisitedLogicalExpression?.Invoke(this, binaryExpression);
        }

        protected override void VisitLiteral(Literal literal)
        {
            VisitingLiteral?.Invoke(this, literal);
            base.VisitLiteral(literal);
            VisitedLiteral?.Invoke(this, literal);
        }

        protected override void VisitIdentifier(Identifier identifier)
        {
            VisitingIdentifier?.Invoke(this, identifier);
            base.VisitIdentifier(identifier);
            VisitedIdentifier?.Invoke(this, identifier);
        }

        protected override void VisitFunctionExpression(IFunction function)
        {
            VisitingFunctionExpression?.Invoke(this, function);
            base.VisitFunctionExpression(function);
            VisitedFunctionExpression?.Invoke(this, function);
        }

        protected override void VisitClassExpression(ClassExpression classExpression)
        {
            VisitingClassExpression?.Invoke(this, classExpression);
            base.VisitClassExpression(classExpression);
            VisitedClassExpression?.Invoke(this, classExpression);
        }

        protected override void VisitExportDefaultDeclaration(ExportDefaultDeclaration exportDefaultDeclaration)
        {
            VisitingExportDefaultDeclaration?.Invoke(this, exportDefaultDeclaration);
            base.VisitExportDefaultDeclaration(exportDefaultDeclaration);
            VisitedExportDefaultDeclaration?.Invoke(this, exportDefaultDeclaration);
        }

        protected override void VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
        {
            VisitingExportAllDeclaration?.Invoke(this, exportAllDeclaration);
            base.VisitExportAllDeclaration(exportAllDeclaration);
            VisitedExportAllDeclaration?.Invoke(this, exportAllDeclaration);
        }

        protected override void VisitExportNamedDeclaration(ExportNamedDeclaration exportNamedDeclaration)
        {
            VisitingExportNamedDeclaration?.Invoke(this, exportNamedDeclaration);
            base.VisitExportNamedDeclaration(exportNamedDeclaration);
            VisitedExportNamedDeclaration?.Invoke(this, exportNamedDeclaration);
        }

        protected override void VisitExportSpecifier(ExportSpecifier exportSpecifier)
        {
            VisitingExportSpecifier?.Invoke(this, exportSpecifier);
            base.VisitExportSpecifier(exportSpecifier);
            VisitedExportSpecifier?.Invoke(this, exportSpecifier);
        }

        protected override void VisitImport(Import import)
        {
            VisitingImport?.Invoke(this, import);
            base.VisitImport(import);
            VisitedImport?.Invoke(this, import);
        }

        protected override void VisitImportDeclaration(ImportDeclaration importDeclaration)
        {
            VisitingImportDeclaration?.Invoke(this, importDeclaration);
            base.VisitImportDeclaration(importDeclaration);
            VisitedImportDeclaration?.Invoke(this, importDeclaration);
        }

        protected override void VisitImportNamespaceSpecifier(ImportNamespaceSpecifier importNamespaceSpecifier)
        {
            VisitingImportNamespaceSpecifier?.Invoke(this, importNamespaceSpecifier);
            base.VisitImportNamespaceSpecifier(importNamespaceSpecifier);
            VisitedImportNamespaceSpecifier?.Invoke(this, importNamespaceSpecifier);
        }

        protected override void VisitImportDefaultSpecifier(ImportDefaultSpecifier importDefaultSpecifier)
        {
            VisitingImportDefaultSpecifier?.Invoke(this, importDefaultSpecifier);
            base.VisitImportDefaultSpecifier(importDefaultSpecifier);
            VisitedImportDefaultSpecifier?.Invoke(this, importDefaultSpecifier);
        }

        protected override void VisitImportSpecifier(ImportSpecifier importSpecifier)
        {
            VisitingImportSpecifier?.Invoke(this, importSpecifier);
            base.VisitImportSpecifier(importSpecifier);
            VisitedImportSpecifier?.Invoke(this, importSpecifier);
        }

        protected override void VisitMethodDefinition(MethodDefinition methodDefinitions)
        {
            VisitingMethodDefinition?.Invoke(this, methodDefinitions);
            base.VisitMethodDefinition(methodDefinitions);
            VisitedMethodDefinition?.Invoke(this, methodDefinitions);
        }

        protected override void VisitForOfStatement(ForOfStatement forOfStatement)
        {
            VisitingForOfStatement?.Invoke(this, forOfStatement);
            base.VisitForOfStatement(forOfStatement);
            VisitedForOfStatement?.Invoke(this, forOfStatement);
        }

        protected override void VisitClassDeclaration(ClassDeclaration classDeclaration)
        {
            VisitingClassDeclaration?.Invoke(this, classDeclaration);
            base.VisitClassDeclaration(classDeclaration);
            VisitedClassDeclaration?.Invoke(this, classDeclaration);
        }

        protected override void VisitClassBody(ClassBody classBody)
        {
            VisitingClassBody?.Invoke(this, classBody);
            base.VisitClassBody(classBody);
            VisitedClassBody?.Invoke(this, classBody);
        }

        protected override void VisitYieldExpression(YieldExpression yieldExpression)
        {
            VisitingYieldExpression?.Invoke(this, yieldExpression);
            base.VisitYieldExpression(yieldExpression);
            VisitedYieldExpression?.Invoke(this, yieldExpression);
        }

        protected override void VisitTaggedTemplateExpression(TaggedTemplateExpression taggedTemplateExpression)
        {
            VisitingTaggedTemplateExpression?.Invoke(this, taggedTemplateExpression);
            base.VisitTaggedTemplateExpression(taggedTemplateExpression);
            VisitedTaggedTemplateExpression?.Invoke(this, taggedTemplateExpression);
        }

        protected override void VisitSuper(Super super)
        {
            VisitingSuper?.Invoke(this, super);
            base.VisitSuper(super);
            VisitedSuper?.Invoke(this, super);
        }

        protected override void VisitMetaProperty(MetaProperty metaProperty)
        {
            VisitingMetaProperty?.Invoke(this, metaProperty);
            base.VisitMetaProperty(metaProperty);
            VisitedMetaProperty?.Invoke(this, metaProperty);
        }

        protected override void VisitObjectPattern(ObjectPattern objectPattern)
        {
            VisitingObjectPattern?.Invoke(this, objectPattern);
            base.VisitObjectPattern(objectPattern);
            VisitedObjectPattern?.Invoke(this, objectPattern);
        }

        protected override void VisitSpreadElement(SpreadElement spreadElement)
        {
            VisitingSpreadElement?.Invoke(this, spreadElement);
            base.VisitSpreadElement(spreadElement);
            VisitedSpreadElement?.Invoke(this, spreadElement);
        }

        protected override void VisitAssignmentPattern(AssignmentPattern assignmentPattern)
        {
            VisitingAssignmentPattern?.Invoke(this, assignmentPattern);
            base.VisitAssignmentPattern(assignmentPattern);
            VisitedAssignmentPattern?.Invoke(this, assignmentPattern);
        }

        protected override void VisitArrayPattern(ArrayPattern arrayPattern)
        {
            VisitingArrayPattern?.Invoke(this, arrayPattern);
            base.VisitArrayPattern(arrayPattern);
            VisitedArrayPattern?.Invoke(this, arrayPattern);
        }

        protected override void VisitVariableDeclarator(VariableDeclarator variableDeclarator)
        {
            VisitingVariableDeclarator?.Invoke(this, variableDeclarator);
            base.VisitVariableDeclarator(variableDeclarator);
            VisitedVariableDeclarator?.Invoke(this, variableDeclarator);
        }

        protected override void VisitTemplateLiteral(TemplateLiteral templateLiteral)
        {
            VisitingTemplateLiteral?.Invoke(this, templateLiteral);
            base.VisitTemplateLiteral(templateLiteral);
            VisitedTemplateLiteral?.Invoke(this, templateLiteral);
        }

        protected override void VisitTemplateElement(TemplateElement templateElement)
        {
            VisitingTemplateElement?.Invoke(this, templateElement);
            base.VisitTemplateElement(templateElement);
            VisitedTemplateElement?.Invoke(this, templateElement);
        }

        protected override void VisitRestElement(RestElement restElement)
        {
            VisitingRestElement?.Invoke(this, restElement);
            base.VisitRestElement(restElement);
            VisitedRestElement?.Invoke(this, restElement);
        }

        protected override void VisitProperty(Property property)
        {
            VisitingProperty?.Invoke(this, property);
            base.VisitProperty(property);
            VisitedProperty?.Invoke(this, property);
        }

        protected override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            VisitingConditionalExpression?.Invoke(this, conditionalExpression);
            base.VisitConditionalExpression(conditionalExpression);
            VisitedConditionalExpression?.Invoke(this, conditionalExpression);
        }

        protected override void VisitCallExpression(CallExpression callExpression)
        {
            VisitingCallExpression?.Invoke(this, callExpression);
            base.VisitCallExpression(callExpression);
            VisitedCallExpression?.Invoke(this, callExpression);
        }

        protected override void VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            VisitingBinaryExpression?.Invoke(this, binaryExpression);
            base.VisitBinaryExpression(binaryExpression);
            VisitedBinaryExpression?.Invoke(this, binaryExpression);
        }

        protected override void VisitArrayExpression(ArrayExpression arrayExpression)
        {
            VisitingArrayExpression?.Invoke(this, arrayExpression);
            base.VisitArrayExpression(arrayExpression);
            VisitedArrayExpression?.Invoke(this, arrayExpression);
        }

        protected override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            VisitingAssignmentExpression?.Invoke(this, assignmentExpression);
            base.VisitAssignmentExpression(assignmentExpression);
            VisitedAssignmentExpression?.Invoke(this, assignmentExpression);
        }

        protected override void VisitContinueStatement(ContinueStatement continueStatement)
        {
            VisitingContinueStatement?.Invoke(this, continueStatement);
            base.VisitContinueStatement(continueStatement);
            VisitedContinueStatement?.Invoke(this, continueStatement);
        }

        protected override void VisitBreakStatement(BreakStatement breakStatement)
        {
            VisitingBreakStatement?.Invoke(this, breakStatement);
            base.VisitBreakStatement(breakStatement);
            VisitedBreakStatement?.Invoke(this, breakStatement);
        }

        protected override void VisitBlockStatement(BlockStatement blockStatement)
        {
            VisitingBlockStatement?.Invoke(this, blockStatement);
            base.VisitBlockStatement(blockStatement);
            VisitedBlockStatement?.Invoke(this, blockStatement);
        }
    }
}
