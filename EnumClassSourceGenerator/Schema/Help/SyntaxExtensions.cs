using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnumClasses.SourceGenerators.Schema.Help;

internal static class SyntaxExtensions
{
    extension (SyntaxList<AttributeListSyntax> attributesList)
    {
        public bool TryGetByName(string name, out AttributeSyntax attribute)
        {
            attribute = attributesList.GetByName(name)!;
            return attribute is not null;
        }

        public AttributeSyntax? GetByName(string name)
        {
            var attributes = attributesList.SelectMany(x => x.Attributes).ToArray();

            if (attributes.Length == 0)
                return null;

            return attributes.FirstOrDefault(attr => attr.Name.ToString() == name);
        }
    }
}
