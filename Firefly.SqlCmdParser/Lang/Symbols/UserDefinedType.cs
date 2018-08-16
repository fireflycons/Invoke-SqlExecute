﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lang.AST;

namespace Lang.Symbols
{
    [Serializable]
    public class UserDefinedType : Symbol, IType
    {
        public UserDefinedType(string name) : base(name)
        {
            ExpressionType = ExpressionTypes.UserDefined;
        }

        public string TypeName
        {
            get { return Name; }
        }

        public ExpressionTypes ExpressionType { get; set; }
        public Ast Src { get; set; }
    }
}
