using UnityEngine;
using System.Collections;
using AssetBundles;
using System.Collections.Generic;

public class MyLoader : MonoBehaviour
{
    public const string AssetBundlesOutputPath = "/AssetBundles/";

    public string assetBundleName;
    public string[] assetNames;

    public string sceneBundleName;
    public string sceneName;

    public Dictionary<string, GameObject> objs = new Dictionary<string, GameObject>();

    public void LoadObj(string name)
    {
        GameObject prefab;

        if(objs.TryGetValue(name, out prefab))
        {
            Instantiate(prefab);
        }
    }

    void SetSourceAssetBundleURL()
    {
        // If ODR is available and enabled, then use it and let Xcode handle download requests.
#if ENABLE_IOS_ON_DEMAND_RESOURCES
		if (UnityEngine.iOS.OnDemandResources.enabled)
		{
			AssetBundleManager.SetSourceAssetBundleURL("odr://");
			return;
		}
#endif

        // In editor / development builds always use asset bundle server
        // (This is quite dependent on the production workflow you might want to have, 
        // it could also make sense to make this configurable in the standalone player.)
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        AssetBundleManager.SetDevelopmentAssetBundleServer();
#else
		// Use the following if asset bundles are side-by-side with web deployment:
		AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
		// Or customize based on your deployment or configuration
		//AssetBundleManager.SetSourceAssetBundleURL("http://www.MyWebsite/MyAssetBundles");
#endif
    }

    // Initialize the downloading url and AssetBundleManifest object.
    protected IEnumerator Initialize()
    {
        // Don't destroy the game object as we base on it to run the loading script.
        DontDestroyOnLoad(gameObject);

        SetSourceAssetBundleURL();

        // Initialize AssetBundleManifest which loads the AssetBundleManifest object.
        var request = AssetBundleManager.Initialize();

        if (request != null)
        {
            yield return StartCoroutine(request);
        }
    }

    protected IEnumerator InstantiateGameObjectAsync(string assetName)
    {
        float startTime = Time.realtimeSinceStartup;

        // Load asset from assetBundle.
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        float elapsedTime = Time.realtimeSinceStartup - startTime;
        // Get the asset.
        GameObject prefab = request.GetAsset<GameObject>();
        Debug.Log(assetName + (prefab == null ? " isn't" : " is") + " loaded successfully in " + elapsedTime + " seconds");

        if (prefab != null)
        {
            objs.Add(assetName, prefab);
        }
    }

    IEnumerator Start()
    {
        yield return StartCoroutine(Initialize());

        for (int i = 0; i < assetNames.Length; i++)
        {
            // Load asset.
            yield return StartCoroutine(InstantiateGameObjectAsync(assetNames[i]));
        }

        // Unload assetBundles.
        //AssetBundleManager.UnloadAssetBundle(assetBundleName);
    }

    protected IEnumerator InitializeLevelAsync(string levelName, bool isAdditive)
    {
        float startTime = Time.realtimeSinceStartup;

        // Load level from assetBundle.
        AssetBundleLoadOperation request = AssetBundleManager.LoadLevelAsync(sceneBundleName, levelName, isAdditive);
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        float elapsedTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Finished loading scene " + levelName + " in " + elapsedTime + " seconds");
    }

    public void LoadScene()
    {
        StartCoroutine(InitializeLevelAsync(sceneName, false));
    }
}
