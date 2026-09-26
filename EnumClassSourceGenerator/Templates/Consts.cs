using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClassSourceGenerator.Templates;

internal class Consts
{
    /// <summary> New line </summary>
    public const string Nl = "\r\n"; // TODO analyze Environment.NewLine
    /// <summary> New line preceded by a comma </summary>
    public const string CommaNl = ",\r\n";
}
