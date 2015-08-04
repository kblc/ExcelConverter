using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;

namespace ExelConverter.Core
{
    public enum TagDirection { Include = 0, Exclude = 1 }

    public class Tag
    {
        public string Value { get; private set; }
        public bool IsStrong { get; private set; }
        public TagDirection Direction { get; private set; }

        private static string DelStars(string str)
        {
            return ClearStringFromDoubleChars(str,'*');
        }

        public static string ClearStringFromDoubleChars(string str, char charToClear)
        {
            if (string.IsNullOrWhiteSpace(str))
                return string.Empty;
            var dStr = new string(new char[] { charToClear, charToClear });
            var sStr = new string(new char[] { charToClear });
            while (str.Contains(dStr))
                str = str.Replace(dStr, sStr);
            return str;
        }

        public static Tag FromString(string tagString)
        {
            if (string.IsNullOrWhiteSpace(tagString))
                return null;

            var direction = default(TagDirection);
            if (tagString.StartsWith("-"))
            {
                tagString = tagString.Substring(1);
                direction = TagDirection.Exclude;
            }
            else
                direction = TagDirection.Include;

            bool isStrong = false;
            if (tagString.StartsWith("!"))
            {
                tagString = tagString.Substring(1);
                isStrong = true;
            }
            else
                isStrong = false;

            bool withoutStars = false;
            if (tagString.StartsWith("="))
            {
                tagString = tagString.Substring(1);
                withoutStars = true;
            }
            else
                withoutStars = false;

            string value = withoutStars
                ? DelStars(tagString.Replace(' ', '*'))
                : DelStars('*' + tagString.Replace(' ', '*') + '*');

            if (string.IsNullOrWhiteSpace(tagString) || value == "*")
                return null;

            return new Tag() { Direction = direction, IsStrong = isStrong, Value = value };
        }

        public static Tag[] FromStrings(string[] tagStrings)
        {
            return tagStrings.Select(s => Tag.FromString(s)).Where(t => t != null).ToArray();
        }

        public static int GetIntersectedCount(string[] allTags, string[] items, out bool hasStrongIntersection)
        {
            hasStrongIntersection = false;
            if (allTags == null || items == null)
                return 0;

            items = items
                .Select(i => i != null ? Tag.ClearStringFromDoubleChars(i.Trim().ToLower(), ' ').Trim() : null)
                .Where(i => i != null)
                .ToArray();

            var allTg = Tag.FromStrings(allTags);
            var exTags = allTg.Where(t => t.Direction == TagDirection.Exclude).ToArray();
            var inTags = allTg.Where(t => t.Direction == TagDirection.Include).ToArray();
            var stTags = allTg.Where(t => t.IsStrong);

            hasStrongIntersection = stTags.Any(t => items.Any(i => i.Like(t.Value)));

            if (exTags.Any(t => items.Any(i => i.Like(t.Value)))) //if find excluded tag, then return zero
                return 0;

            var result = items.Select(i => inTags.Count(t => i.Like(t.Value))).DefaultIfEmpty(0).Sum();
            return result;
        }

        public static decimal IsIntersected(string[] allTags, string[] items, out bool hasStrongIntersection, decimal ifNotIntersectedDefaultValue = 0)
        {
            hasStrongIntersection = false;
            if (items.Length == 0)
                return ifNotIntersectedDefaultValue;
            var val = GetIntersectedCount(allTags, items, out hasStrongIntersection) / items.Length;
            return val == 0 ? ifNotIntersectedDefaultValue : (val > 1m ? 1m : val);
        }
    }
}
