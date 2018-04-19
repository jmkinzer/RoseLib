﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibApp.RoseLib.Composers
{
    public interface IComposer
    {
        IComposer ParentComposer { get; set; }
        void Replace(SyntaxNode oldNode, SyntaxNode newNode);
    }
}
