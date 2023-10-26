using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System;
using System.Collections;

namespace VConverter
{
	public class VConverter
	{

		/// <summary>
		/// Gets the string representation of the <paramref name="value"/>.
		/// </summary>
		/// <param name="value">Any value to be converted into a string representation of it.</param>
		/// <param name="withQuotes">Indicates whether to add quotes to <see cref="string"/> and <see cref="char"/> data-types.</param>
		/// <returns>a <see cref="string"/> representation of the <paramref name="value"/>.</returns>
		public static string GetStringValue(object value, bool withQuotes=true)
		{
			try
			{
				Type type=GetTypeInfo(value);
				if(type==null || value==null)
					return "null";
				if(value is Exception valueAsException)
					return valueAsException.Source + ": " + valueAsException.Message + "\r\n" + valueAsException.StackTrace;
				if(type.IsEnum)
					return GetStringFromArray(value);
				else if(value is DateTime vdt)
					return vdt.ToString("MM-dd-yyyy | hh:mm:ss:fffffff tt");
				else if(value is string stringValue)
				{
					Dictionary<string,string> exps=new Dictionary<string, string>()
					{
						{ @"[\r]{0}[\n]", "\r\n" }
					};
					foreach(var sel in exps)
						if(Regex.IsMatch(stringValue, sel.Key))
							stringValue=Regex.Replace(stringValue, sel.Key, sel.Value);
					return withQuotes ? "\""+stringValue+"\"" : stringValue;
				}
				else if(value is char valueAsChar)
				{
					string val=valueAsChar==0 ? "" : valueAsChar.ToString();
					return withQuotes ? "'"+val+"'" : val;
				}
				else if(value is bool boolValue)
					return boolValue ? "true" : "false";
				else if(value is byte byteValue)
					return byteValue.ToString("X2");
				else if(type.IsClass)
				{
					var dictBuff=new Dictionary<string,dynamic>();
					PropertyInfo[] properties=type.GetProperties();
					foreach(PropertyInfo property in properties)
						dictBuff.Add(property.Name, property.GetValue(value));
					return GetStringValue(dictBuff);
				}
				else
					return value.ToString();
			}
			catch { }
			return "";
		}

		/// <summary>
		/// Gets the data-type of the <paramref name="value"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Type? GetTypeInfo(object value)
		{
			return IsComObject(value) ? null : value?.GetType();
		}
		/// <summary>
		/// Determines if the <paramref name="value"/> is a <see cref="__ComObject"/>.
		/// </summary>
		/// <param name="value">Any data-type value.</param>
		/// <returns><see cref="bool">true</see> upon success, <see cref="bool">false</see> otherwise.</returns>
		public static bool IsComObject(dynamic value)
		{
			return (value!=null) && Marshal.IsComObject(value);
		}
		/// <summary>
		/// Determines if the <paramref name="value"/> is <see langword="null"/>.
		/// </summary>
		/// <param name="value">Any data-type value.</param>
		/// <returns><see cref="bool">true</see> upon success, <see cref="bool">false</see> otherwise.</returns>
		public static bool IsNull(dynamic value)
		{
			return value is null || ReferenceEquals(value,DBNull.Value);
		}
		/// <summary>
		/// Gets the readl value from the <paramref name="inputValue"/>.
		/// </summary>
		/// <param name="inputValue"></param>
		/// <returns></returns>
		public static dynamic GetRealValue(dynamic inputValue)
		{
			if(inputValue is JsonElement)
				return GetRealValue((JsonElement)inputValue.GetString());
			if(((string)inputValue.ToString()).CheckValue())
			{
				string value=inputValue.ToString().Trim();
				if(Regex.IsMatch(value,@"[0-9\-eE]+") && !Regex.IsMatch(value,@"[^0-9\-eE]"))
					return Convert.ToInt32(value);
				else if(Regex.IsMatch(value,@"[0-9\-eE.]+") && !Regex.IsMatch(value,@"[^0-9\-eE.]"))
					return Convert.ToDouble(value);
				else if(value.ToLower()=="true")
					return true;
				else if(value.ToLower()=="false")
					return false;
				else if(value.StartsWith("[") && value.EndsWith("]"))
					return VJson.Deserialize(value);
				else if(value.StartsWith("{") && value.EndsWith("}"))
					return VJson.Deserialize(value);
				else if(value.ToLower()=="null")
					return null;
				else if((inputValue is IEnumerable) && inputValue.Length==1)
					return inputValue[0];
				else if(Regex.IsMatch(value,@"[0-9]+") && Regex.IsMatch(value,@"[\/\-]"))
					return DateTime.Parse(inputValue);
				return inputValue;
			}
			return null;
		}

