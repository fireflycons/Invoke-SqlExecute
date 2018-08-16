﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lang.Spaces;
using Lang.Data;
using Lang.Symbols;
using Lang.Visitors;

namespace Lang.AST
{
    public abstract class Ast : IAcceptVisitor
    {
        public MemorySpace CallingMemory { get; set; }

        public Scope CallingScope { get; set; }

        public Scope CurrentScope { get; set; }

        public Scope Global { get; set; }

        public Token Token { get; set; }

        public IType AstSymbolType { get; set; } 

        public List<Ast> Children { get; private set; }

        public Ast ConvertedExpression { get; set; }

        public bool IsLink { get; set; }

        public Ast(Token token)
        {
            Token = token;
            Children = new List<Ast>();
        }

        public void AddChild(Ast child)
        {
            if (child != null)
            {
                Children.Add(child);
            }
        }

        public override string ToString()
        {
            return Token.TokenType + " " + Children.Aggregate("", (acc, ast) => acc + " " + ast);
        }

        public abstract void Visit(IAstVisitor visitor);

        /// <summary>
        /// Used instead of reflection to determine the syntax tree type
        /// </summary>
        public abstract AstTypes AstType { get; }

        /// <summary>
        /// Pure dynamic's are resolved only at runtime
        /// </summary>
        public bool IsPureDynamic { get; set; }
    }
}
