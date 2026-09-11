using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HikanyanLibrary.UISystem.Editor
{
    public static class CodeGenerationValidation
    {
        private static readonly HashSet<string> Keywords = new(
            ("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while").Split(' '));
        public static bool IsIdentifier(string value) => !string.IsNullOrWhiteSpace(value) &&
            Regex.IsMatch(value, @"^[\p{L}_][\p{L}\p{Nd}_]*$") && !Keywords.Contains(value);
        public static bool IsNamespace(string value) => !string.IsNullOrWhiteSpace(value) && value.Split('.').All(IsIdentifier);
    }
}
