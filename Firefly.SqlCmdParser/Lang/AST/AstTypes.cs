﻿using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lang.AST
{
    public enum AstTypes
    {
        Expression,
        For,
        FunctionInvoke,
        MethodDeclr,
        Return,
        VarDeclr,
        While,
        Conditional,
        ScopeDeclr,
        Class,
        Print,
        ClassRef,
        New,
        TryCatch,
        ArrayIndex
    }
}
