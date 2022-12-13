﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RoseLib.Validation_Attributes
{
    abstract class ArgumentValidationAttribute : Attribute
    {
        public abstract void Validate(object value, string argumentName);
    }
}