		/// <summary>
		/// Gets the human-readable string value representing the <paramref name="value"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>a <see cref="string"/> representation of the <paramref name="value"/>.</returns>
		public static string GetReadableStringValue(dynamic value)
		{
			if(value!=null)
			{
				Type type= value.GetType();
				if(value is string || value is char)
					return value is char ? "'"+value.ToString()+"'" : "\""+value+"\"";
				else if(type.IsBoolean())
					return value ? "true" : "false";
				else if(value is DateTime)
					return value.ToString("MM-dd-yyyy | hh:mm:ss:fffffff tt");
				else return type.IsIterable() || type.Name.ToLower().Contains("collection") ? (string)GetStringFromArray(value) : (string)value.ToString();
			}
			return "null";
		}
		/// <summary>
		/// Generates a string value representing the collection of data.
		/// </summary>
		/// <param name="dynamicValue"></param>
		/// <returns></returns>
		public static string GetStringFromArray(dynamic dynamicValue)
		{
			string res="";
			if(dynamicValue!=null)
			{
				Type type= dynamicValue.GetType();
				if(type.Name.Contains("VArray"))
					return dynamicValue.ToString();
				if(type.IsIterable())
				{
					if(type.IsArray || type.IsList())
					{
						foreach(dynamic sel in dynamicValue)
							res+=(res.Length>0 ? "," : "")+GetReadableStringValue(sel);
						return "["+res+"]";
					}
					else
					{
						foreach(dynamic sel in dynamicValue)
							res+=(res.Length>0 ? "," : "")+GetReadableStringValue(sel.Key)+":"+GetReadableStringValue(sel.Value);
						return "{"+res+"}";
					}
				}
				else if(type.IsEnum)
				{
					foreach(dynamic sel in Enum.GetValues(type))
						res+=(res.Length>0 ? "," : "")+GetReadableStringValue(sel);
					return res;
				}
			}
			return "null";
		}
		/// <summary>
		/// Compares two dynamic values.
		/// </summary>
		/// <param name="xInput">One of the string values to compare.</param>
		/// <param name="yInput">One of the string values to compare.</param>
		/// <returns>an <see cref="int"/> representation of how to order the two values.</returns>
		public static int Comparator(dynamic xInput, dynamic yInput)
		{
			dynamic x=xInput is DynamicValue ? xInput.Value : xInput;
			dynamic y=yInput is DynamicValue ? yInput.Value : yInput;
			Type typeX=VConverter.GetTypeInfo(xInput);
			Type typeY=VConverter.GetTypeInfo(yInput);
			if(typeX!=null && typeY!=null)
			{
				string xtName=typeX.Name.ToLower();
				string ytName=typeY.Name.ToLower();
				if(xtName!=ytName)
					return CompareStringValues(xtName,ytName);
				if(typeX.IsBoolean())
				{
					if(typeY.IsBoolean())
						return x ? 1 : y ? -1 : 0;
					return -1;
				}
				else if(typeX.IsNumeric())
				{
					if(typeY.IsNumeric())
						return x<y ? -1 : x>y ? 1 : 0;
					return 1;
				}
				else if(x is DateTime)
				{
					if(y is DateTime)
						return x.Ticks<y.Ticks ? -1 : x.Ticks>y.Ticks ? 1 : 0;
					return 1;
				}
				else if(typeX.IsIterable())
				{
					if(typeY.IsIterable())
					{
						int lenX=GetCollectionLength(x);
						int lenY=GetCollectionLength(y);
						return lenX<lenY ? -1 : lenX>lenY ? 1 : 0;
					}
					return 1;
				}
			}
			return CompareStringValues(x, y);
		}
		/// <summary>
		/// Compares two string values.
		/// </summary>
		/// <param name="x">One of the string values to compare.</param>
		/// <param name="y">One of the string values to compare.</param>
		/// <returns>an <see cref="int"/> representation of how to order the two values.</returns>
		private static int CompareStringValues(string x, string y)
		{
			string first=GetStringValue(x,false);
			string second=GetStringValue(y,false);
			if(first==null && second!=null)
				return 1;
			if(first==null && second==null)
				return 0;
			if(first!=null && second!=null)
			{
				string tmpX=first.ToLower();
				string tmpY=second.ToLower();
				if(tmpX==tmpY)
				{
					if(first==second)
						return 0;
					for(int i = 0;i<first.Length;i++)
					{
						if(first[i]>second[i])
							return 1;
						else if(first[i]<second[i])
							return -1;
					}
					return 0;
				}
				for(int i = 0;i<Math.Min(first.Length, second.Length);i++)
				{
					if(first[i]>second[i])
						return 1;
					else if(first[i]<second[i])
						return -1;
				}
				return first.Length>second.Length ? 1 : first.Length<second.Length ? -1 : 0;
			}
			return first!=null && second==null ? -1 : 0;
		}
		/// <summary>
		/// Gets the length of the collection.
		/// </summary>
		/// <param name="collection">The collection to analyze.</param>
		/// <returns>an <see cref="int"/> representation of the number of items in the <paramref name="collection"/>.</returns>
		/// <exception cref="ArgumentNullException">The argument cannot be null.</exception>
		/// <exception cref="ArgumentException">The argument must be a collection that inherits enumerable.</exception>
		private static int GetCollectionLength(object collection)
		{
			Type type=GetTypeInfo(collection)??throw new ArgumentNullException(nameof(collection), "The collection cannot be null!");
			if(collection is IDictionary colValue)
				return colValue.Count;
			if(collection is ICollection collValue)
				return collValue.Count;
			if(collection is IList listValue)
				return listValue.Count;
			if(collection is Array arrayValue)
				return arrayValue.Length;
			if(collection is not IEnumerable)
				throw new ArgumentException("The argument must inherit from the IEnumerable interface.", nameof(collection));
			throw new InvalidOperationException("The argument (Data-Type: \"" + type.FullName + "\") could not be used in the operation.");
		}

		public static bool IsIterable(object value)
		{
			if(value is IEnumerable || value is Array)
				return true;
			else
			{
				Type? type=GetTypeInfo(value);
				if(type is not null)
				{

				}
			}
		}

		public static bool TypeHasMember(Type type, string name)
		{
			return type.GetMember(name, MemberTypes.All, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.CreateInstance | BindingFlags.Default | BindingFlags.DeclaredOnly | BindingFlags.DoNotWrapExceptions | BindingFlags.ExactBinding | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty | BindingFlags.SetField | BindingFlags.SuppressChangeType).Length>0;
		}

		public static MemberInfo[] GetTypeMemberInfo(this Type type, MemberTypes memberTypes, )

	}
}