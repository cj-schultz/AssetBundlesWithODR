using UnityEngine;
using UnityEditor;
using System.Collections;

#if ENABLE_IOS_ON_DEMAND_RESOURCES || ENABLE_IOS_APP_SLICING
using UnityEditor.iOS;
public class BuildResources
{
	[InitializeOnLoadMethod]
	static void SetupResourcesBuild()
	{
        UnityEngine.Debug.Log("ASDASD");
		UnityEditor.iOS.BuildPipeline.collectResources += CollectResources;
	}

	static UnityEditor.iOS.Resource[] CollectResources()
	{
		return new Resource[]
		{
			new Resource("iOS", "AssetBundles/iOS/iOS").AddOnDemandResourceTags("iOS"),
			new Resource("scene.unity3d", "AssetBundles/iOS/scene.unity3d").AddOnDemandResourceTags("scene.unity3d"), 
			new Resource("cube.unity3d", "AssetBundles/iOS/cube.unity3d").AddOnDemandResourceTags("cube.unity3d"), 
			new Resource("texture.unity3d", "AssetBundles/iOS/texture.unity3d").AddOnDemandResourceTags("texture.unity3d"),
			new Resource("material.unity3d", "AssetBundles/iOS/material.unity3d").AddOnDemandResourceTags("material.unity3d"),
            new Resource("my-loader-assets", "AssetBundles/iOS/my-loader-assets").AddOnDemandResourceTags("my-loader-assets")

			//new Resource("variants/variant-scene.unity3d", "AssetBundles/iOS/variants/variant-scene.unity3d").AddOnDemandResourceTags("variants/variant-scene.unity3d"),
			//new Resource("variants/myassets").BindVariant("AssetBundles/iOS/variants/myassets.hd", "hd")
			//								 .BindVariant("AssetBundles/iOS/variants/myassets.sd", "sd")
		};
	}
}
#endif
