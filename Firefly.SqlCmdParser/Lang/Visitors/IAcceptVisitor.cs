﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lang.Visitors
{
interface IAcceptVisitor
{
    void Visit(IAstVisitor visitor);
}
}
