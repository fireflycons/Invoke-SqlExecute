﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lang.AST;
using Lang.Data;
using Lang.Exceptions;
using Lang.Spaces;
using Lang.Symbols;
using Lang.Utils;

namespace Lang.Visitors
{
    public class ScopeBuilderVisitor : IAstVisitor
    {
        private void SetScopeType(ScopeType scopeType)
        {
            ScopeContainer.CurrentScopeType = scopeType;
        }

        private Scope Global { get; set; }

        private Scope Current
        {
            get { return ScopeTree.Current; }
            set { ScopeContainer.CurrentScopeStack.Current = value; }
        }

        private ScopeStack<Scope> ScopeTree { get { return ScopeContainer.CurrentScopeStack; } }

        private MethodDeclr CurrentMethod { get; set; }

        private Boolean ResolvingTypes { get; set; }

        private ScopeContainer ScopeContainer { get; set; }

        public ScopeBuilderVisitor(bool resolvingTypes = false)
        {
            ResolvingTypes = resolvingTypes;
            ScopeContainer = new ScopeContainer();
        }

        public void Visit(Conditional ast)
        {
            if (ast.Predicate != null)
            {
                ast.Predicate.Visit(this);
            }

            ast.Body.Visit(this);

            if (ast.Alternate != null)
            {
                ast.Alternate.Visit(this);
            }

            SetScope(ast);
        }

        public void Visit(Expr ast)
        {
            if (ast.Left != null)
            {
                ast.Left.Visit(this);
            }

            if (ast.Right != null)
            {
                ast.Right.Visit(this);
            }

            SetScope(ast);

            if (ast.Left == null && ast.Right == null)
            {
                ast.AstSymbolType = ResolveOrDefine(ast);
            }
            else
            {
                if (ResolvingTypes)
                {
                    ast.AstSymbolType = GetExpressionType(ast.Left, ast.Right, ast.Token);
                }
            }
        }

        /// <summary>
        /// Creates a type for built in types or resolves user defined types
        /// </summary>
        /// <param name="ast"></param>
        /// <returns></returns>
        private IType ResolveOrDefine(Expr ast)
        {
            if (ast == null)
            {
                return null;
            }

            switch (ast.Token.TokenType)
            {
                case TokenType.Word: return ResolveType(ast);
            }

            return ScopeUtil.CreateSymbolType(ast);
        }

        /// <summary>
        /// Determines user type
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private IType GetExpressionType(Ast left, Ast right, Token token)
        {
            switch (token.TokenType)
            {
                case TokenType.Ampersand:
                case TokenType.Or:
                case TokenType.GreaterThan:
                case TokenType.Compare:
                case TokenType.LessThan:
                case TokenType.NotCompare:
                    return new BuiltInType(ExpressionTypes.Boolean);
                
                case TokenType.Method:
                case TokenType.Infer:
                    if (right is MethodDeclr)
                    {
                        return new BuiltInType(ExpressionTypes.Method, right);
                    }

                    return right.AstSymbolType;
            }

            if (!ResolvingTypes && (left.AstSymbolType == null || right.AstSymbolType == null))
            {
                return null;
            }

            if (!TokenUtil.EqualOrPromotable(left.AstSymbolType.ExpressionType, right.AstSymbolType.ExpressionType))
            {
                throw new Exception("Mismatched types");
            }

            return left.AstSymbolType;
        }

        public void Visit(FuncInvoke ast)
        {
            if (ast.CallingScope != null)
            {
                ast.Arguments.ForEach(arg => arg.CallingScope = ast.CallingScope);
            }

            ast.Arguments.ForEach(arg => arg.Visit(this));

            SetScope(ast);

            var functionType = Resolve(ast.FunctionName) as MethodSymbol;

            if (functionType != null && ast.Arguments.Count < functionType.MethodDeclr.Arguments.Count)
            {
                var curriedMethod = CreateCurriedMethod(ast, functionType);

                curriedMethod.Visit(this);

                var methodSymbol = ScopeUtil.DefineMethod(curriedMethod);

                Current.Define(methodSymbol);

                ast.ConvertedExpression = curriedMethod;
            }
            else if(ResolvingTypes)
            {
                ast.AstSymbolType = ResolveType(ast.FunctionName, ast.CurrentScope);
            }
        }

