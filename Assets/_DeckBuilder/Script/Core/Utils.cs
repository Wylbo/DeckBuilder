using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class Utils
{

}


public static class EnumUtils
{
	public static IEnumerable<T> GetValues<T>()
	{
		return Enum.GetValues(typeof(T)).Cast<T>();
	}
}