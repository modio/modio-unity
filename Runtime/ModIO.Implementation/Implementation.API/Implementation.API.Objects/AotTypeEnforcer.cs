using ModIO.Implementation.API.Objects;
using Newtonsoft.Json.Utilities;
using UnityEngine;
 
namespace ModIO.Implementation.API
{
	/// <summary>
	/// You do not need to use this class. This is used to ensure specific types in API request
	/// objects get AOT code generated when using IL2CPP compilation.
	/// </summary>
	internal class AotTypeEnforcer : MonoBehaviour
	{
		public void Awake()
		{
			AotHelper.EnsureList<ImageObject>();
			AotHelper.EnsureList<MetadataKVPObject>();
			AotHelper.EnsureList<ModTagObject>();
			AotHelper.EnsureList<ModObject>();
		}
	}
}