        private LambdaDeclr CreateCurriedMethod(FuncInvoke ast, MethodSymbol functionType)
        {
            var srcMethod = functionType.MethodDeclr;

            var fixedAssignments = new List<VarDeclrAst>();

            var count = 0;
            foreach (var argValue in ast.Arguments)
            {
                var srcArg = srcMethod.Arguments[count] as VarDeclrAst;

                var token = new Token(srcArg.DeclarationType.Token.TokenType, argValue.Token.TokenValue);

                var declr = new VarDeclrAst(token, srcArg.Token, new Expr(argValue.Token));

                // if we're creating a curry using a variable then we need to resolve the variable type
                // otherwise we can make a symbol for the literal
                var newArgType = argValue.Token.TokenType == TokenType.Word ? 
                                        ast.CurrentScope.Resolve(argValue).Type
                                    :   ScopeUtil.CreateSymbolType(argValue);

                // create a symbol type for the target we're invoking on so we can do type checking
                var targetArgType = ScopeUtil.CreateSymbolType(srcArg.DeclarationType);

                if (!TokenUtil.EqualOrPromotable(newArgType, targetArgType))
                {
                    throw new InvalidSyntax(String.Format("Cannot pass argument {0} of type {1} to partial function {2} as argument {3} of type {4}",
                        argValue.Token.TokenValue, 
                        newArgType.TypeName, 
                        srcMethod.MethodName.Token.TokenValue, 
                        srcArg.VariableName.Token.TokenValue,
                        targetArgType.TypeName)); 
                }

                fixedAssignments.Add(declr);

                count++;
            }

            var newBody = fixedAssignments.Concat(srcMethod.Body.ScopedStatements).ToList();

            var curriedMethod = new LambdaDeclr(srcMethod.Arguments.Skip(ast.Arguments.Count).ToList(), new ScopeDeclr(newBody));

            SetScope(curriedMethod);

            return curriedMethod;
        }

