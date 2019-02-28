using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MoreLinq;
using TipTacToe.Annotations;
using Expression = System.Linq.Expressions.Expression;
using TipTacToe.Common.UtilityClasses;
using TipTacToe.Models;
using Convert = System.Convert;
using MenuItem = TipTacToe.Common.UtilityClasses.MenuItem;

namespace TipTacToe.Common
{
    public static class Extensions
    {
        public static CultureInfo Culture = new CultureInfo("") { NumberFormat = { NumberDecimalSeparator = "," } };

        #region Constants

        private const double TOLERANCE = 0.00001;

        #endregion

        #region T Extensions

        public static bool EqualsAny<T>(this T o, params T[] os)
        {
            return os.Contains(o);
        }

        public static T DeepClone<T>(this T a)
        {
            using (var stream = new MemoryStream())
            {
                var f = new BinaryFormatter();
                f.Serialize(stream, a);
                stream.Position = 0;
                return (T)f.Deserialize(stream);
            }
        }

        #endregion

        #region String Extensions

        public static bool HasValueBetween(this string str, string start, string end)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end) &&
                   str.Contains(start) &&
                   str.Contains(end) &&
                   str.IndexOf(start, StringComparison.Ordinal) < str.IndexOf(end, StringComparison.Ordinal);
        }

        public static string Between(this string str, string start, string end)
        {
            return Regex.Match(str, $@"\{start}([^)]*)\{end}").Groups[1].Value;
        }

        public static string TakeUntil(this string str, string end)
        {
            return str.Split(new[] { end }, StringSplitOptions.None)[0];
        }

        public static string UntilWithout(this string str, string end)
        {
            return str.Split(new[] { end }, StringSplitOptions.None)[0];
        }

        public static string RemoveHTMLSymbols(this string str)
        {
            return str.Replace("&nbsp;", "")
                .Replace("&amp;", "");
        }

        public static bool IsNullWhiteSpaceOrDefault(this string str, string defVal)
        {
            return string.IsNullOrWhiteSpace(str) || str == defVal;
        }

        public static bool ContainsAny(this string str, params string[] strings)
        {
            return strings.Any(str.Contains);
        }

        public static string Remove(this string str, string substring)
        {
            return str.Replace(substring, "");
        }

        public static string RemoveMany(this string str, params string[] substrings)
        {
            return substrings.Aggregate(str, (current, substring) => current.Remove(substring));
        }

        public static string[] Split(this string str, string separator, bool includeSeparator = false)
        {
            var split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            if (includeSeparator)
            {
                var splitWithSeparator = new string[split.Length + split.Length - 1];
                var j = 0;
                for (var i = 0; i < splitWithSeparator.Length; i++)
                {
                    if (i % 2 == 1)
                        splitWithSeparator[i] = separator;
                    else
                        splitWithSeparator[i] = split[j++];
                }
                split = splitWithSeparator;
            }
            return split;
        }

        public static string[] SplitByFirst(this string str, params string[] strings)
        {
            foreach (var s in strings)
                if (str.Contains(s))
                    return str.Split(s);
            return new[] { str };
        }

        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException(@"Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        public static string[] SameWords(this string str, string otherStr, bool casaeSensitive = false, string splitBy = " ", int minWordLength = 1)
        {
            if (casaeSensitive)
            {
                str = str.ToLower();
                otherStr = otherStr.ToLower();
            }
            
            var str1Arr = str.Split(splitBy);
            var str2Arr = otherStr.Split(splitBy);
            var intersection = str1Arr.Intersect(str2Arr).Where(w => w.Length >= minWordLength);
            return intersection.ToArray();
        }

        public static string[] SameWords(this string str, string[] otherStrings, bool casaeSensitive, string splitBy = " ", int minWordLength = 1)
        {
            var sameWords = new List<string>();

            foreach (var otherStr in otherStrings)
                sameWords.AddRange(str.SameWords(otherStr, casaeSensitive, splitBy, minWordLength));

            return sameWords.Distinct().ToArray();
        }

        public static string[] SameWords(this string str, params string[] otherStrings)
        {
            return str.SameWords(otherStrings, false, " ", 1);
        }

        public static bool HasSameWords(this string str, string otherStr, bool caseSensitive = false, string splitBy = " ", int minWordLength = 1)
        {
            return str.SameWords(otherStr, caseSensitive, splitBy, minWordLength).Any();
        }

        public static bool HasSameWords(this string str, string[] otherStrings, bool caseSensitive, string splitBy = " ", int minWordLength = 1)
        {
            return str.SameWords(otherStrings, caseSensitive, splitBy, minWordLength).Any();
        }

        public static bool HasSameWords(this string str, params string[] otherStrings)
        {
            return str.SameWords(otherStrings, false, " ", 1).Any();
        }
        
        public static double? TryToDouble(this string str)
        {
            str = str.Replace(',', '.');
            var isParsable = double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
            if (isParsable)
                return value;
            return null;
        }

        public static double ToDouble(this string str)
        {
            var parsedD = str.TryToDouble();
            if (parsedD == null)
                throw new InvalidCastException("Nie można sparsować wartości double");
            return (double) parsedD;
        }

        public static bool IsDouble(this string str)
        {
            return str.TryToDouble() != null;
        }

        public static bool StartsWithAny(this string str, params string[] strings)
        {
            return strings.Any(str.StartsWith);
        }

        public static bool EndsWithAny(this string str, params string[] strings)
        {
            return strings.Any(str.EndsWith);
        }

        public static bool ContainsAll(this string str, params string[] strings)
        {
            return strings.All(str.Contains);
        }

        public static string RemoveWord(this string str, string word, string separator = " ")
        {
            return string.Join(separator, str.Split(separator).Where(w => w != word));
        }

        public static string RemoveWords(this string str, string[] words, string separator)
        {
            foreach (var w in words)
                str = str.RemoveWord(w);
            return str;
        }

        public static string RemoveWords(this string str, params string[] words)
        {
            return str.RemoveWords(words, " ");
        }

        public static bool IsUrl(this string str)
        {
            return Uri.TryCreate(str, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static string From(this string str, string substring)
        {
            var split = str.Split(substring);
            return substring + split[split.Length - 1];
        }

        public static string After(this string str, string substring)
        {
            if (!string.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).Last();
            return str;
        }

        public static string Before(this string str, string substring)
        {
            if (!string.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).First();
            return str;
        }

        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static T ToEnumOrDefault<T>(this string value, T defaultValue)
        {
            T parsedEnum;
            try
            {
                parsedEnum = (T) Enum.Parse(typeof(T), value, true);
            }
            catch (ArgumentException)
            {
                parsedEnum = defaultValue;
            }

            return parsedEnum;
        }

        public static string RegexReplace(this string str, string pattern, string replacement)
        {
            return Regex.Replace(str, pattern, replacement);
        }

        public static string AddTrailingZeroes(this string str, int n)
        {
            var strLength = str.Length;
            var zeroesToAdd = n - strLength;
            return $"{Enumerable.Repeat("0", zeroesToAdd).ToDelimitedString("")}{str}";
        }

        #endregion

        #region Double Extensions

        public static bool Eq(this double x, double y)
        {
            return Math.Abs(x - y) < TOLERANCE;
        }

        #endregion

        #region DateTime Extensions

        public static string MonthName(this DateTime date)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month);
        }

        public static DateTime Period(this DateTime date, int periodInDays)
        {
            var startDate = new DateTime();
            var myDate = new DateTime(date.Year, date.Month, date.Day);
            var diff = myDate - startDate;
            return myDate.AddDays(-(diff.TotalDays % periodInDays));
        }

        public static DateTime? ToDMY(this DateTime? dateTimeNullable)
        {
            if (dateTimeNullable == null)
                return null;

            var date = (DateTime)dateTimeNullable;
            date = new DateTime(date.Year, date.Month, date.Day);
            return date;
        }

        public static DateTime ToDMY(this DateTime dateTime)
        {
            var date = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
            return date;
        }


        #endregion

        #region Collections Extensions

        #region - Array Extensions

        public static T[] Swap<T>(this T[] a, int i, int j)
        {
            var temp = a[j];
            a[j] = a[i];
            a[i] = temp;
            return a;
        }

        public static T[] Copy<T>(this T[] arr)
        {
            return (T[]) arr.Clone();
        }

        #endregion

        #region - List Extensions

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static void RemoveBy<TSource>(this List<TSource> source, Func<TSource, bool> selector) where TSource : class
        {
            var src = source.ToArray();
            foreach (var entity in src)
            {
                if (selector(entity))
                    source.Remove(entity);
            }
        }

        public static void RemoveByMany<TSource, TKey>(this List<TSource> source, Func<TSource, TKey> selector, IEnumerable<TKey> matches) where TSource : class
        {
            foreach (var match in matches)
                source.RemoveBy(e => Equals(selector(e), match));
        }

        public static T[] ToArray<T>(this IList<T> list)
        {
            var array = new T[list.Count];
            list.CopyTo(array, 0);
            return array;
        }

        public static object[] ToArray(this IList list)
        {
            var array = new object[list.Count];
            list.CopyTo(array, 0);
            return array;
        }

        public static List<T> CloneList<T>(this List<T> oldList)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, oldList);
            stream.Position = 0;
            return (List<T>)formatter.Deserialize(stream);
        }

        #endregion

        #region - IEnumerable Extensions

        public static T LastOrNull<T>(this IEnumerable<T> enumerable)
        {
            var en = enumerable as T[] ?? enumerable.ToArray();
            return en.Any() ? en.Last() : (T)Convert.ChangeType(null, typeof(T));
        }

        public static IEnumerable<T> ConcatMany<T>(this IEnumerable<T> enumerable, params IEnumerable<T>[] enums)
        {
            return enumerable.Concat(enums.SelectMany(x => x));
        }

        public static IEnumerable<TSource> WhereByMany<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> selector, 
            IEnumerable<TKey> matches) where TSource : class
        {
            return source.Where(e => matches.Any(sel => Equals(sel, selector(e))));
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, T el)
        {
            return enumerable.Except(new[] { el });
        }

        /// <summary>
        /// Splits an array into several smaller arrays.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="enumerable">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static IEnumerable<IEnumerable<T>> SplitInParts<T>(this IEnumerable<T> enumerable, int size)
        {
            var array = enumerable as T[] ?? enumerable.ToArray();
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

        public static T[][] ToJagged<T>(this IEnumerable<IEnumerable<T>> enumerable)
        {
            return enumerable.Select(x => x.ToArray()).ToArray();
        }

        public static TrainSet ToTrainSet(this IEnumerable<TrainRow> trainRows)
        {
            return new TrainSet(trainRows);
        }

        #endregion

        #region - IQueryable Extensions

        public static TEntity LastOrNullBy<TEntity, TValue>(this IQueryable<TEntity> query, Expression<Func<TEntity, TValue>> keyFieldPredicate)
        {
            if (keyFieldPredicate == null)
                throw new ArgumentNullException(nameof(keyFieldPredicate));

            var p = keyFieldPredicate.Parameters.Single();

            if (query.LongCount() == 0)
                return default(TEntity);

            var max = query.Max(keyFieldPredicate);

            var equalsOne = (Expression)Expression.Equal(keyFieldPredicate.Body, Expression.Constant(max, typeof(TValue)));
            return query.Single(Expression.Lambda<Func<TEntity, bool>>(equalsOne, p));
        }


        #endregion

        #region - ItemCollection Extensions

        public static T[] ToArray<T>(this ItemCollection list)
        {
            var array = new T[list.Count];
            list.CopyTo(array, 0);
            return array;
        }

        #endregion

        #region - DataTable

        public static void DeleteInstantly(this DataRow row)
        {
            row.Delete();
            row.Table.AcceptChanges();
        }

        public static DataRow RowByCellValue(this DataTable table, string val)
        {
            for (var i = 0; i < table.Rows.Count; i++)
            {
                for (var j = 0; j < table.Columns.Count; j++)
                {
                    if (table.Rows[i][j].ToString() == val)
                        return table.Rows[i];
                }
            }
            return null;
        }

        public static Dictionary<string, object[]> ToColumnsDictionary(this DataTable dt, params string[] columnsToSkip)
        {
            var dict = new Dictionary<string, object[]>();
            for (var i = 0; i < dt.Columns.Count; i++)
                if (!dt.Columns[i].ColumnName.EqualsAny(columnsToSkip))
                    dict.Add(dt.Columns[i].ColumnName, dt.Rows.Cast<DataRow>().Select(row => row[dt.Columns[i]]).ToArray());
            return dict;
        }

        #endregion

        #region - Dictionary

        //public static DataTable(this Dictionary<string, object[]> dict)
        //{
        //    var dt = new DataTable();
        //    foreach (var kvp in dict)
        //    {
        //        dt.Columns.Add(kvp.Key);
        //    }
        //    return dt;
        //}

        #endregion

        #region - IList

        public static int RemoveAll<TSource>(this IList collection, Func<TSource, bool> selector)
        {
            var removedCount = 0;
            for (var i = collection.Count - 1; i >= 0; i--)
            {
                if (selector((TSource)collection[i]))
                {
                    collection.RemoveAt(i);
                    removedCount++;
                }
            }
            return removedCount;
        }

        #endregion

        #endregion

        #region DependencyObject

        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            foreach (var rawChild in LogicalTreeHelper.GetChildren(depObj))
            {
                var depObjRawChild = rawChild as DependencyObject;
                if (depObjRawChild == null) continue;
                var child = depObjRawChild;
                var tChild = child as T;
                if (tChild != null)
                    yield return tChild;

                foreach (var childOfChild in FindLogicalChildren<T>(child))
                    yield return childOfChild;
            }
        }

        public static IEnumerable<Control> FindLogicalChildren<T1, T2>(this DependencyObject depObj) 
            where T1 : DependencyObject
            where T2 : DependencyObject
        {
            return ConcatMany(FindLogicalChildren<T1>(depObj).Cast<Control>(), FindLogicalChildren<T2>(depObj).Cast<Control>());
        }
        
        public static ScrollViewer GetScrollViewer(this DependencyObject o)
        {
            if (o is ScrollViewer) { return (ScrollViewer)o; }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                    continue;
                return result;
            }

            return null;
        }

        public static T FindLogicalParent<T>(this DependencyObject child) where T : DependencyObject
        {
            while (true)
            {
                var parentObject = LogicalTreeHelper.GetParent(child);
                if (parentObject == null) return null;
                var parent = parentObject as T;
                if (parent != null) return parent;
                child = parentObject;
            }
        }

        #endregion

        #region FrameworkElement

        public static Rect ClientRectangle(this FrameworkElement el, FrameworkElement container)
        {
            var rect = VisualTreeHelper.GetDescendantBounds(el);
            var loc = el.TransformToAncestor(container).Transform(new Point(0, 0));
            rect.Location = loc;
            return rect;
        }

        public static bool HasClientRectangle(this FrameworkElement el, FrameworkElement container)
        {
            return !VisualTreeHelper.GetDescendantBounds(el).IsEmpty;
        }

        public static bool IsWithinBounds(this FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.IntersectsWith(bounds);
        }

        #endregion

        #region Textbox Extensions

        public static void ResetValue(this TextBox thisTxtBox, bool force = false)
        {
            var text = thisTxtBox.Text;
            var tag = thisTxtBox.Tag;
            if (tag == null)
                return;

            var placeholder = tag.ToString();
            if (text != placeholder && !string.IsNullOrWhiteSpace(text) && !force)
                return;

            var currBg = ((SolidColorBrush)thisTxtBox.Foreground).Color;
            var newBrush = new SolidColorBrush(Color.FromArgb(128, currBg.R, currBg.G, currBg.B));

            thisTxtBox.FontStyle = FontStyles.Italic;
            thisTxtBox.Foreground = newBrush;
            thisTxtBox.Text = placeholder;
        }

        public static void ClearValue(this TextBox thisTxtBox, bool force = false)
        {
            var text = thisTxtBox.Text;
            var tag = thisTxtBox.Tag;
            if (tag == null)
                return;

            var placeholder = tag.ToString();
            if (text != placeholder && !force)
                return;

            var currBg = ((SolidColorBrush)thisTxtBox.Foreground).Color;
            var newBrush = new SolidColorBrush(Color.FromArgb(255, currBg.R, currBg.G, currBg.B));

            thisTxtBox.FontStyle = FontStyles.Normal;
            thisTxtBox.Foreground = newBrush;
            thisTxtBox.Text = string.Empty;
        }

        public static bool IsNullWhitespaceOrTag(this TextBox txt)
        {
            return txt.Text.IsNullWhiteSpaceOrDefault(txt.Tag?.ToString() ?? "");
        }

        #endregion

        #region ComboBox Extensions

        public static void SelectByCustomId(this ComboBox rddl, int id)
        {
            rddl.SelectedItem = rddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
        }

        public static void SelectByCustomValue(this ComboBox rddl, string val)
        {
            rddl.SelectedItem = rddl.ItemsSource.Cast<DdlItem>().Single(i => i.Text == val);
        }

        public static T SelectedEnumValue<T>(this ComboBox rddl)
        {
            var selectedItem = (DdlItem)rddl.SelectedItem;
            var enumType = typeof(T);

            var value = (Enum)Enum.ToObject(enumType, selectedItem.Index);
            if (Enum.IsDefined(enumType, value) == false)
                throw new NotSupportedException($"Nie można przekonwertować wartości na podany typ: {enumType}");

            return (T)(object)value;
        }

        public static DdlItem SelectedDdlItem(this ComboBox ddl)
        {
            return (DdlItem) ddl.SelectedItem;
        }

        #endregion

        #region RadListBox Extensions

        public static void SelectByCustomId(this ListBox mddl, int id)
        {
            var item = mddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
            mddl.SelectedItems.Add(item);
        }

        public static void SelectAll(this ListBox mddl)
        {
            mddl.UnselectAll();
            var items = mddl.Items.ToArray();
            foreach (var item in items)
                mddl.SelectedItems.Add(item);
        }

        public static void UnselectAll(this ListBox mddl)
        {
            var selectedItems = mddl.SelectedItems.ToArray();
            foreach (var item in selectedItems)
                mddl.SelectedItems.Remove(item);
        }

        public static void SelectByCustomIds(this ListBox mddl, IEnumerable<int> ids)
        {
            var ddlItems = mddl.ItemsSource.Cast<DdlItem>().Where(i => ids.Any(id => i.Index == id)).ToList();
            foreach (var item in ddlItems)
                mddl.SelectedItems.Add(item);
        }

        public static int[] SelectedCustomIds(this ListBox mddl)
        {
            return mddl.SelectedItems.Cast<DdlItem>().Select(i => i.Index).ToArray();
        }


        #endregion

        #region RadGridView Extensions

        public static void RefreshWith<T>(this DataGrid gv, ICollection<T> data)
        {
            gv.SelectedItems.Clear();
            gv.SelectedItem = null;
            gv.ItemsSource = null;
            gv.ItemsSource = data;
        }

        public static void RefreshWith(this DataGrid gv, DataTable dt)
        {
            gv.ItemsSource = null;
            gv.ItemsSource = dt.DefaultView;
        }

        public static void ResetAndHide(this DataGrid gv)
        {
            gv.ItemsSource = null;
            gv.Columns.Clear();
            gv.Visibility = Visibility.Hidden;
        }

        public static void ScrollToStart(this ListBox lv, bool selectLast = false)
        {
            if (lv.Items.Count > 0)
                lv.GetScrollViewer().ScrollToTop();
        }

        public static void ScrollToEnd(this ListBox lv, bool selectLast = false)
        {
            if (lv.Items.Count > 0)
                lv.GetScrollViewer().ScrollToBottom();
        }

        #endregion

        #region TreeView

        public static void ExpandAll(this TreeView tv)
        {
            tv.Items.OfType<MenuItem>().ToList().ForEach(ExpandAll);
        }

        private static void ExpandAll(MenuItem treeItem)
        {
            treeItem.IsExpanded = true;
            foreach (var childItem in treeItem.Items)
                ExpandAll(childItem);
        }

        #endregion

        #region Point

        public static Point Translate(this Point p, double x, double y)
        {
            return new Point(p.X + x, p.Y + y);
        }

        public static Point Translate(this Point p, Point t)
        {
            return new Point(p.X + t.X, p.Y + t.Y);
        }

        public static Point TranslateX(this Point p, double x)
        {
            return new Point(p.X + x, p.Y);
        }

        public static Point TranslateY(this Point p, double y)
        {
            return new Point(p.X, p.Y + y);
        }

        public static bool IsOnScreen(this Point absolutePoint)
        {
            var screenRect = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            return screenRect.Contains(absolutePoint);
        }
        
        public static bool IsXOnScreen(this Point absolutePoint)
        {
            return absolutePoint.X >= 0 && absolutePoint.X <= SystemParameters.PrimaryScreenWidth;
        }

        public static bool IsYOnScreen(this Point absolutePoint)
        {
            return absolutePoint.Y >= 0 && absolutePoint.Y <= SystemParameters.PrimaryScreenHeight;
        }

        public static Point Select(this Point absolutePoint, Func<Point, Point> selector)
        {
            return selector(absolutePoint);
        }

        public static double Distance(this Point p1, Point p2)
        {
            var dX = p1.X - p2.X;
            var dY = p1.Y - p2.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }

        #endregion

        #region Enum Extensions

        public static string ConvertToString(this Enum en, bool toLower = false, string betweenWords = "")
        {
            var name = Enum.GetName(en.GetType(), en);
            if (!string.IsNullOrEmpty(betweenWords))
                name = Regex.Replace(name ?? "", @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", $"{betweenWords}$0");
            if (toLower)
                name = name?.ToLower();
            return name;
        }

        #endregion

        #region Object Extensions

        public static T ToEnum<T>(this object value) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T must be an Enum");
            return (T)Enum.Parse(typeof(T), value.ToString().RemoveMany(" ", "-"), true);
        }

        public static int? ToIntN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToInt32(obj);
            return int.TryParse(obj.ToString().Replace(".", ",").TakeUntil(","), NumberStyles.Any, Culture, out int tmpvalue) ? tmpvalue : (int?)null;
        }

        public static int ToInt(this object obj)
        {
            var intN = obj.ToIntN();
            if (intN != null) return (int)intN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static uint? ToUIntN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToUInt32(obj);
            return uint.TryParse(obj.ToString().Replace(".", ",").TakeUntil(","), NumberStyles.Any, Culture, out uint tmpvalue) ? tmpvalue : (uint?)null;
        }

        public static uint ToUInt(this object obj)
        {
            var uintN = obj.ToUIntN();
            if (uintN != null) return (uint)uintN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static long? ToLongN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToInt64(obj);
            return long.TryParse(obj.ToString().Replace(".", ",").TakeUntil(","), NumberStyles.Any, Culture, out long tmpvalue) ? tmpvalue : (long?)null;
        }

        public static long ToLong(this object obj)
        {
            var longN = obj.ToLongN();
            if (longN != null) return (long)longN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static double? ToDoubleN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToDouble(obj);
            return double.TryParse(obj.ToString().Replace(".", ","), NumberStyles.Any, Culture, out double tmpvalue) ? tmpvalue : (double?)null;
        }

        public static double ToDouble([NotNull] this object obj)
        {
            var doubleN = obj.ToDoubleN();
            if (doubleN != null) return (double)doubleN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static decimal? ToDecimalN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToDecimal(obj);
            return decimal.TryParse(obj.ToString().Replace(".", ","), NumberStyles.Any, Culture, out decimal tmpvalue) ? tmpvalue : (decimal?)null;
        }

        public static decimal ToDecimal([NotNull] this object obj)
        {
            var decimalN = obj.ToDecimalN();
            if (decimalN != null) return (decimal)decimalN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static DateTime? ToDateTimeN(this object obj)
        {
            return DateTime.TryParse(obj?.ToString(), out DateTime tmpvalue) ? tmpvalue : (DateTime?)null;
        }

        public static DateTime ToDateTime(this object obj)
        {
            var DateTimeN = obj.ToDateTimeN();
            if (DateTimeN != null) return (DateTime)DateTimeN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static DateTime ToDateTimeExact(this object obj, string format)
        {
            //var t = DateTime.UtcNow.ToString("ddd, d MMM yy HH:mm:ss", CultureInfo.InvariantCulture);
            return DateTime.ParseExact(obj?.ToString(), format, CultureInfo.InvariantCulture);
        }

        public static bool? ToBoolN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return (bool)obj;
            return bool.TryParse(obj.ToString(), out bool tmpvalue) ? tmpvalue : (bool?)null;
        }

        public static bool ToBool(this object obj)
        {
            var boolN = obj.ToBoolN();
            if (boolN != null) return (bool)boolN;
            throw new ArgumentNullException(nameof(obj));
        }

        #endregion

        #region Label Extensions

        public static void SetWrappedText(this Label lbl, string text)
        {
            lbl.Content = new TextBlock { Text = text };
        }

        #endregion
    }
}
