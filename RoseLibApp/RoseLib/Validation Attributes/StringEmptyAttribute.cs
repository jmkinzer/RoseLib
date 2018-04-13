﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RoseLibApp.RoseLib.Validation_Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    class NotBlankAttribute : ArgumentValidationAttribute
    {
        public override void Validate(object value, string argumentName)
        {
            string strValue = value as string;
            if (value == null || strValue.Length == 0)
            {
                throw new ArgumentException("String should not be null nor empty.", argumentName);
            }
        }
    }
}