        /// <summary>
        /// Resolve the target ast type from the current scope, OR give it a scope to use.  
        /// Since things can be resolved in two passes (initial scope and forward reference scope)
        /// we want to be able to pass in a scope override.  The second value is usually only ever used
        /// on the second pass when determining forward references
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="currentScope"></param>
        /// <returns></returns>
        private IType ResolveType(Ast ast, Scope currentScope = null)
        {
            var scopeTrys = new List<Scope> { currentScope, ast.CurrentScope };

            try
            {
                return Current.Resolve(ast).Type;
            }
            catch (Exception ex)
            {
                try
                {
                    return ast.CallingScope.Resolve(ast).Type;
                }
                catch
                {
                    foreach (var scopeTry in scopeTrys)
                    {
                        try
                        {
                            if (scopeTry == null)
                            {
                                continue;
                            }

                            var resolvedType = scopeTry.Resolve(ast);

                            var allowedFwdReferences = scopeTry.AllowedForwardReferences(ast);

                            if (allowedFwdReferences ||
                                scopeTry.AllowAllForwardReferences ||
                                resolvedType is ClassSymbol ||
                                resolvedType is MethodSymbol)
                            {
                                return resolvedType.Type;
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }

            if (ResolvingTypes)
            {
                if (ast.IsPureDynamic)
                {
                    return new BuiltInType(ExpressionTypes.Inferred);
                }

                throw new UndefinedElementException(String.Format("Undefined element {0}",
                                                                          ast.Token.TokenValue));
            }

            return null;
        }


        private Symbol Resolve(String name)
        {
            try
            {
                return Current.Resolve(name);
            }
            catch (Exception ex)
            {
                try
                {
                    return Global.Resolve(name);
                }
                catch
                {
                    if (ResolvingTypes)
                    {
                        return null;
                    }
                }

                throw;
            }
        }

        private Symbol Resolve(Ast ast)
        {
            return Resolve(ast.Token.TokenValue);
        }

        public void Visit(VarDeclrAst ast)
        {
            var isVar = ast.DeclarationType.Token.TokenType == TokenType.Infer;

            if (ast.DeclarationType != null && !isVar)
            {
                var symbol = ScopeUtil.DefineUserSymbol(ast.DeclarationType, ast.VariableName);

                symbol.IsArray = ast is ArrayDeclrAst;

                DefineToScope(ast, symbol);

                ast.AstSymbolType = symbol.Type;
            }

            if (ast.VariableValue == null && isVar)
            {
                var symbol = ScopeUtil.DefineUserSymbol(ast.DeclarationType, ast.VariableName);

                DefineToScope(ast, symbol);

                ast.AstSymbolType = symbol.Type;
            }

            else if (ast.VariableValue != null)
            {
                ast.VariableValue.Visit(this);

                // if its type inferred, determine the declaration by the value's type
                if (isVar)
                {
                    // if the right hand side is a method declaration, make sure to track the source value
                    // this way we can reference it later to determine not only that this is a method type, but what
                    // is the expected return value for static type checking later

                    var val = ast.VariableValue.ConvertedExpression ?? ast.VariableValue;

                    ast.AstSymbolType = val is MethodDeclr
                                            ? new BuiltInType(ExpressionTypes.Method, val)
                                            : val.AstSymbolType;

                    var symbol = ScopeUtil.DefineUserSymbol(ast.AstSymbolType, ast.VariableName);

                    symbol.IsArray = ast is ArrayDeclrAst;

                    DefineToScope(ast, symbol);
                }
                else if (ResolvingTypes)
                {
                    var declaredType = ScopeUtil.CreateSymbolType(ast.DeclarationType);

                    var value = ast.VariableValue.ConvertedExpression ?? ast.VariableValue;

                    ReturnAst returnType = null;

                    // when we're resolving types check if the rhs is a function invoke. if it is, see 
                    // what the return value of the src expression is so we can make sure that the 
                    // lhs and the rhs match.
                    try
                    {
                        returnType =
                            value is FuncInvoke
                                ? ((value as FuncInvoke).AstSymbolType) != null
                                      ? ((value as FuncInvoke).AstSymbolType.Src as MethodDeclr).ReturnAst
                                      : null
                                : null;

                    }
                    catch
                    {
                    }

                    value = returnType != null ? returnType.ReturnExpression : value;

                    if (!TokenUtil.EqualOrPromotable(value.AstSymbolType.ExpressionType, declaredType.ExpressionType))
                    {
                        throw new InvalidSyntax(String.Format("Cannot assign {0} of type {1} to {2}", ast.VariableValue, 
                            value.AstSymbolType.ExpressionType, 
                            declaredType.ExpressionType));
                    } 

                }
            }

            SetScope(ast);
        }

        private void DefineToScope(Ast ast, Symbol symbol)
        {
            if (ast.CurrentScope != null && ast.CurrentScope.Symbols.ContainsKey(symbol.Name))
            {
                Symbol old = ast.CurrentScope.Resolve(symbol.Name);
                if (old.Type == null)
                {
                    ast.CurrentScope.Define(symbol);
                }
            }

            Current.Define(symbol);
        }

        public void Visit(MethodDeclr ast)
        {
            var previousMethod = CurrentMethod;

            CurrentMethod = ast;

            var symbol = ScopeUtil.DefineMethod(ast);

            Current.Define(symbol);

            ScopeTree.CreateScope();

            ast.Arguments.ForEach(arg => arg.Visit(this));

            ast.Body.Visit(this);

            SetScope(ast);

            if (symbol.Type.ExpressionType == ExpressionTypes.Inferred)
            {
                if (ast.ReturnAst == null)
                {
                    ast.AstSymbolType = new BuiltInType(ExpressionTypes.Void);
                }
                else
                {
                    ast.AstSymbolType = ast.ReturnAst.AstSymbolType;
                }
            }
            else
            {
                ast.AstSymbolType = symbol.Type;
            }

            ValidateReturnStatementType(ast, symbol);

            ScopeTree.PopScope();

            CurrentMethod = previousMethod;
        }

        private void ValidateReturnStatementType(MethodDeclr ast, Symbol symbol)
        {
            if (!ResolvingTypes)
            {
                return;
            }

            IType returnStatementType;

            // no return found
            if (ast.ReturnAst == null)
            {
                returnStatementType = new BuiltInType(ExpressionTypes.Void);
            }
            else
            {
                returnStatementType = ast.ReturnAst.AstSymbolType;
            }

            var delcaredSymbol = ScopeUtil.CreateSymbolType(ast.MethodReturnType);

            // if its inferred, just use whatever the return statement i
            if (delcaredSymbol.ExpressionType == ExpressionTypes.Inferred)
            {
                return;
            }

            if (!TokenUtil.EqualOrPromotable(returnStatementType.ExpressionType, delcaredSymbol.ExpressionType))
            {
                throw new InvalidSyntax(String.Format("Return type {0} for function {1} is not of the same type of declared method (type {2})",
                    returnStatementType.ExpressionType, symbol.Name, delcaredSymbol.ExpressionType));
            }
        }

        public void Visit(WhileLoop ast)
        {
            ast.Predicate.Visit(this);

            ast.Body.Visit(this);

            SetScope(ast);
        }

        public void Visit(ScopeDeclr ast)
        {
            ScopeTree.CreateScope();

            ast.ScopedStatements.ForEach(statement => statement.Visit(this));

            SetScope(ast);

            ScopeTree.PopScope();
        }

        private void SetScope(Ast ast)
        {
            if (ast.CurrentScope == null)
            {
                ast.CurrentScope = Current;

                ast.Global = Global;
            }

            if (ast.CurrentScope != null && ast.CurrentScope.Symbols.Count < Current.Symbols.Count)
            {
                ast.CurrentScope = Current;
            }

            if (ast.Global != null && ast.Global.Symbols.Count < Global.Symbols.Count)
            {
                ast.Global = Global;
            }
        }

        public void Visit(ForLoop ast)
        {
            ast.Setup.Visit(this);

            ast.Predicate.Visit(this);

            if (ResolvingTypes && ast.Predicate.AstSymbolType.ExpressionType != ExpressionTypes.Boolean)
            {
                throw new InvalidSyntax("For loop predicate has to evaluate to a boolean");
            }

            ast.Update.Visit(this);

            ast.Body.Visit(this);

            SetScope(ast);
        }

        public void Visit(ReturnAst ast)
        {
            if (ast.ReturnExpression != null)
            {
                ast.ReturnExpression.Visit(this);

                ast.AstSymbolType = ast.ReturnExpression.AstSymbolType;

                CurrentMethod.ReturnAst = ast;
            }
        }

        public void Visit(PrintAst ast)
        {
            ast.Expression.Visit(this);

            if (ResolvingTypes)
            {
                if (ast.Expression.AstSymbolType == null)
                {
                    throw new InvalidSyntax("Undefined expression in print statement");
                }

                if (ast.Expression.AstSymbolType.ExpressionType == ExpressionTypes.Void)
                {
                    throw new InvalidSyntax("Cannot print a void expression");
                }

                if (ast.Expression.AstSymbolType.ExpressionType == ExpressionTypes.Method)
                {
                    var returnAst = (ast.Expression.AstSymbolType.Src as MethodDeclr);

                    if (returnAst != null)
                    {
                        var retStatement = returnAst.ReturnAst;

                        if (retStatement == null)
                        {
                            throw new InvalidSyntax("Cannot print a void expression");
                        }
                    }

                }
            }
        }

        public void Start(Ast ast)
        {
            LambdaDeclr.LambdaCount = 0;

            if (ast.Global != null)
            {
                Global = ast.Global;
            }

            ast.Visit(this);
        }

        public void Visit(ClassAst ast)
        {
            if (Global == null)
            {
                Global = Current;
            }

            var classSymbol = ScopeUtil.DefineClassSymbol(ast);

            Current.Define(classSymbol);

            SetScopeType(ScopeType.Class);

            SetScopeSource(classSymbol);

            ScopeTree.CreateScope();

            ast.Body.Visit(this);

            classSymbol.Symbols = ast.Body.CurrentScope.Symbols;

            //redefine the class symbol now with actual symbols
            Current.Define(classSymbol);

            ast.Body.CurrentScope.AllowAllForwardReferences = true;

            ScopeTree.PopScope();

            if (ast.Global == null)
            {
                ast.Global = Global;
            }

            SetScopeType(ScopeType.Global);
        }

        public static MethodDeclr GetConstructorForClass(ClassAst ast)
        {
            return
                ast.Body.ScopedStatements.Where(i => i is MethodDeclr)
                                         .FirstOrDefault(i => (i as MethodDeclr).MethodName.Token.TokenValue == SpecialNames.CONSTRUCTOR_NAME) as MethodDeclr;
        }

        private void SetScopeSource(Symbol classSymbol)
        {
            Current = classSymbol;
        }

        public void Visit(ClassReference ast)
        {
            if (!ResolvingTypes)
            {
                return;
            }

            var declaredSymbol = Resolve(ast.ClassInstance);

            if (declaredSymbol == null)
            {
                throw new UndefinedElementException(string.Format("Class instance '{0}' does not exist in current scope", ast.ClassInstance.Token.TokenValue));
            }

            var classScope = Resolve(declaredSymbol.Type.TypeName) as ClassSymbol;

            if (classScope == null)
            {
                classScope = Global.Resolve(declaredSymbol.Type.TypeName) as ClassSymbol;
            }

            var oldScope = Current;
            
            Current = classScope;

            foreach (var reference in ast.Deferences)
            {
                if (reference == ast.Deferences.Last())
                {
                    reference.CallingScope = oldScope;
                }

                reference.CurrentScope = Current;

                reference.IsPureDynamic = Current == null;

                reference.Visit(this);

                var field = Resolve(reference);


                if (field == null && !reference.IsPureDynamic)
                {
                    throw new InvalidSyntax(String.Format("Class {0} has no field named {1}", declaredSymbol.Type.TypeName, reference.Token.TokenValue));
                }

                if (field != null && field.Type.ExpressionType == ExpressionTypes.UserDefined)
                {
                    Current = Global.Resolve(field.Type.TypeName) as ClassSymbol;
                }
            }

            Current = oldScope; 

            ast.AstSymbolType = ast.Deferences.Last().AstSymbolType;

            if (ast.AstSymbolType.ExpressionType == ExpressionTypes.Method)
            {
                try
                {
                    ast.AstSymbolType = (ast.AstSymbolType.Src as MethodDeclr).ReturnAst.AstSymbolType;
                }
                catch (Exception ex)
                {
                    
                }
            }

            SetScope(ast);

            SetScope(ast.ClassInstance);
        }

        public void Visit(NewAst ast)
        {
            ast.Args.ForEach(arg => arg.Visit(this));

            if (ResolvingTypes)
            {
                if (ast.Name.Token.TokenType == TokenType.Word && !ast.IsArray)
                {
                    var className = Resolve(ast.Name) as ClassSymbol;

                    if (className == null)
                    {
                        className = ast.Global.Resolve(ast.Name) as ClassSymbol;

                        if (className == null)
                        {
                            throw new InvalidSyntax(String.Format("Class {0} is undefined", ast.Name.Token.TokenValue));
                        }
                    }

                    ValidateClassConstructorArgs(ast, className);

                    ast.AstSymbolType = className.Type;
                }
                else if (ast.IsArray)
                {

                }
                else
                {
                    throw new InvalidSyntax("Cannot new type of " + ast.Name);
                }
            }

            SetScope(ast);
        }

        public void Visit(TryCatchAst ast)
        {
            ast.TryBody.Visit(this);

            if (ast.CatchBody != null)
            {
                ast.CatchBody.Visit(this);
            }
        }
         
        public void Visit(ArrayIndexAst ast)
        {
            ast.Name.Visit(this);

            ast.Index.Visit(this);

            if (ResolvingTypes)
            {
                var symbol = Resolve(ast.Name);

                if (!symbol.IsArray)
                {
                    throw new InvalidSyntax("Trying to index a non array");
                }

                if (ast.Index.AstSymbolType.ExpressionType != ExpressionTypes.Int)
                {
                    throw new InvalidSyntax("Cannot index an array with a non integer type: " + ast.Index.AstSymbolType.ExpressionType);
                }
            }
        }

        private void ValidateClassConstructorArgs(NewAst ast, ClassSymbol classSource)
        {
            if (!ResolvingTypes)
            {
                return;
            }

            if (classSource == null)
            {
                throw new ArgumentException("classSource");
            }

            var constructor = GetConstructorForClass(classSource.Src as ClassAst);

            if (constructor != null)
            {
                if (CollectionUtil.IsNullOrEmpty(ast.Args) && !CollectionUtil.IsNullOrEmpty(constructor.Arguments))
                {
                    throw new InvalidSyntax("Not enough arguments for constructor arguments");
                }

                if (ast.Args.Count != constructor.Arguments.Count)
                {
                    throw new InvalidSyntax("Not enough arguments for constructor arguments");
                }

                (classSource.Src as ClassAst).Constructor = constructor;
            }
        }
    }
}

