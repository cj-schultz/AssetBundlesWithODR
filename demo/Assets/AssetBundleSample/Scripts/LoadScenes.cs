using UnityEngine;
using System.Collections;
using System.IO;
using AssetBundles;


public class LoadScenes : MonoBehaviour
{
	public string sceneAssetBundle;
	public string sceneName;
	public bool loadLevelAdditive = true;

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
		AssetBundleManager.SetDevelopmentAssetBundleServer ();
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
			yield return StartCoroutine(request);
	}

	protected IEnumerator InitializeLevelAsync (string levelName, bool isAdditive)
	{
		float startTime = Time.realtimeSinceStartup;

		// Load level from assetBundle.
		AssetBundleLoadOperation request = AssetBundleManager.LoadLevelAsync(sceneAssetBundle, levelName, isAdditive);
		if (request == null)
			yield break;
		yield return StartCoroutine(request);

		float elapsedTime = Time.realtimeSinceStartup - startTime;
		Debug.Log("Finished loading scene " + levelName + " in " + elapsedTime + " seconds" );
	}

	// Use this for initialization
	IEnumerator Start ()
	{	
		yield return StartCoroutine(Initialize() );

		// Load level.
		yield return StartCoroutine(InitializeLevelAsync (sceneName, loadLevelAdditive) );

		// Unload assetBundles.
		AssetBundleManager.UnloadAssetBundle(sceneName);
	}
}
